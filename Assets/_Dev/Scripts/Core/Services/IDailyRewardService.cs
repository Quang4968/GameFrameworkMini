using System;

namespace Game.Core.Services
{
    // Remembers how many daily rewards were claimed and on which calendar day the last one was.
    // Decides nothing: DailyRewardRules turns these into "can claim" and "which slot".
    public interface IDailyRewardService
    {
        int ClaimedCount { get; }

        // DailyRewardRules.DayNumber of the last claim; negative when nothing was ever claimed.
        int LastClaimDay { get; }

        // DailyRewardRules.DayNumber of today in the device's time zone.
        int Today { get; }

        // Raised with the new ClaimedCount after every claim.
        event Action<int> ClaimedCountChanged;

        void RecordClaim(int day);
    }
}
