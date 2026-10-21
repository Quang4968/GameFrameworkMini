using System;
using Game.Core.Services;

namespace Game.Game.Services
{
    // The seam between the game's board and the framework. Registered once in MainGameLifetimeScope;
    // the game's bootstrap resolves it and wires the delegates, then its outcome watcher calls
    // ReportWon / ReportLost. Every delegate is optional: unset, revive is impossible, pauses are
    // granted and tracking counts no items.
    public sealed class GameplayBridge : ILevelOutcomeSource, ILevelReviveHandler, IGameplayPauseService
    {
        public event Action Won;
        public event Action Lost;
        public event Action Resumed;

        public Func<int> ItemsRemainingProvider { get; set; }
        public Func<bool> CanReviveProvider { get; set; }
        public Func<bool> ReviveHandler { get; set; }
        public Func<IDisposable> PauseHandler { get; set; }

        public int ItemsRemaining => ItemsRemainingProvider != null ? ItemsRemainingProvider() : 0;
        public bool CanRevive => CanReviveProvider != null && CanReviveProvider();

        public void ReportWon() => Won?.Invoke();
        public void ReportLost() => Lost?.Invoke();
        public void ReportResumed() => Resumed?.Invoke();

        public bool TryRevive() => ReviveHandler != null && ReviveHandler();

        public IDisposable TryHold() => PauseHandler != null ? PauseHandler() : new NoHold();

        sealed class NoHold : IDisposable
        {
            public void Dispose() { }
        }
    }
}
