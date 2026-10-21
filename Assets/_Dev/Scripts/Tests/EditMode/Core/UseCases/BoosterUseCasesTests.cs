using System;
using System.Collections.Generic;
using NUnit.Framework;
using Game.Core.Domain;
using Game.Core.Services;
using Game.Core.UseCases;

namespace Game.Core.Tests.UseCases
{
    public class BoosterUseCasesTests
    {
        // Kho giả: giữ đúng hai luật của IBoosterInventoryService (không cộng số <= 0, không tiêu
        // quá số đang có) để test bắt được nếu use case đẩy số âm xuống service.
        sealed class FakeBoosterInventory : IBoosterInventoryService
        {
            readonly Dictionary<BoosterType, int> counts = new();

            public int ChangeCount { get; private set; }

            public event Action<BoosterType, int> CountChanged;

            public FakeBoosterInventory(int shuffleCount)
            {
                counts[Boosters.A] = shuffleCount;
            }

            public int GetCount(BoosterType type) => counts.TryGetValue(type, out var count) ? count : 0;

            public void Add(BoosterType type, int amount)
            {
                if (amount <= 0) return;

                counts[type] = GetCount(type) + amount;
                ChangeCount++;
                CountChanged?.Invoke(type, counts[type]);
            }

            public bool TrySpend(BoosterType type, int amount)
            {
                if (amount <= 0 || GetCount(type) < amount) return false;

                counts[type] = GetCount(type) - amount;
                ChangeCount++;
                CountChanged?.Invoke(type, counts[type]);
                return true;
            }
        }

        sealed class FakeCoinService : ICoinService
        {
            public int Coins { get; private set; }

            public event Action<int> CoinsChanged;

            public FakeCoinService(int coins)
            {
                Coins = coins;
            }

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

        [Test]
        public void Buy_AddsWholeBatch_AndSpendsCoinsOnce()
        {
            var coins = new FakeCoinService(500);
            var boosters = new FakeBoosterInventory(1);

            var bought = BuyBoosterWithCoinsUseCase.Execute(coins, boosters, Boosters.A,
                coinCost: 200, amount: 3);

            Assert.AreEqual(3, bought);
            Assert.AreEqual(300, coins.Coins);
            Assert.AreEqual(4, boosters.GetCount(Boosters.A));
        }

        [Test]
        public void Buy_WithoutEnoughCoins_TouchesNeitherWallet()
        {
            var coins = new FakeCoinService(199);
            var boosters = new FakeBoosterInventory(1);

            var bought = BuyBoosterWithCoinsUseCase.Execute(coins, boosters, Boosters.A,
                coinCost: 200, amount: 3);

            Assert.AreEqual(0, bought);
            Assert.AreEqual(199, coins.Coins);
            Assert.AreEqual(1, boosters.GetCount(Boosters.A), "Không đủ tiền thì kho không được đổi.");
            Assert.AreEqual(0, boosters.ChangeCount);
        }

        [Test]
        public void Buy_WithZeroCost_IsDisabled()
        {
            var coins = new FakeCoinService(500);
            var boosters = new FakeBoosterInventory(0);

            var bought = BuyBoosterWithCoinsUseCase.Execute(coins, boosters, Boosters.A,
                coinCost: 0, amount: 3);

            Assert.AreEqual(0, bought, "Giá 0 là cách tắt đường mua của một loại, không phải cho không.");
            Assert.AreEqual(500, coins.Coins);
        }
    }
}
