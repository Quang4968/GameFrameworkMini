using Game.Core.GameLogic;
using Game.Core.Services;

namespace Game.Core.UseCases
{
    // Takes today's daily reward: the next slot of the cycle, paid out once per calendar day.
    public static class ClaimDailyRewardUseCase
    {
        public const int NotClaimed = -1;

        // Returns the slot paid out, or NotClaimed when today's reward is already taken. The claim is
        // recorded BEFORE the payout: an interruption between the two loses a reward rather than
        // handing out a second one on the next launch.
        public static int Execute(IDailyRewardService state, IDailyRewardSchedule schedule, IDailyRewardGranter granter)
        {
            if (state == null || schedule == null) return NotClaimed;

            var days = schedule.Days;
            if (days == null || days.Count == 0) return NotClaimed;

            var today = state.Today;
            if (!DailyRewardRules.CanClaim(state.LastClaimDay, today)) return NotClaimed;

            var slot = DailyRewardRules.NextSlot(state.ClaimedCount, days.Count);
            state.RecordClaim(today);
            granter?.Grant(days[slot].Items, slot);

            return slot;
        }
    }
}
