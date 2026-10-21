using System;
using NUnit.Framework;
using Game.Core.Services;
using Game.Core.UseCases;

namespace Game.Core.Tests.UseCases
{
    public class LeavingLevelCostsLifeUseCaseTests
    {
        sealed class FakeLifeService : ILifeService
        {
            public int Lives { get; set; }
            public int MaxLives { get; } = 5;
            public TimeSpan TimeUntilNextLife => TimeSpan.Zero;
            public TimeSpan UnlimitedTimeLeft { get; set; }
            public TimeSpan? UnlimitedAfterRefresh { get; set; }
            public event Action<int> LivesChanged { add { } remove { } }
            public event Action<bool> UnlimitedChanged { add { } remove { } }

            public void Add(int amount) { }
            public bool TrySpend(int amount) => false;
            public void AddUnlimited(TimeSpan duration) { }

            public void Refresh()
            {
                if (UnlimitedAfterRefresh.HasValue) UnlimitedTimeLeft = UnlimitedAfterRefresh.Value;
            }
        }

        [Test]
        public void Execute_NoUnlimited_CostsLife()
        {
            Assert.IsTrue(LeavingLevelCostsLifeUseCase.Execute(new FakeLifeService { Lives = 3 }));
        }

        [Test]
        public void Execute_UnlimitedRunning_IsFree()
        {
            var lives = new FakeLifeService { Lives = 3, UnlimitedTimeLeft = TimeSpan.FromMinutes(10) };

            Assert.IsFalse(LeavingLevelCostsLifeUseCase.Execute(lives));
        }

        [Test]
        public void Execute_WindowRanOutSinceLastRefresh_CostsLife()
        {
            var lives = new FakeLifeService
            {
                UnlimitedTimeLeft = TimeSpan.FromSeconds(1), UnlimitedAfterRefresh = TimeSpan.Zero,
            };

            Assert.IsTrue(LeavingLevelCostsLifeUseCase.Execute(lives));
        }

        [Test]
        public void Execute_NullService_StillAsks()
        {
            Assert.IsTrue(LeavingLevelCostsLifeUseCase.Execute(null));
        }
    }
}
