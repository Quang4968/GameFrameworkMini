using System.Collections.Generic;
using Game.Core.Domain.DailyReward;

namespace Game.Core.Services
{
    // One full cycle of daily rewards, one entry per login day, plus the level that opens the feature.
    // Always a valid cycle (DailyRewardRules.IsValidCycle): a broken remote value falls back to the shipped one.
    public interface IDailyRewardSchedule
    {
        IReadOnlyList<DailyRewardDay> Days { get; }

        int UnlockLevel { get; }
    }
}
