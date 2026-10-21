using System;

namespace Game.Core.Services
{
    // The one "level over" signal the framework listens to: popups, rewards, streaks and tracking
    // all hang off it. The game reports outcomes through GameplayBridge; each event fires once per
    // outcome, and Resumed means a loss was undone by a revive.
    public interface ILevelOutcomeSource
    {
        event Action Won;
        event Action Lost;
        event Action Resumed;

        // Items the player still has to clear, sampled by tracking; 0 when the game does not count.
        int ItemsRemaining { get; }
    }
}
