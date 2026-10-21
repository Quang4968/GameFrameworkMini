using System;
using NUnit.Framework;
using Game.Core.Services;
using Game.Core.UseCases;

namespace Game.Core.Tests.UseCases
{
    public class AwardWinCoinsUseCaseTests
    {
        // Ví giả: giữ đúng hai luật của ICoinService (không cộng số <= 0, không tiêu quá số dư)
        // để test bắt được nếu use case đẩy số âm xuống service.
        sealed class FakeCoinService : ICoinService
        {
            public int Coins { get; private set; }
            public int ChangeCount { get; private set; }

            public event Action<int> CoinsChanged;

            public void Add(int amount)
            {
                if (amount <= 0) return;

                Coins += amount;
                ChangeCount++;
                CoinsChanged?.Invoke(Coins);
            }

            public bool TrySpend(int amount)
            {
                if (amount <= 0 || Coins < amount) return false;

                Coins -= amount;
                ChangeCount++;
                CoinsChanged?.Invoke(Coins);
                return true;
            }
        }

        [Test]
        public void Execute_AddsReward_AndReturnsAwardedAmount()
        {
            var coins = new FakeCoinService();

            var awarded = AwardWinCoinsUseCase.Execute(coins, 30);

            Assert.AreEqual(30, awarded, "Trả về đúng số coin vừa cộng để popup hiện '+30'.");
            Assert.AreEqual(30, coins.Coins);
            Assert.AreEqual(1, coins.ChangeCount, "Chỉ được đụng vào ví đúng một lần.");
        }

        [Test]
        public void Execute_TwoWins_Accumulate()
        {
            var coins = new FakeCoinService();

            AwardWinCoinsUseCase.Execute(coins, 30);
            AwardWinCoinsUseCase.Execute(coins, 30);

            Assert.AreEqual(60, coins.Coins, "Thắng lại màn cũ vẫn được thưởng tiếp.");
        }

        [Test]
        public void Execute_NonPositiveReward_DoesNothing()
        {
            var coins = new FakeCoinService();

            Assert.AreEqual(0, AwardWinCoinsUseCase.Execute(coins, 0));
            Assert.AreEqual(0, AwardWinCoinsUseCase.Execute(coins, -10));
            Assert.AreEqual(0, coins.Coins);
            Assert.AreEqual(0, coins.ChangeCount);
        }

        [Test]
        public void Execute_NullService_ReturnsZero_AndDoesNotThrow()
        {
            Assert.AreEqual(0, AwardWinCoinsUseCase.Execute(null, 30));
        }
    }
}
