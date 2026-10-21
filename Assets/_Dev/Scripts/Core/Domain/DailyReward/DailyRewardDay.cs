using System;
using System.Collections.Generic;

namespace Game.Core.Domain.DailyReward
{
    // Everything one login day pays out, in display order.
    public sealed class DailyRewardDay
    {
        public IReadOnlyList<DailyRewardItem> Items { get; }

        // A chest keeps its items hidden behind a gift until claimed; it pays out exactly like any other day.
        public bool IsChest { get; }

        public DailyRewardDay(IReadOnlyList<DailyRewardItem> items, bool isChest = false)
        {
            Items = items ?? Array.Empty<DailyRewardItem>();
            IsChest = isChest;
        }
    }
}
