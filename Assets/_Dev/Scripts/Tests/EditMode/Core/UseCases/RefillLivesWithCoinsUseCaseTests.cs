using System;
using NUnit.Framework;
using Game.Core.Services;
using Game.Core.UseCases;

namespace Game.Core.Tests.UseCases
{
    public class RefillLivesWithCoinsUseCaseTests
    {
        sealed class FakeCoinService : ICoinService
        {
            public int Coins { get; private set; }
            public event Action<int> CoinsChanged;

            public FakeCoinService(int coins = 0) => Coins = coins;

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
            public TimeSpan UnlimitedTimeLeft => TimeSpan.Zero;
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

            public void AddUnlimited(TimeSpan duration) { }
        }

        // Nút mua và cú mua thật phải nhìn cùng một luật — nếu hai bên lệch nhau thì nút sáng mà
        // bấm không ăn (hoặc ngược lại).
        [Test]
        public void CanRefill_AgreesWithRefill_InEveryCase()
        {
            var cases = new[]
            {
                new { coins = 900, lives = 0, max = 5, cost = 900 },
                new { coins = 899, lives = 0, max = 5, cost = 900 },
                new { coins = 5000, lives = 5, max = 5, cost = 900 },
                new { coins = 5000, lives = 4, max = 5, cost = 900 },
                new { coins = 5000, lives = 0, max = 5, cost = 0 },
            };

            foreach (var c in cases)
            {
                var coins = new FakeCoinService(c.coins);
                var lives = new FakeLifeService(c.max, c.lives);

                var predicted = CanRefillLivesUseCase.Execute(coins, lives, c.cost);
                var actual = RefillLivesWithCoinsUseCase.Execute(coins, lives, c.cost) > 0;

                Assert.AreEqual(predicted, actual,
                    $"CanRefill và Refill lệch nhau ở coins={c.coins} lives={c.lives}/{c.max} cost={c.cost}");
            }
        }

        [Test]
        public void CanRefill_NullService_ReturnsFalse_AndDoesNotThrow()
        {
            Assert.IsFalse(CanRefillLivesUseCase.Execute(null, new FakeLifeService(5, 0), 900));
            Assert.IsFalse(CanRefillLivesUseCase.Execute(new FakeCoinService(5000), null, 900));
        }

        [Test]
        public void Execute_FromEmpty_FillsToMax()
        {
            var coins = new FakeCoinService(1000);
            var lives = new FakeLifeService(5, 0);

            Assert.AreEqual(5, RefillLivesWithCoinsUseCase.Execute(coins, lives, 900));
            Assert.AreEqual(5, lives.Lives);
            Assert.AreEqual(100, coins.Coins);
        }

        // Giá là MỘT LẦN ĐẦY BÌNH, không nhân theo số tim thiếu: thiếu 1 tim vẫn trả đúng 900.
        [Test]
        public void Execute_MissingOneLife_CostsTheSameAsAFullTank()
        {
            var coins = new FakeCoinService(1000);
            var lives = new FakeLifeService(5, 4);

            Assert.AreEqual(1, RefillLivesWithCoinsUseCase.Execute(coins, lives, 900));
            Assert.AreEqual(5, lives.Lives);
            Assert.AreEqual(100, coins.Coins, "Thiếu 1 tim hay thiếu cả 5 đều trả bằng nhau.");
        }

        [Test]
        public void Execute_NotEnoughCoins_Fails_AndLeavesLivesUntouched()
        {
            var coins = new FakeCoinService(899);
            var lives = new FakeLifeService(5, 0);

            Assert.AreEqual(0, RefillLivesWithCoinsUseCase.Execute(coins, lives, 900));
            Assert.AreEqual(0, lives.Lives);
            Assert.AreEqual(899, coins.Coins, "Không đủ tiền thì không được trừ đồng nào.");
        }

        [Test]
        public void Execute_AlreadyFullLives_Fails_AndDoesNotSpendCoins()
        {
            var coins = new FakeCoinService(1000);
            var lives = new FakeLifeService(5, 5);

            Assert.AreEqual(0, RefillLivesWithCoinsUseCase.Execute(coins, lives, 900));
            Assert.AreEqual(1000, coins.Coins, "Đầy tim rồi thì không cho mua tốn coin oan.");
        }

        [Test]
        public void Execute_RefreshesLivesBeforeChecking()
        {
            var coins = new FakeCoinService(1000);
            var lives = new FakeLifeService(5, 0);

            RefillLivesWithCoinsUseCase.Execute(coins, lives, 900);

            Assert.GreaterOrEqual(lives.RefreshCount, 1,
                "Tim có thể vừa hồi trong lúc popup đang mở — phải đọc số mới nhất trước khi trừ tiền.");
        }

        [Test]
        public void Execute_NonPositiveCost_Fails()
        {
            var coins = new FakeCoinService(1000);
            var lives = new FakeLifeService(5, 0);

            Assert.AreEqual(0, RefillLivesWithCoinsUseCase.Execute(coins, lives, 0));
            Assert.AreEqual(0, lives.Lives, "Giá 0 là tắt tính năng mua, không phải cho không.");
        }

        [Test]
        public void Execute_NullService_ReturnsZero_AndDoesNotThrow()
        {
            Assert.AreEqual(0, RefillLivesWithCoinsUseCase.Execute(null, new FakeLifeService(5, 0), 900));
            Assert.AreEqual(0, RefillLivesWithCoinsUseCase.Execute(new FakeCoinService(1000), null, 900));
        }
    }
}
