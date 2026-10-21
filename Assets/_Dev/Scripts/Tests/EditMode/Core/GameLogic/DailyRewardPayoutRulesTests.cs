using System;
using NUnit.Framework;
using Game.Core.Domain;
using Game.Core.Domain.DailyReward;
using Game.Core.Domain.Shop;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class DailyRewardPayoutRulesTests
    {
        static ShopReward Map(string id, int amount)
        {
            Assert.IsTrue(DailyRewardPayoutRules.TryToShopReward(new DailyRewardItem(id, amount), out var reward), id);
            return reward;
        }

        [Test]
        public void TryToShopReward_Currencies_MapToTheirShopKinds()
        {
            Assert.AreEqual(E_ShopRewardKind.Coins, Map("coins", 100).Kind);
            Assert.AreEqual(E_ShopRewardKind.Lives, Map("lives", 1).Kind);

            var unlimited = Map("unlimited_lives_minutes", 15);
            Assert.AreEqual(E_ShopRewardKind.UnlimitedLives, unlimited.Kind);
            Assert.AreEqual(15, unlimited.Amount, "Unlimited lives travel in minutes.");
        }

        // Every booster the game declares is reachable by its snake_case name, with no mapping to maintain.
        [Test]
        public void TryToShopReward_EveryBoosterBySnakeCaseName_MapsToThatBooster()
        {
            foreach (BoosterType booster in Enum.GetValues(typeof(BoosterType)))
            {
                var reward = Map(DailyRewardPayoutRules.IdOf(booster), 2);
                Assert.AreEqual(E_ShopRewardKind.Booster, reward.Kind);
                Assert.AreEqual(booster, reward.Booster);
                Assert.AreEqual(2, reward.Amount);
            }
        }

        [Test]
        public void TryToShopReward_UnknownIdNumericIdOrNonPositiveAmount_IsRefused()
        {
            Assert.IsFalse(DailyRewardPayoutRules.TryToShopReward(new DailyRewardItem("gems", 5), out _));
            Assert.IsFalse(DailyRewardPayoutRules.TryToShopReward(new DailyRewardItem("999", 5), out _));
            Assert.IsFalse(DailyRewardPayoutRules.TryToShopReward(new DailyRewardItem("coins", 0), out _));
            Assert.IsFalse(DailyRewardPayoutRules.TryToShopReward(new DailyRewardItem(null, 5), out _));
        }
    }
}
