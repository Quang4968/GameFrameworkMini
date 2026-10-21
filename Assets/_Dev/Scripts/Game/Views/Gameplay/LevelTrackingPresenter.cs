using System;
using System.Collections.Generic;
using MoodBoost.Sdk;
using MoodBoost.Sdk.Ads;
using MoodBoost.Sdk.Tracking;
using Game.Core.Domain;
using Game.Core.Services;
using Game.Game.Installers;
using UnityEngine;
using VContainer.Unity;

namespace Game.Game.Views.Gameplay
{
    // The four level_* events of the tracking plan, hung off ILevelOutcomeSource - the project's only "level over"
    // signal. The level number comes from the SDK; play time from I_LevelPlayTimeService, which
    // LevelPlayTimePresenter pauses for ads and popups.
    //
    // ONE play logs EXACTLY ONE level_end. `Lost` is not final: LevelReviveController reports Resumed and the
    // level carries on. So the loss is HELD and only committed once certain - the player leaves, or the app goes to the
    // background for anything but a full-screen ad. A revive cancels it and logs level_reopen instead.
    public sealed class LevelTrackingPresenter : IStartable, ITickable, IDisposable
    {
        // The loss reason the plan expects when the player runs out of moves.
        const string LoseByNoMove = "no_move";

        // The game has no countdown, so the plan's time-limit columns log -1 instead of a real value.
        const long NoTimeLimitMs = -1;

        const string LevelAchievementPrefsPrefix = "MoodBoost.Attribution.LevelAchieved.";

        readonly ILevelTracking tracking;
        readonly IAttributionService attribution;
        readonly ILevelOutcomeSource outcome;
        readonly ILevelProgressService levelProgress;
        readonly I_LevelSessionService session;
        readonly IAppLifecycle lifecycle;
        readonly I_LevelPlayTimeService playTime;
        readonly IAdEventStream ads;

        // Aliases of the boosters used, in order - the plan's action_seq.
        readonly List<int> actions = new List<int>();

        // Items at level start = the most ever seen. Start() is too early: the game builds its board afterwards,
        // so the count is 0 and items_cleared would go negative. The max over the level is the start value.
        int itemsTotal;
        bool pendingLose;
        bool ended;
        bool adOnScreen;

        public LevelTrackingPresenter(
            ILevelTracking tracking,
            IAttributionService attribution,
            ILevelOutcomeSource outcome,
            ILevelProgressService levelProgress,
            I_LevelSessionService session,
            IAppLifecycle lifecycle,
            I_LevelPlayTimeService playTime,
            IAdEventStream ads)
        {
            this.tracking = tracking;
            this.attribution = attribution;
            this.outcome = outcome;
            this.levelProgress = levelProgress;
            this.session = session;
            this.lifecycle = lifecycle;
            this.playTime = playTime;
            this.ads = ads;
        }

        public void Tick() => SampleItems();

        void SampleItems()
        {
            var remaining = Remaining();
            if (remaining > itemsTotal) itemsTotal = remaining;
        }

        // What counts as an item is the game's call, through ILevelOutcomeSource.ItemsRemaining.
        int Remaining() => outcome.ItemsRemaining;

        // Every booster reports here through BoosterButton.Used, so action_seq keeps the order the player pressed them.
        // Alias = enum value + 1, because "0" in the plan means no action at all.
        public void ReportBoosterUsed(BoosterType type) => actions.Add((int)type + 1);

        public void Start()
        {
            playTime.Begin();
            SampleItems();

            session.BeginPlay(levelProgress.LevelShow + 1);

            tracking.LogLevelStart(Info(), NoTimeLimitMs);

            outcome.Won += OnWon;
            outcome.Lost += OnLost;
            outcome.Resumed += OnResumed;

            if (lifecycle != null) lifecycle.Paused += OnAppPaused;

            ads.AdRequested += OnAdRequested;
            ads.Completed += OnAdCompleted;
        }

        public void Dispose()
        {
            outcome.Won -= OnWon;
            outcome.Lost -= OnLost;
            outcome.Resumed -= OnResumed;

            if (lifecycle != null) lifecycle.Paused -= OnAppPaused;

            ads.AdRequested -= OnAdRequested;
            ads.Completed -= OnAdCompleted;

            FlushPendingLose();

            // Leaving with no result is abandoning the level, not losing it by the rules.
            if (ended) return;

            ended = true;
            tracking.LogLevelExit(Info(), Stats(), session.ExitIndex);
            session.RecordExit();
        }

        void OnWon()
        {
            // A win after a revive: the held loss never happened.
            pendingLose = false;
            if (ended) return;

            ended = true;
            tracking.LogLevelEnd(Info(), Stats(), LevelResult.Win);
            LogLevelAchievement();
        }

        void LogLevelAchievement()
        {
            var level = levelProgress.LevelShow + 1;
            if (!attribution.IsReady || !MmpEvents.TryLevelAchieved(level, out var eventName)) return;

            var prefsKey = LevelAchievementPrefsPrefix + level;
            if (PlayerPrefs.GetInt(prefsKey, 0) == 1) return;

            // Persist first so replaying the same level after an interrupted win flow cannot duplicate the conversion.
            PlayerPrefs.SetInt(prefsKey, 1);
            PlayerPrefs.Save();
            attribution.LogEvent(eventName);
        }

        // Not logged yet: the player can still revive from the lose popup.
        void OnLost() => pendingLose = true;

        void OnResumed()
        {
            if (!pendingLose) return;

            pendingLose = false;
            tracking.LogLevelReopen(Info(), NoTimeLimitMs);
        }

        // A full-screen ad pauses the app on Android, and that ad may be the revive - so the loss stays held through it.
        void OnAppPaused()
        {
            if (!adOnScreen) FlushPendingLose();
        }

        void OnAdRequested(AdFormat format, string placement)
        {
            if (format != AdFormat.Banner && format != AdFormat.Mrec) adOnScreen = true;
        }

        // Blocked is also what a second show gets while an ad is still on screen.
        void OnAdCompleted(AdCompletion completion)
        {
            if (completion.Result != AdResult.Blocked) adOnScreen = false;
        }

        void FlushPendingLose()
        {
            if (!pendingLose || ended) return;

            pendingLose = false;
            ended = true;

            tracking.LogLevelEnd(Info(), Stats(), LevelResult.Lose, LoseByNoMove);
            session.RecordLoss();
        }

        LevelTrackingInfo Info()
        {
            // LevelLoad differs from LevelShow only after the game's last level loops, and then it is the plan's loop_by.
            var loopBy = levelProgress.LevelLoad != levelProgress.LevelShow ? levelProgress.LevelLoad + 1 : 0;

            return new LevelTrackingInfo(
                ToPlayType(session.PlayType),
                session.PlayIndex,
                session.LoseIndex,
                loopBy,
                UserPropertyInstaller.DefaultMode);
        }

        LevelPlayStats Stats()
        {
            var remaining = Remaining();

            return new LevelPlayStats(NoTimeLimitMs, NoTimeLimitMs, NoTimeLimitMs, playTime.PlayedMs,
                itemsTotal, itemsTotal - remaining, TrackingValues.Sequence(actions));
        }

        static PlayType ToPlayType(E_LevelPlayType value)
        {
            switch (value)
            {
                case E_LevelPlayType.Next: return PlayType.Next;
                case E_LevelPlayType.Restart: return PlayType.Restart;
                default: return PlayType.Home;
            }
        }
    }
}
