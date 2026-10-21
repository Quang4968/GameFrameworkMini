using System;

namespace Game.Core.Services
{
    // Holds the game's automatic actions (auto-fire, timers) while a lesson or a booster waits for
    // the player. TryHold returns null when the game refuses; disposing the hold releases it.
    public interface IGameplayPauseService
    {
        IDisposable TryHold();
    }
}
