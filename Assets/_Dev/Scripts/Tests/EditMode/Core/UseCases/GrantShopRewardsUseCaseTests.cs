using System;
using System.Collections.Generic;
using NUnit.Framework;
using Game.Core.Domain;
using Game.Core.Domain.Shop;
using Game.Core.Services;
using Game.Core.UseCases;

namespace Game.Core.Tests.UseCases
{
    public class GrantShopRewardsUseCaseTests
    {
        sealed class FakeCoinService : ICoinService
        {
            public int Coins { get; private set; }
            public event Action<int> CoinsChanged;

            public void Add(int amount)
            {
                if (amount <= 0) return;
                Coins += amount;
                CoinsChanged?.Invoke(Coins);
            }

            public bool TrySpend(int amount)
            {
                if (amount <= 0 || Coins < amount) return false;
                Coins -= amount;
                CoinsChanged?.Invoke(Coins);
                return true;
            }
        }

        sealed class FakeLifeService : ILifeService
        {
            public int Lives { get; private set; }
            public int MaxLives { get; }
            public TimeSpan TimeUntilNextLife => TimeSpan.Zero;
            public int RefreshCount { get; private set; }
            public event Action<int> LivesChanged;
            public TimeSpan UnlimitedTimeLeft { get; private set; }
            public event Action<bool> UnlimitedChanged { add { } remove { } }

            public FakeLifeService(int maxLives, int lives) { MaxLives = maxLives; Lives = lives; }

            public void Add(int amount)
            {
                if (amount <= 0 || Lives >= MaxLives) return;
                Lives = Math.Min(Lives + amount, MaxLives);
                LivesChanged?.Invoke(Lives);
            }

            public bool TrySpend(int amount)
            {
                if (amount <= 0 || Lives < amount) return false;
                Lives -= amount;
                LivesChanged?.Invoke(Lives);
                return true;
            }

            public void Refresh() => RefreshCount++;

            public void AddUnlimited(TimeSpan duration)
            {
                if (duration > TimeSpan.Zero) UnlimitedTimeLeft += duration;
            }
        }

        sealed class FakeBoosterInventory : IBoosterInventoryService
        {
            readonly Dictionary<BoosterType, int> counts = new();

            public event Action<BoosterType, int> CountChanged;

            public int GetCount(BoosterType type) => counts.TryGetValue(type, out var count) ? count : 0;

            public void Add(BoosterType type, int amount)
            {
                if (amount <= 0) return;
                counts[type] = GetCount(type) + amount;
                CountChanged?.Invoke(type, counts[type]);
            }

            public bool TrySpend(BoosterType type, int amount)
            {
                if (amount <= 0 || GetCount(type) < amount) return false;
                counts[type] = GetCount(type) - amount;
                CountChanged?.Invoke(type, counts[type]);
                return true;
            }
        }

        static IReadOnlyList<ShopGrant> Grant(
            IReadOnlyList<ShopReward> rewards, FakeCoinService coins, FakeLifeService lives,
            FakeBoosterInventory boosters)
            => GrantShopRewardsUseCase.Execute(rewards, coins, lives, boosters);

        [Test]
        public void Execute_PaysOutEveryLineOfThePack()
        {
            var coins = new FakeCoinService();
            var lives = new FakeLifeService(5, 0);
            var boosters = new FakeBoosterInventory();

            var granted = Grant(new[]
            {
                new ShopReward(E_ShopRewardKind.Coins, 2000),
                new ShopReward(E_ShopRewardKind.Booster, 3, Boosters.A),
                new ShopReward(E_ShopRewardKind.Booster, 2, Boosters.E),
            }, coins, lives, boosters);

            Assert.AreEqual(2000, coins.Coins);
            Assert.AreEqual(3, boosters.GetCount(Boosters.A));
            Assert.AreEqual(2, boosters.GetCount(Boosters.E));
            Assert.AreEqual(3, granted.Count);
        }

        // Tracking reports what the player actually got; a life pack bought on a nearly full bar
        // hands over less than it promises, and resource_balance has to match the counter on screen.
        [Test]
        public void Execute_ReportsTheClampedAmount_NotThePromisedOne()
        {
            var lives = new FakeLifeService(5, 4);

            var granted = Grant(new[] { new ShopReward(E_ShopRewardKind.Lives, 3) },
                new FakeCoinService(), lives, new FakeBoosterInventory());

            Assert.AreEqual(5, lives.Lives);
            Assert.AreEqual(1, granted.Count);
            Assert.AreEqual(1, granted[0].Amount);
            Assert.AreEqual(5, granted[0].Balance);
        }

        // Regen can fill the bar while the payment sheet is open, so the cap must be measured
        // against the number the player is about to see rather than a stale one.
        [Test]
        public void Execute_RefreshesLives_BeforeGrantingThem()
        {
            var lives = new FakeLifeService(5, 1);

            Grant(new[] { new ShopReward(E_ShopRewardKind.Lives, 1) },
                new FakeCoinService(), lives, new FakeBoosterInventory());

            Assert.AreEqual(1, lives.RefreshCount);
        }

        // Minutes stack onto a running window, and the balance tracking reports is the whole window.
        [Test]
        public void Execute_UnlimitedLives_ExtendsTheWindow_AndReportsMinutesLeft()
        {
            var lives = new FakeLifeService(5, 5);
            lives.AddUnlimited(TimeSpan.FromMinutes(30));

            var granted = Grant(new[] { new ShopReward(E_ShopRewardKind.UnlimitedLives, 60) },
                new FakeCoinService(), lives, new FakeBoosterInventory());

            Assert.AreEqual(TimeSpan.FromMinutes(90), lives.UnlimitedTimeLeft);
            Assert.AreEqual(1, granted.Count);
            Assert.AreEqual(E_ShopRewardKind.UnlimitedLives, granted[0].Kind);
            Assert.AreEqual(60, granted[0].Amount);
            Assert.AreEqual(90, granted[0].Balance);
        }

        [Test]
        public void Execute_UnlimitedLivesOfZeroMinutes_GrantsNothing()
        {
            var lives = new FakeLifeService(5, 5);

            var granted = Grant(new[] { new ShopReward(E_ShopRewardKind.UnlimitedLives, 0) },
                new FakeCoinService(), lives, new FakeBoosterInventory());

            Assert.AreEqual(TimeSpan.Zero, lives.UnlimitedTimeLeft);
            Assert.AreEqual(0, granted.Count);
        }

        // A pack whose catalog row is half filled in must not take the whole delivery down with it -
        // the player has already paid by the time this runs.
        [Test]
        public void Execute_SkipsBrokenLines_AndSurvivesNullServices()
        {
            var coins = new FakeCoinService();

            var granted = GrantShopRewardsUseCase.Execute(new[]
            {
                new ShopReward(E_ShopRewardKind.Coins, 0),
                new ShopReward(E_ShopRewardKind.Booster, 5, Boosters.C),
                new ShopReward(E_ShopRewardKind.Coins, 100),
            }, coins, null, null);

            Assert.AreEqual(100, coins.Coins);
            Assert.AreEqual(1, granted.Count);
        }

        [Test]
        public void Execute_NullRewards_ReturnsEmpty_AndDoesNotThrow()
        {
            Assert.AreEqual(0, GrantShopRewardsUseCase.Execute(null, null, null, null).Count);
        }
    }
}
