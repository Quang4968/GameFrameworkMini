using System;

namespace Game.Core.Services
{
    // Calendar days since the player first opened the game, in their install-local timezone. D0 is install day.
    //
    // Refresh is public because the day can roll over while the app is simply left open - nothing else would notice.
    public interface I_ActiveDayService
    {
        int DayIndex { get; }

        // True for a player who was already on an older build when this system shipped: their install date is
        // unknowable, so their day index is seeded from first sight and must stay separable from a real D0.
        bool IsEstimated { get; }

        event Action<int> DayIndexChanged;

        void Refresh();
    }
}
