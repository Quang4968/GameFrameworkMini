using System;
using System.Collections.Generic;
using NUnit.Framework;
using Game.Core.Domain.DailyReward;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class DailyRewardRulesTests
    {
        const int Cycle = 14;
        static readonly TimeSpan Utc7 = TimeSpan.FromHours(7);

        static DailyRewardDay DayWith(string id, int amount) =>
            new DailyRewardDay(new[] { new DailyRewardItem(id, amount) });

        static List<DailyRewardDay> Days(int count)
        {
            var days = new List<DailyRewardDay>();
            for (var i = 0; i < count; i++) days.Add(DayWith("coins", 10));
            return days;
        }

        [Test]
        public void DayNumber_RollsAtLocalMidnight_NotAtUtcMidnight()
        {
            // 16:59 and 17:00 UTC straddle midnight in UTC+7.
            var before = DailyRewardRules.DayNumber(new DateTime(2026, 9, 24, 16, 59, 0, DateTimeKind.Utc), Utc7);
            var after = DailyRewardRules.DayNumber(new DateTime(2026, 9, 24, 17, 0, 0, DateTimeKind.Utc), Utc7);

            Assert.AreEqual(before + 1, after);
        }

        [Test]
        public void DayNumber_SameLocalDay_IsEqualAcrossHours()
        {
            var morning = DailyRewardRules.DayNumber(new DateTime(2026, 9, 24, 1, 0, 0, DateTimeKind.Utc), Utc7);
            var evening = DailyRewardRules.DayNumber(new DateTime(2026, 9, 24, 16, 0, 0, DateTimeKind.Utc), Utc7);

            Assert.AreEqual(morning, evening);
        }

        [Test]
        public void CanClaim_NeverClaimed_IsTrue() => Assert.IsTrue(DailyRewardRules.CanClaim(-1, 100));

        [Test]
        public void CanClaim_SameDay_IsFalse() => Assert.IsFalse(DailyRewardRules.CanClaim(100, 100));

        [Test]
        public void CanClaim_NextDay_IsTrue() => Assert.IsTrue(DailyRewardRules.CanClaim(100, 101));

        [Test]
        public void CanClaim_ClockWoundBack_IsFalse() => Assert.IsFalse(DailyRewardRules.CanClaim(100, 99));

        [Test]
        public void NextSlot_CountsClaimsNotDays_AndWrapsAfterTheCycle()
        {
            Assert.AreEqual(0, DailyRewardRules.NextSlot(0, Cycle));
            Assert.AreEqual(5, DailyRewardRules.NextSlot(5, Cycle));
            Assert.AreEqual(13, DailyRewardRules.NextSlot(13, Cycle));
            Assert.AreEqual(0, DailyRewardRules.NextSlot(14, Cycle), "Day 7 of the even week loops to day 1 of the odd week.");
            Assert.AreEqual(7, DailyRewardRules.NextSlot(21, Cycle));
        }

        [Test]
        public void FocusSlot_AfterTodaysClaim_StaysOnTheLastSlotTaken()
        {
            Assert.AreEqual(7, DailyRewardRules.FocusSlot(7, Cycle, canClaim: true));
            Assert.AreEqual(6, DailyRewardRules.FocusSlot(7, Cycle, canClaim: false));
            Assert.AreEqual(13, DailyRewardRules.FocusSlot(14, Cycle, canClaim: false));
        }

        [Test]
        public void PageStart_SplitsTheCycleIntoWeeks()
        {
            Assert.AreEqual(0, DailyRewardRules.PageStart(0));
            Assert.AreEqual(0, DailyRewardRules.PageStart(6));
            Assert.AreEqual(7, DailyRewardRules.PageStart(7));
            Assert.AreEqual(7, DailyRewardRules.PageStart(13));
        }

        [Test]
        public void StateOf_ClaimableDay_MarksEarlierClaimedAndLaterLocked()
        {
            Assert.AreEqual(DailyRewardDayState.Claimed, DailyRewardRules.StateOf(0, 2, Cycle, true));
            Assert.AreEqual(DailyRewardDayState.Claimed, DailyRewardRules.StateOf(1, 2, Cycle, true));
            Assert.AreEqual(DailyRewardDayState.Claimable, DailyRewardRules.StateOf(2, 2, Cycle, true));
            Assert.AreEqual(DailyRewardDayState.Locked, DailyRewardRules.StateOf(3, 2, Cycle, true));
        }

        [Test]
        public void StateOf_TodayAlreadyClaimed_HasNoClaimableDay()
        {
            Assert.AreEqual(DailyRewardDayState.Claimed, DailyRewardRules.StateOf(1, 2, Cycle, false));
            Assert.AreEqual(DailyRewardDayState.Locked, DailyRewardRules.StateOf(2, 2, Cycle, false));
        }

        [Test]
        public void StateOf_CycleJustCompleted_ShowsTheWholeLastWeekClaimed()
        {
            for (var slot = 7; slot < Cycle; slot++)
                Assert.AreEqual(DailyRewardDayState.Claimed, DailyRewardRules.StateOf(slot, 14, Cycle, false));
        }

        [Test]
        public void StateOf_NewPassAfterFullCycle_StartsOverAtDayOne()
        {
            Assert.AreEqual(DailyRewardDayState.Claimable, DailyRewardRules.StateOf(0, 14, Cycle, true));
            Assert.AreEqual(DailyRewardDayState.Locked, DailyRewardRules.StateOf(1, 14, Cycle, true));
        }

        [Test]
        public void StateOf_FirstEverOpen_OnlyDayOneIsClaimable()
        {
            Assert.AreEqual(DailyRewardDayState.Claimable, DailyRewardRules.StateOf(0, 0, Cycle, true));
            Assert.AreEqual(DailyRewardDayState.Locked, DailyRewardRules.StateOf(1, 0, Cycle, true));
        }

        [Test]
        public void IsUnlocked_ZeroMeansAlways_OtherwiseNeedsTheLevel()
        {
            Assert.IsTrue(DailyRewardRules.IsUnlocked(1, 0));
            Assert.IsFalse(DailyRewardRules.IsUnlocked(2, 3));
            Assert.IsTrue(DailyRewardRules.IsUnlocked(3, 3));
        }

        [Test]
        public void ShouldAutoOpen_OnlyOncePerLaunch_AndOnlyWithSomethingToClaim()
        {
            Assert.IsTrue(DailyRewardRules.ShouldAutoOpen(false, true, true));
            Assert.IsFalse(DailyRewardRules.ShouldAutoOpen(true, true, true), "Closed unclaimed: not again this launch.");
            Assert.IsFalse(DailyRewardRules.ShouldAutoOpen(false, true, false));
            Assert.IsFalse(DailyRewardRules.ShouldAutoOpen(false, false, true));
        }

        [Test]
        public void IsValidCycle_WholeWeeksWithItems_IsTrue() => Assert.IsTrue(DailyRewardRules.IsValidCycle(Days(14)));

        [Test]
        public void IsValidCycle_PartialWeek_IsFalse() => Assert.IsFalse(DailyRewardRules.IsValidCycle(Days(10)));

        [Test]
        public void IsValidCycle_EmptyOrNull_IsFalse()
        {
            Assert.IsFalse(DailyRewardRules.IsValidCycle(null));
            Assert.IsFalse(DailyRewardRules.IsValidCycle(Days(0)));
        }

        [Test]
        public void IsValidCycle_DayWithoutItemsOrWithBadItem_IsFalse()
        {
            var empty = Days(7);
            empty[3] = new DailyRewardDay(Array.Empty<DailyRewardItem>());
            Assert.IsFalse(DailyRewardRules.IsValidCycle(empty));

            var zero = Days(7);
            zero[2] = DayWith("coins", 0);
            Assert.IsFalse(DailyRewardRules.IsValidCycle(zero));

            var noId = Days(7);
            noId[1] = DayWith("", 5);
            Assert.IsFalse(DailyRewardRules.IsValidCycle(noId));
        }
    }
}
