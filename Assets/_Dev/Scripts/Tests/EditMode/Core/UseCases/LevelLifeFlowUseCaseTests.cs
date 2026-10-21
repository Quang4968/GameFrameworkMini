using System;
using NUnit.Framework;
using Game.Core.Services;
using Game.Core.UseCases;

namespace Game.Core.Tests.UseCases
{
    // Vào màn trừ tim, thắng hoàn lại — hai use case này chỉ có nghĩa khi đi cùng nhau nên test
    // chung một chỗ, đúng cái vòng người chơi thật sự đi qua.
    public class LevelLifeFlowUseCaseTests
    {
        sealed class FakeLifeService : ILifeService
        {
            public int Lives { get; set; }
            public int MaxLives { get; set; } = 5;
            public TimeSpan TimeUntilNextLife => TimeSpan.Zero;
            public event Action<int> LivesChanged;
            public TimeSpan UnlimitedTimeLeft { get; set; }
            public event Action<bool> UnlimitedChanged { add { } remove { } }

            // What UnlimitedTimeLeft becomes on the next Refresh, to model a window running out.
            public TimeSpan? UnlimitedAfterRefresh { get; set; }

            public void Add(int amount)
            {
                Lives = Math.Min(Lives + amount, MaxLives);
                LivesChanged?.Invoke(Lives);
            }

            public bool TrySpend(int amount)
            {
                if (Lives < amount) return false;

                Lives -= amount;
                LivesChanged?.Invoke(Lives);
                return true;
            }

            public void AddUnlimited(TimeSpan duration) => UnlimitedTimeLeft += duration;

            public void Refresh()
            {
                if (UnlimitedAfterRefresh.HasValue) UnlimitedTimeLeft = UnlimitedAfterRefresh.Value;
            }
        }

        [Test]
        public void Start_SpendsLife_AndReturnsSpentAmount()
        {
            var lives = new FakeLifeService { Lives = 5 };

            Assert.AreEqual(1, SpendLifeOnLevelStartUseCase.Execute(lives, 1));
            Assert.AreEqual(4, lives.Lives);
        }

        [Test]
        public void StartThenWin_LeavesLivesUnchanged()
        {
            var lives = new FakeLifeService { Lives = 3 };

            SpendLifeOnLevelStartUseCase.Execute(lives, 1);
            Assert.AreEqual(2, lives.Lives, "Đang chơi thì tim đã bị trừ rồi.");

            RefundLifeOnWinUseCase.Execute(lives, 1);
            Assert.AreEqual(3, lives.Lives, "Thắng thì được hoàn đúng số đã trả.");
        }

        [Test]
        public void StartWithoutWin_LosesLife()
        {
            var lives = new FakeLifeService { Lives = 3 };

            // Thua hoặc bỏ ngang = không có cú hoàn nào chạy.
            SpendLifeOnLevelStartUseCase.Execute(lives, 1);

            Assert.AreEqual(2, lives.Lives);
        }

        [Test]
        public void FiveStartsWithoutWin_EmptiesLives()
        {
            var lives = new FakeLifeService { Lives = 5 };

            for (var i = 0; i < 5; i++) SpendLifeOnLevelStartUseCase.Execute(lives, 1);

            Assert.AreEqual(0, lives.Lives);
        }

        [Test]
        public void Start_WhenOutOfLives_ReturnsZero_AndLeavesLivesAtZero()
        {
            var lives = new FakeLifeService { Lives = 0 };

            Assert.AreEqual(0, SpendLifeOnLevelStartUseCase.Execute(lives, 1));
            Assert.AreEqual(0, lives.Lives, "Không bao giờ trừ xuống âm.");
        }

        [Test]
        public void Refund_WhenAlreadyFull_AddsNothing()
        {
            var lives = new FakeLifeService { Lives = 5 };

            Assert.AreEqual(0, RefundLifeOnWinUseCase.Execute(lives, 1), "Đầy tim thì không cộng lố.");
            Assert.AreEqual(5, lives.Lives);
        }

        [Test]
        public void NonPositiveCost_DoesNothing()
        {
            var lives = new FakeLifeService { Lives = 3 };

            Assert.AreEqual(0, SpendLifeOnLevelStartUseCase.Execute(lives, 0));
            Assert.AreEqual(0, RefundLifeOnWinUseCase.Execute(lives, 0));
            Assert.AreEqual(3, lives.Lives);
        }

        [Test]
        public void Start_DuringUnlimited_ChargesNothing()
        {
            var lives = new FakeLifeService { Lives = 0, UnlimitedTimeLeft = TimeSpan.FromMinutes(10) };

            Assert.AreEqual(0, SpendLifeOnLevelStartUseCase.Execute(lives, 1));
            Assert.AreEqual(0, lives.Lives);
        }

        [Test]
        public void Start_WindowRanOutSinceLastRefresh_Charges()
        {
            var lives = new FakeLifeService
            {
                Lives = 3, UnlimitedTimeLeft = TimeSpan.FromSeconds(1), UnlimitedAfterRefresh = TimeSpan.Zero,
            };

            Assert.AreEqual(1, SpendLifeOnLevelStartUseCase.Execute(lives, 1));
            Assert.AreEqual(2, lives.Lives);
        }

        [Test]
        public void FreeStart_WindowRunsOutMidLevel_WinMintsNothing()
        {
            var lives = new FakeLifeService { Lives = 3, UnlimitedTimeLeft = TimeSpan.FromMinutes(1) };

            var charged = SpendLifeOnLevelStartUseCase.Execute(lives, 1);
            lives.UnlimitedTimeLeft = TimeSpan.Zero;

            Assert.AreEqual(0, RefundLifeOnWinUseCase.Execute(lives, charged));
            Assert.AreEqual(3, lives.Lives);
        }

        [Test]
        public void PaidStart_UnlimitedBoughtMidLevel_WinStillRefunds()
        {
            var lives = new FakeLifeService { Lives = 3 };

            var charged = SpendLifeOnLevelStartUseCase.Execute(lives, 1);
            lives.AddUnlimited(TimeSpan.FromHours(1));

            Assert.AreEqual(1, RefundLifeOnWinUseCase.Execute(lives, charged));
            Assert.AreEqual(3, lives.Lives);
        }

        [Test]
        public void NullService_DoesNotThrow()
        {
            Assert.AreEqual(0, SpendLifeOnLevelStartUseCase.Execute(null, 1));
            Assert.AreEqual(0, RefundLifeOnWinUseCase.Execute(null, 1));
        }
    }
}
