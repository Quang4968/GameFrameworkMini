using System;
using Game.Game.Views.Popup;
using UnityEngine;
using VContainer.Unity;

namespace Game.Game.Views.Gameplay
{
    // Freezes the level while a popup is on screen: every animation stops and the board holds
    // where they are, and every effect in flight resumes from that same frame when the popup closes.
    //
    // Time.timeScale rather than a flag each system checks: play is driven by LitMotion handles,
    // coroutines and Update deltas spread over a dozen views, and only the clock reaches all of them
    // at once. Everything that must keep living behind a popup already runs on unscaled time — the
    // popup animation, button feedback, audio fades, haptics, the life ticker and the scene
    // transition — so pausing here costs them nothing.
    //
    // Registered in the gameplay scene only: Home has no play to freeze, and a stopped clock there
    // would just kill its own tab animations.
    public sealed class LevelPopupPause : IStartable, IDisposable
    {
        readonly IPopupService _popups;

        // The speed play resumes at. Captured instead of hardcoded to 1 so the editor's time scale
        // slider survives a popup.
        float _playingTimeScale = 1f;
        bool _paused;

        public LevelPopupPause(IPopupService popups)
        {
            _popups = popups;
        }

        public void Start()
        {
            // A popup can already be up: the unlock reward opens before the entry points of its own scene start.
            if (_popups.OpenCount > 0) Pause();

            _popups.Opened += OnPopupOpened;
            _popups.Closed += OnPopupClosed;
        }

        public void Dispose()
        {
            _popups.Opened -= OnPopupOpened;
            _popups.Closed -= OnPopupClosed;

            // Leaving the level straight from a popup (Home out of Settings) must not carry the freeze
            // into the next scene — Time.timeScale outlives the scene that set it.
            Resume();
        }

        // Counted by the service, not latched here: the last popup to close is the one that resumes play,
        // whichever order they were opened in.
        void OnPopupOpened(PopupId id)
        {
            if (_popups.OpenCount == 1) Pause();
        }

        void OnPopupClosed(PopupId id)
        {
            if (_popups.OpenCount == 0) Resume();
        }

        void Pause()
        {
            if (_paused) return;

            _paused = true;

            // A zero already on the clock belongs to something else; resuming to it would freeze play for good.
            if (Time.timeScale > 0f) _playingTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        void Resume()
        {
            if (!_paused) return;

            _paused = false;
            Time.timeScale = _playingTimeScale;
        }
    }
}
