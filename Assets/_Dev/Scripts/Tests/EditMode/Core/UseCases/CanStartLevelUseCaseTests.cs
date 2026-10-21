using System;
using NUnit.Framework;
using Game.Core.Services;
using Game.Core.UseCases;

namespace Game.Core.Tests.UseCases
{
    public class CanStartLevelUseCaseTests
    {
        sealed class FakeLifeService : ILifeService
        {
            public int Lives { get; set; }
            public int MaxLives { get; } = 5;
            public TimeSpan TimeUntilNextLife => TimeSpan.Zero;
            public int RefreshCount { get; private set; }
            public event Action<int> LivesChanged;
            public TimeSpan UnlimitedTimeLeft { get; set; }
            public event Action<bool> UnlimitedChanged { add { } remove { } }

            public void Add(int amount) { }
            public bool TrySpend(int amount) => false;
            public void AddUnlimited(TimeSpan duration) { }

            public void Refresh()
            {
                RefreshCount++;
                LivesChanged?.Invoke(Lives);
            }
        }

        [Test]
        public void Execute_HasLives_ReturnsTrue()
        {
            var lives = new FakeLifeService { Lives = 1 };

            Assert.IsTrue(CanStartLevelUseCase.Execute(lives));
        }

        [Test]
        public void Execute_NoLives_ReturnsFalse()
        {
            var lives = new FakeLifeService { Lives = 0 };

            Assert.IsFalse(CanStartLevelUseCase.Execute(lives));
        }

        [Test]
        public void Execute_NoLives_UnlimitedRunning_ReturnsTrue()
        {
            var lives = new FakeLifeService { Lives = 0, UnlimitedTimeLeft = TimeSpan.FromMinutes(5) };

            Assert.IsTrue(CanStartLevelUseCase.Execute(lives));
        }

        [Test]
        public void Execute_RefreshesBeforeChecking()
        {
            var lives = new FakeLifeService { Lives = 0 };

            CanStartLevelUseCase.Execute(lives);

            Assert.AreEqual(1, lives.RefreshCount, "Phải bắt kịp tim vừa hồi xong trước khi chặn oan.");
        }

        [Test]
        public void Execute_NullService_ReturnsTrue_AndDoesNotThrow()
        {
            Assert.IsTrue(CanStartLevelUseCase.Execute(null), "Thiếu hệ tim thì không chặn gameplay.");
        }
    }
}
