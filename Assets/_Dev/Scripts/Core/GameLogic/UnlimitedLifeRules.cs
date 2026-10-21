using System;

namespace Game.Core.GameLogic
{
    // Unlimited lives run to an absolute UTC end, so real time keeps counting while the app is closed.
    // Deliberately unguarded, unlike LifeClockGuard: winding the clock forward ends the window early,
    // winding it back stretches it.
    public static class UnlimitedLifeRules
    {
        public static TimeSpan TimeLeft(DateTime endUtc, DateTime nowUtc)
            => endUtc > nowUtc ? endUtc - nowUtc : TimeSpan.Zero;

        // A grant landing while a window runs stacks onto its end; one landing after it ran out starts now.
        public static DateTime Extend(DateTime endUtc, DateTime nowUtc, TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero) return endUtc;

            var start = endUtc > nowUtc ? endUtc : nowUtc;
            return DateTime.MaxValue - start > duration ? start + duration : DateTime.MaxValue;
        }
    }
}
