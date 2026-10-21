using System;
using NUnit.Framework;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class LifeRegenRulesTests
    {
        static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
        static readonly TimeSpan Zero = TimeSpan.Zero;

        [Test]
        public void Apply_FullLives_NeverStartsTimer()
        {
            var (lives, remaining) = LifeRegenRules.Apply(5, 5, Zero, TimeSpan.FromHours(10), Interval);

            Assert.AreEqual(5, lives);
            Assert.AreEqual(Zero, remaining, "Đầy tim thì không có đồng hồ nào chạy.");
        }

        [Test]
        public void Apply_MissingLives_NoTimerYet_StartsFullInterval()
        {
            var (lives, remaining) = LifeRegenRules.Apply(4, 5, Zero, Zero, Interval);

            Assert.AreEqual(4, lives);
            Assert.AreEqual(Interval, remaining, "Vừa thiếu tim thì phải chờ trọn một khoảng.");
        }

        [Test]
        public void Apply_TimerNotDueYet_JustCountsDown()
        {
            var (lives, remaining) = LifeRegenRules.Apply(4, 5, Interval, TimeSpan.FromMinutes(10), Interval);

            Assert.AreEqual(4, lives);
            Assert.AreEqual(TimeSpan.FromMinutes(20), remaining);
        }

        [Test]
        public void Apply_TimerDue_AddsOneLife_AndStopsAtMax()
        {
            var (lives, remaining) = LifeRegenRules.Apply(4, 5, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), Interval);

            Assert.AreEqual(5, lives);
            Assert.AreEqual(Zero, remaining, "Vừa chạm trần thì tắt đồng hồ luôn.");
        }

        [Test]
        public void Apply_OfflineForSeveralIntervals_CatchesUpMultipleLives()
        {
            // Còn 10 phút nữa có tim, rồi offline 75 phút: tim thứ nhất ở phút 10, thứ hai ở phút
            // 40, thứ ba ở phút 70 — và còn dư 25 phút của khoảng kế tiếp.
            var (lives, remaining) = LifeRegenRules.Apply(
                1, 5, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(75), Interval);

            Assert.AreEqual(4, lives, "Offline qua nhiều khoảng thì hồi đúng số tim đã trôi qua.");
            Assert.AreEqual(TimeSpan.FromMinutes(25), remaining,
                "Phần dư nối đúng nhịp cũ, không reset về trọn một khoảng.");
        }

        [Test]
        public void Apply_OfflineLongEnoughToFillUp_ClampsAtMax_AndStopsTimer()
        {
            var (lives, remaining) = LifeRegenRules.Apply(1, 5, Interval, TimeSpan.FromDays(1), Interval);

            Assert.AreEqual(5, lives);
            Assert.AreEqual(Zero, remaining);
        }

        [Test]
        public void Apply_ZeroInterval_DisablesRegen()
        {
            var (lives, remaining) = LifeRegenRules.Apply(1, 5, Zero, TimeSpan.FromDays(1), Zero);

            Assert.AreEqual(1, lives, "refillInterval = 0 là tắt hồi tim.");
            Assert.AreEqual(Zero, remaining);
        }

        [Test]
        public void Apply_LivesAboveMax_ClampsDownToMax()
        {
            var (lives, remaining) = LifeRegenRules.Apply(9, 5, Zero, Zero, Interval);

            Assert.AreEqual(5, lives, "Hạ trần trong asset thì số dư cũ phải bị cắt bớt.");
            Assert.AreEqual(Zero, remaining);
        }

        [Test]
        public void Apply_NoElapsedTime_LeavesTimerUntouched()
        {
            var (lives, remaining) = LifeRegenRules.Apply(2, 5, TimeSpan.FromMinutes(7), Zero, Interval);

            Assert.AreEqual(2, lives);
            Assert.AreEqual(TimeSpan.FromMinutes(7), remaining,
                "Gọi lại lúc chưa trôi giây nào không được đẩy đồng hồ đi.");
        }

        [Test]
        public void TimeUntilFull_FullLives_IsZero()
        {
            Assert.AreEqual(Zero, LifeRegenRules.TimeUntilFull(5, 5, Zero, Interval));
            Assert.AreEqual(Zero, LifeRegenRules.TimeUntilFull(9, 5, Zero, Interval));
        }

        [Test]
        public void TimeUntilFull_OneMissing_IsTheRunningTimer()
        {
            Assert.AreEqual(TimeSpan.FromMinutes(12), LifeRegenRules.TimeUntilFull(4, 5, TimeSpan.FromMinutes(12), Interval));
        }

        [Test]
        public void TimeUntilFull_SeveralMissing_AddsWholeIntervals()
        {
            Assert.AreEqual(TimeSpan.FromMinutes(70), LifeRegenRules.TimeUntilFull(2, 5, TimeSpan.FromMinutes(10), Interval));
            Assert.AreEqual(TimeSpan.FromMinutes(125), LifeRegenRules.TimeUntilFull(0, 5, TimeSpan.FromMinutes(5), Interval));
        }

        [Test]
        public void TimeUntilFull_TimerNotStarted_CountsAFullInterval()
        {
            Assert.AreEqual(Interval, LifeRegenRules.TimeUntilFull(4, 5, Zero, Interval));
        }

        [Test]
        public void TimeUntilFull_RegenOff_IsZero()
        {
            Assert.AreEqual(Zero, LifeRegenRules.TimeUntilFull(1, 5, TimeSpan.FromMinutes(3), Zero),
                "With regen off the lives never fill, so there is nothing to notify.");
        }
    }
}
