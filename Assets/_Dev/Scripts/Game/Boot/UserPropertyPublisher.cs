using System;
using MoodBoost.Sdk;
using MoodBoost.Sdk.Tracking;
using Game.Core.Domain;
using Game.Core.Services;
using Game.Game.Installers;
using Game.Game.Services;
using UnityEngine;
using VContainer.Unity;

namespace Game.Game.Boot
{
    // Publishes every user property the GAME owns: one burst when the scene opens, then whatever changes.
    //
    // The opening burst is not redundant with the event subscriptions. A provider that failed to init this session
    // received nothing at all, and SetUserProperty is fire-and-forget with no way to read back what Firebase holds -
    // so the only way to repair drift is to overwrite on every launch. TrackingService drops the unchanged ones.
    public sealed class UserPropertyPublisher : IStartable, ITickable, IDisposable
    {
        // The day can roll over while the player simply leaves the app open; nothing else would notice.
        const float ActiveDayIntervalSeconds = 60f;

        readonly ITrackingService tracking;
        readonly ICoinService coins;
        readonly ILifeService lives;
        readonly ILevelProgressService levelProgress;
        readonly I_LevelStreakService streaks;
        readonly I_ActiveDayService activeDay;
        readonly IAppLifecycle lifecycle;

        float untilNextTick;

        public UserPropertyPublisher(
            ITrackingService tracking,
            ICoinService coins,
            ILifeService lives,
            ILevelProgressService levelProgress,
            I_LevelStreakService streaks,
            I_ActiveDayService activeDay,
            IAppLifecycle lifecycle)
        {
            this.tracking = tracking;
            this.coins = coins;
            this.lives = lives;
            this.levelProgress = levelProgress;
            this.streaks = streaks;
            this.activeDay = activeDay;
            this.lifecycle = lifecycle;
        }

        public void Start()
        {
            // LevelShow + 1 is the number on the player's screen and in support tickets. LevelLoad diverges from it
            // once the endgame loop starts, and mixing the two under one key makes the segment meaningless.
            tracking.SetCurrentLevel(levelProgress.LevelShow + 1);

            // No game-mode or live-ops system exists yet. Published as constants anyway, because a key with no data
            // at all is a key the data team cannot build on.
            tracking.SetCurrentMode(UserPropertyInstaller.DefaultMode);
            tracking.SetCurrentEvent(null);

            PublishStreaks(streaks.Streak);
            PublishActiveDay(activeDay.DayIndex);
            PublishCoins(coins.Coins);
            PublishLives(lives.Lives);

            coins.CoinsChanged += PublishCoins;
            lives.LivesChanged += PublishLives;
            streaks.StreakChanged += PublishStreaks;
            activeDay.DayIndexChanged += PublishActiveDay;

            if (lifecycle != null) lifecycle.Resumed += OnResumed;
        }

        public void Tick()
        {
            untilNextTick -= Time.unscaledDeltaTime;
            if (untilNextTick > 0f) return;

            untilNextTick = ActiveDayIntervalSeconds;
            activeDay.Refresh();
        }

        public void Dispose()
        {
            coins.CoinsChanged -= PublishCoins;
            lives.LivesChanged -= PublishLives;
            streaks.StreakChanged -= PublishStreaks;
            activeDay.DayIndexChanged -= PublishActiveDay;

            if (lifecycle != null) lifecycle.Resumed -= OnResumed;
        }

        void OnResumed() => activeDay.Refresh();

        void PublishCoins(int value) => tracking.SetResourceBalance(ResourceNames.Coin, value);

        void PublishLives(int value) => tracking.SetResourceBalance(ResourceNames.Life, value);

        void PublishStreaks(LevelStreak value) => tracking.SetStreaks(value.Win, value.Lose);

        void PublishActiveDay(int value) => tracking.SetActiveDay(value);
    }
}
