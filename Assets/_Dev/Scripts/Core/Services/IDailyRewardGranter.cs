using System.Collections.Generic;
using Game.Core.Domain.DailyReward;

namespace Game.Core.Services
{
    // The one game-specific seam of the daily reward: turns item ids into this game's currencies.
    // An id the game does not know is skipped, never thrown on, so a typo in remote config costs one item.
    public interface IDailyRewardGranter
    {
        void Grant(IReadOnlyList<DailyRewardItem> items, int slot);
    }
}
