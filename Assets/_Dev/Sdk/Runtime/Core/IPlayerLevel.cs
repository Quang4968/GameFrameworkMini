using System;

namespace MoodBoost.Sdk
{
    // The player's current level as the player sees it, counted from 1. The SDK has no notion of levels, so the game
    // hands over a function at boot and every module that gates on level reads it through here.
    public interface IPlayerLevel
    {
        // Call before ISdkRuntime.InitializeAsync. Calling again replaces the previous function.
        void SetProvider(Func<int> currentLevel);

        // false while no provider is set, or when it throws - a level-gated rule must then stay out of the way.
        bool TryGetCurrent(out int level);
    }
}
