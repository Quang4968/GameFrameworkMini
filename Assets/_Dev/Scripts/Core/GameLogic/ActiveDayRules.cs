using System;

namespace Game.Core.GameLogic
{
    // How many calendar days the player has been with us: D0 is the day they installed.
    //
    // Days are counted in the timezone the player installed in, captured once as an offset, so someone who travels
    // does not gain or lose a day. The offset is passed in rather than read here for the same reason the clock is:
    // this file stays pure and testable with fake time.
    public static class ActiveDayRules
    {
        public static int DayIndex(DateTime firstOpenUtc, int offsetMinutes, DateTime nowUtc)
        {
            var offset = TimeSpan.FromMinutes(offsetMinutes);

            // Date subtraction, NOT TotalDays: the boundary is real local midnight, not a 24-hour span from install.
            var days = ((nowUtc + offset).Date - (firstOpenUtc + offset).Date).Days;
            return days < 0 ? 0 : days;
        }

        // Winding the device clock back must not rewrite the install date, or every rolled-back session reports D0.
        public static DateTime GuardFirstOpen(DateTime storedUtc, DateTime nowUtc)
        {
            if (storedUtc == default) return nowUtc;
            return storedUtc > nowUtc ? nowUtc : storedUtc;
        }
    }
}
