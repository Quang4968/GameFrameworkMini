using System;
using NUnit.Framework;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class LifeClockGuardTests
    {
        static readonly DateTime Now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Measure_NoAnchorYet_CountsNothing_AndSetsAnchor()
        {
            var (elapsed, anchor) = LifeClockGuard.Measure(DateTime.MinValue, Now);

            Assert.AreEqual(TimeSpan.Zero, elapsed, "Máy mới cài không được hồi tim theo cả quãng từ năm 0001.");
            Assert.AreEqual(Now, anchor);
        }

        [Test]
        public void Measure_TimeMovesForward_CountsElapsed_AndAdvancesAnchor()
        {
            var (elapsed, anchor) = LifeClockGuard.Measure(Now, Now + TimeSpan.FromMinutes(45));

            Assert.AreEqual(TimeSpan.FromMinutes(45), elapsed);
            Assert.AreEqual(Now + TimeSpan.FromMinutes(45), anchor);
        }

        [Test]
        public void Measure_ClockSetBackwards_CountsNothing_AndKeepsAnchor()
        {
            var (elapsed, anchor) = LifeClockGuard.Measure(Now, Now - TimeSpan.FromDays(365));

            Assert.AreEqual(TimeSpan.Zero, elapsed, "Vặn giờ lùi không được hồi tim.");
            Assert.AreEqual(Now, anchor, "Mốc phải giữ nguyên, nếu không lần sau sẽ tính lại cả quãng đã vặn.");
        }

        [Test]
        public void Measure_ClockJumpedForwardThenBack_FreezesUntilRealTimeCatchesUp()
        {
            // Vặn tới 1 ngày để lấy tim...
            var (cheated, anchor) = LifeClockGuard.Measure(Now, Now + TimeSpan.FromDays(1));
            Assert.AreEqual(TimeSpan.FromDays(1), cheated);

            // ...rồi chỉnh về giờ thật: mốc vẫn ở tương lai nên không hồi thêm được gì.
            var (afterReset, keptAnchor) = LifeClockGuard.Measure(anchor, Now + TimeSpan.FromMinutes(5));
            Assert.AreEqual(TimeSpan.Zero, afterReset);
            Assert.AreEqual(anchor, keptAnchor);

            // Chỉ khi giờ thật vượt qua mốc đã vặn thì đồng hồ mới chạy tiếp — phần "vay trước"
            // coi như đã trả xong.
            var (afterCatchUp, _) = LifeClockGuard.Measure(keptAnchor, Now + TimeSpan.FromDays(1) + TimeSpan.FromMinutes(10));
            Assert.AreEqual(TimeSpan.FromMinutes(10), afterCatchUp);
        }
    }
}
