using System.Collections.Generic;
using MoodBoost.Sdk.Tracking;
using Game.Core.Domain.DailyReward;
using Game.Core.Domain.Shop;
using Game.Core.GameLogic;
using Game.Core.Services;
using Game.Core.UseCases;
using UnityEngine;

namespace Game.Game.Services
{
    // Pays a daily reward day through the shop's accounts, with the same resource_earn shape as a
    // bought pack. A game whose rewards are not coins, lives or boosters replaces this one class.
    public sealed class ShopDailyRewardGranter : IDailyRewardGranter
    {
        readonly ICoinService coins;
        readonly ILifeService lives;
        readonly IBoosterInventoryService boosters;
        readonly ITrackingService tracking;

        public ShopDailyRewardGranter(
            ICoinService coins, ILifeService lives, IBoosterInventoryService boosters, ITrackingService tracking)
        {
            this.coins = coins;
            this.lives = lives;
            this.boosters = boosters;
            this.tracking = tracking;
        }

        public void Grant(IReadOnlyList<DailyRewardItem> items, int slot)
        {
            if (items == null || items.Count == 0) return;

            var rewards = new List<ShopReward>(items.Count);

            foreach (var item in items)
            {
                if (DailyRewardPayoutRules.TryToShopReward(item, out var reward))
                {
                    rewards.Add(reward);
                    continue;
                }

                Debug.LogWarning($"[{nameof(ShopDailyRewardGranter)}] Unknown daily reward item " +
                    $"'{item.Id}' x{item.Amount} on day {slot + 1}; skipped.");
            }

            var granted = GrantShopRewardsUseCase.Execute(rewards, coins, lives, boosters);

            if (tracking == null) return;

            var changes = ShopGrantTracking.ToChanges(granted);
            if (changes.Count == 0) return;

            tracking.LogResourceEarn(
                changes, TrackingPlacements.DailyReward, EarnReason.Reward, "day_" + (slot + 1));
        }
    }
}
