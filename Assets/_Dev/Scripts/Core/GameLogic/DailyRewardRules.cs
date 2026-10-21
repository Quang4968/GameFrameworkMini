using System;
using System.Collections.Generic;
using Game.Core.Domain.DailyReward;

namespace Game.Core.GameLogic
{
    // The daily login cycle, independent of what any day pays out.
    //
    // The slot to claim counts CLAIMS, not calendar days: a player away for a week resumes on the next
    // slot, nothing is missed or skipped. A day boundary is local midnight, passed in as an offset so
    // this stays pure. The cycle is whole pages of DaysPerPage; after its last slot it wraps to slot 0.
    public static class DailyRewardRules
    {
        public const int DaysPerPage = 7;

        // Days since 0001-01-01 in the given offset: the boundary is local midnight, not 24 hours
        // since the last claim.
        public static int DayNumber(DateTime utcNow, TimeSpan utcOffset) =>
            (int)((utcNow + utcOffset).Date.Ticks / TimeSpan.TicksPerDay);

        // Strictly later, not merely different: winding the clock back cannot re-open a day already
        // taken, and a claim made on a clock set ahead waits until real time catches up.
        public static bool CanClaim(int lastClaimDay, int today) => lastClaimDay < 0 || today > lastClaimDay;

        // unlockLevel 0 or below means the feature is open from the first session.
        public static bool IsUnlocked(int levelShow, int unlockLevel) => unlockLevel <= 0 || levelShow >= unlockLevel;

        // Once per launch, and only when there is something to take: closing it unclaimed is an answer.
        public static bool ShouldAutoOpen(bool offeredThisLaunch, bool unlocked, bool canClaim) =>
            !offeredThisLaunch && unlocked && canClaim;

        public static int NextSlot(int claimedCount, int cycleLength) =>
            cycleLength <= 0 || claimedCount <= 0 ? 0 : claimedCount % cycleLength;

        // Slot the popup shows: the one to claim, or the last one taken once today's is gone, so a
        // reopen after the final day still shows the ticked page rather than a fresh one.
        public static int FocusSlot(int claimedCount, int cycleLength, bool canClaim) =>
            canClaim || claimedCount <= 0
                ? NextSlot(claimedCount, cycleLength)
                : NextSlot(claimedCount - 1, cycleLength);

        public static int PageStart(int slot) => slot < 0 ? 0 : slot - slot % DaysPerPage;

        public static DailyRewardDayState StateOf(int slot, int claimedCount, int cycleLength, bool canClaim)
        {
            if (cycleLength <= 0 || claimedCount <= 0)
                return canClaim && slot == 0 ? DailyRewardDayState.Claimable : DailyRewardDayState.Locked;

            // Slots taken in the current pass. A pass just completed counts as full, not as zero.
            var taken = canClaim ? NextSlot(claimedCount, cycleLength) : NextSlot(claimedCount - 1, cycleLength) + 1;

            if (slot < taken) return DailyRewardDayState.Claimed;
            return canClaim && slot == taken ? DailyRewardDayState.Claimable : DailyRewardDayState.Locked;
        }

        // A usable cycle: whole pages, every day paying at least one item with an id and a positive amount.
        public static bool IsValidCycle(IReadOnlyList<DailyRewardDay> days)
        {
            if (days == null || days.Count == 0 || days.Count % DaysPerPage != 0) return false;

            foreach (var day in days)
            {
                if (day == null || day.Items.Count == 0) return false;

                foreach (var item in day.Items)
                {
                    if (string.IsNullOrEmpty(item.Id) || item.Amount <= 0) return false;
                }
            }

            return true;
        }
    }
}
