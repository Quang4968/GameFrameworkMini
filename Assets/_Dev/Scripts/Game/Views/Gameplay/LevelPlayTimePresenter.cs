using System;
using MoodBoost.Sdk;
using MoodBoost.Sdk.Ads;
using Game.Core.Services;
using Game.Game.Views.Popup;
using VContainer.Unity;

namespace Game.Game.Views.Gameplay
{
    // Pauses the level's play time for everything that is not play: a full-screen ad, any popup on screen,
    // a lost level waiting to be revived, and the app sitting in the background.
    public sealed class LevelPlayTimePresenter : IStartable, IDisposable
    {
        readonly I_LevelPlayTimeService _playTime;
        readonly IAdEventStream _ads;
        readonly IAppLifecycle _lifecycle;
        readonly IPopupService _popups;
        readonly ILevelOutcomeSource _outcome;

        // One latch per source, so a release from a source that never paused cannot unpause another.
        bool _popupOnScreen;
        bool _adOnScreen;
        bool _backgrounded;
        bool _lost;

        public LevelPlayTimePresenter(
            I_LevelPlayTimeService playTime,
            IAdEventStream ads,
            IAppLifecycle lifecycle,
            IPopupService popups,
            ILevelOutcomeSource outcome)
        {
            _playTime = playTime;
            _ads = ads;
            _lifecycle = lifecycle;
            _popups = popups;
            _outcome = outcome;
        }

        public void Start()
        {
            // A popup can already be up: the unlock reward opens before this entry point starts.
            SetLatch(ref _popupOnScreen, _popups.OpenCount > 0);

            _popups.Opened += OnPopupOpened;
            _popups.Closed += OnPopupClosed;
            _ads.AdRequested += OnAdRequested;
            _ads.Completed += OnAdCompleted;
            _lifecycle.Paused += OnAppPaused;
            _lifecycle.Resumed += OnAppResumed;
            _outcome.Lost += OnLost;
            _outcome.Resumed += OnRevived;
        }

        public void Dispose()
        {
            _popups.Opened -= OnPopupOpened;
            _popups.Closed -= OnPopupClosed;
            _ads.AdRequested -= OnAdRequested;
            _ads.Completed -= OnAdCompleted;
            _lifecycle.Paused -= OnAppPaused;
            _lifecycle.Resumed -= OnAppResumed;
            _outcome.Lost -= OnLost;
            _outcome.Resumed -= OnRevived;
        }

        // Held on the service's own tally rather than a set here, so popups opened on top of each other
        // resume play exactly once, when the last one closes.
        void OnPopupOpened(PopupId id) => SetLatch(ref _popupOnScreen, true);

        void OnPopupClosed(PopupId id) => SetLatch(ref _popupOnScreen, _popups.OpenCount > 0);

        // AdRequested is the last step before the ad takes the screen; AdShown only fires after it closes.
        void OnAdRequested(AdFormat format, string placement)
        {
            if (format == AdFormat.Banner || format == AdFormat.Mrec) return;

            SetLatch(ref _adOnScreen, true);
        }

        // Blocked is also what a second show gets WHILE an ad is on screen - releasing on it would resume mid-ad.
        void OnAdCompleted(AdCompletion completion)
        {
            if (completion.Result == AdResult.Blocked) return;

            SetLatch(ref _adOnScreen, false);
        }

        void OnAppPaused() => SetLatch(ref _backgrounded, true);

        void OnAppResumed() => SetLatch(ref _backgrounded, false);

        void OnLost() => SetLatch(ref _lost, true);

        void OnRevived() => SetLatch(ref _lost, false);

        void SetLatch(ref bool latch, bool held)
        {
            if (latch == held) return;

            latch = held;
            if (held) _playTime.Pause();
            else _playTime.Resume();
        }
    }
}
