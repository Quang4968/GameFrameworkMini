using System;
using System.Collections.Generic;
using NUnit.Framework;
using Game.Core.Domain.DailyReward;
using Game.Core.Services;
using Game.Core.UseCases;

namespace Game.Core.Tests.UseCases
{
    public class ClaimDailyRewardUseCaseTests
    {
        sealed class FakeDailyRewardService : IDailyRewardService
        {
            public int ClaimedCount { get; set; }
            public int LastClaimDay { get; set; } = -1;
            public int Today { get; set; } = 100;
            public event Action<int> ClaimedCountChanged;

            public void RecordClaim(int day)
            {
                ClaimedCount++;
                LastClaimDay = day;
                ClaimedCountChanged?.Invoke(ClaimedCount);
            }
        }

        sealed class FakeSchedule : IDailyRewardSchedule
        {
            public IReadOnlyList<DailyRewardDay> Days { get; }
            public int UnlockLevel => 0;

            public FakeSchedule(int count)
            {
                var days = new List<DailyRewardDay>();
                for (var i = 0; i < count; i++)
                    days.Add(new DailyRewardDay(new[] { new DailyRewardItem("coins", (i + 1) * 10) }));
                Days = days;
            }
        }

        sealed class FakeGranter : IDailyRewardGranter
        {
            public readonly List<(IReadOnlyList<DailyRewardItem> items, int slot)> Calls = new();

            public void Grant(IReadOnlyList<DailyRewardItem> items, int slot) => Calls.Add((items, slot));
        }

        [Test]
        public void Execute_FirstEverClaim_PaysDayOneAndRecordsToday()
        {
            var state = new FakeDailyRewardService();
            var granter = new FakeGranter();

            var slot = ClaimDailyRewardUseCase.Execute(state, new FakeSchedule(14), granter);

            Assert.AreEqual(0, slot);
            Assert.AreEqual(1, state.ClaimedCount);
            Assert.AreEqual(100, state.LastClaimDay);
            Assert.AreEqual(1, granter.Calls.Count);
            Assert.AreEqual(10, granter.Calls[0].items[0].Amount);
        }

        [Test]
        public void Execute_SameDayTwice_RefusesAndPaysNothing()
        {
            var state = new FakeDailyRewardService { ClaimedCount = 3, LastClaimDay = 100 };
            var granter = new FakeGranter();

            Assert.AreEqual(ClaimDailyRewardUseCase.NotClaimed,
                ClaimDailyRewardUseCase.Execute(state, new FakeSchedule(14), granter));
            Assert.AreEqual(3, state.ClaimedCount);
            Assert.IsEmpty(granter.Calls);
        }

        [Test]
        public void Execute_AfterDaysAway_PaysTheNextSlotNotASkippedOne()
        {
            var state = new FakeDailyRewardService { ClaimedCount = 3, LastClaimDay = 90, Today = 100 };
            var granter = new FakeGranter();

            Assert.AreEqual(3, ClaimDailyRewardUseCase.Execute(state, new FakeSchedule(14), granter));
            Assert.AreEqual(40, granter.Calls[0].items[0].Amount);
        }

        [Test]
        public void Execute_AfterTheLastDay_LoopsBackToDayOne()
        {
            var state = new FakeDailyRewardService { ClaimedCount = 14, LastClaimDay = 99 };

            Assert.AreEqual(0, ClaimDailyRewardUseCase.Execute(state, new FakeSchedule(14), new FakeGranter()));
            Assert.AreEqual(15, state.ClaimedCount);
        }

        [Test]
        public void Execute_NullStateOrSchedule_ReturnsNotClaimed_AndDoesNotThrow()
        {
            Assert.AreEqual(ClaimDailyRewardUseCase.NotClaimed,
                ClaimDailyRewardUseCase.Execute(null, new FakeSchedule(14), new FakeGranter()));
            Assert.AreEqual(ClaimDailyRewardUseCase.NotClaimed,
                ClaimDailyRewardUseCase.Execute(new FakeDailyRewardService(), null, new FakeGranter()));
        }

        [Test]
        public void Execute_EmptySchedule_RecordsNothing()
        {
            var state = new FakeDailyRewardService();

            Assert.AreEqual(ClaimDailyRewardUseCase.NotClaimed,
                ClaimDailyRewardUseCase.Execute(state, new FakeSchedule(0), new FakeGranter()));
            Assert.AreEqual(0, state.ClaimedCount);
        }

        [Test]
        public void Execute_NullGranter_StillRecordsTheClaim()
        {
            var state = new FakeDailyRewardService();

            Assert.AreEqual(0, ClaimDailyRewardUseCase.Execute(state, new FakeSchedule(14), null));
            Assert.AreEqual(1, state.ClaimedCount);
        }

        [Test]
        public void Execute_RaisesClaimedCountChangedOnce_WithNewValue()
        {
            var state = new FakeDailyRewardService { ClaimedCount = 4, LastClaimDay = 99 };
            var raised = 0;
            var last = -1;
            state.ClaimedCountChanged += value => { raised++; last = value; };

            ClaimDailyRewardUseCase.Execute(state, new FakeSchedule(14), new FakeGranter());

            Assert.AreEqual(1, raised);
            Assert.AreEqual(5, last);
        }
    }
}
