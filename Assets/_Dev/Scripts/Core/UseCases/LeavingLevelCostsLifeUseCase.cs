using System;
using Game.Core.Services;

namespace Game.Core.UseCases
{
    // Whether quitting mid-level loses a life, i.e. whether the leave confirmation (PopupOut) has
    // anything to warn about. An unlimited window makes leaving free, so the warning is skipped.
    public static class LeavingLevelCostsLifeUseCase
    {
        // No life system means the old behaviour: ask first rather than drop the player out unwarned.
        public static bool Execute(ILifeService lives)
        {
            if (lives == null) return true;

            lives.Refresh();
            return lives.UnlimitedTimeLeft <= TimeSpan.Zero;
        }
    }
}
