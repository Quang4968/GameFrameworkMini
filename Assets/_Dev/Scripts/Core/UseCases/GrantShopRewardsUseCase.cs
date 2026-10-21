using System;
using System.Collections.Generic;
using Game.Core.Domain.Shop;
using Game.Core.Services;

namespace Game.Core.UseCases
{
    // The ONE place that turns reward lines into balance changes (daily reward, debug menu), so every
    // source pays out the same way. It does not guard against being called twice; the caller does.
    public static class GrantShopRewardsUseCase
    {
        // Returns what LANDED, not what was promised: a life grant on a full bar pays out less.
        public static IReadOnlyList<ShopGrant> Execute(
            IReadOnlyList<ShopReward> rewards,
            ICoinService coins,
            ILifeService lives,
            IBoosterInventoryService boosters)
        {
            var granted = new List<ShopGrant>(rewards?.Count ?? 0);
            if (rewards == null) return granted;

            foreach (var reward in rewards)
            {
                switch (reward.Kind)
                {
                    case E_ShopRewardKind.Coins:
                    {
                        if (coins == null || reward.Amount <= 0) break;

                        coins.Add(reward.Amount);
                        granted.Add(new ShopGrant(reward.Kind, reward.Amount, coins.Coins));
                        break;
                    }

                    case E_ShopRewardKind.Lives:
                    {
                        if (lives == null || reward.Amount <= 0) break;

                        // Regen may have filled the bar while the payment sheet was open, so the cap
                        // has to be measured against the number the player is about to see.
                        lives.Refresh();

                        var before = lives.Lives;
                        lives.Add(reward.Amount);
                        granted.Add(new ShopGrant(reward.Kind, lives.Lives - before, lives.Lives));
                        break;
                    }

                    case E_ShopRewardKind.Booster:
                    {
                        if (boosters == null || reward.Amount <= 0) break;

                        boosters.Add(reward.Booster, reward.Amount);
                        granted.Add(new ShopGrant(
                            reward.Kind, reward.Amount, boosters.GetCount(reward.Booster), reward.Booster));
                        break;
                    }

                    case E_ShopRewardKind.UnlimitedLives:
                    {
                        if (lives == null || reward.Amount <= 0) break;

                        // No cap to measure against: the minutes stack onto whatever window runs, so
                        // the grant is the promise and the balance is the whole window left.
                        lives.AddUnlimited(TimeSpan.FromMinutes(reward.Amount));
                        granted.Add(new ShopGrant(
                            reward.Kind, reward.Amount, (int)Math.Ceiling(lives.UnlimitedTimeLeft.TotalMinutes)));
                        break;
                    }
                }
            }

            return granted;
        }
    }
}
