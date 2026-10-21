using System;
using NUnit.Framework;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class ActiveDayRulesTests
    {
        // UTC+7, múi giờ của phần lớn người chơi.
        const int OffsetMinutes = 7 * 60;

        static readonly DateTime FirstOpen = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        static int Day(DateTime nowUtc) => ActiveDayRules.DayIndex(FirstOpen, OffsetMinutes, nowUtc);

        [Test]
        public void DayIndex_SameMomentAsFirstOpen_IsZero()
        {
            Assert.AreEqual(0, Day(FirstOpen));
        }

        [Test]
        public void DayIndex_LaterTheSameLocalDay_StaysZero()
        {
            Assert.AreEqual(0, Day(FirstOpen + TimeSpan.FromHours(4)),
                "Cùng một ngày lịch thì vẫn là D0, dù cách nhau mấy tiếng.");
        }

        [Test]
        public void DayIndex_TwoMinutesApartAcrossLocalMidnight_IsOne()
        {
            // 16:59 UTC = 23:59 giờ local (UTC+7); thêm 2 phút là đã sang ngày mới ở local.
            var justBeforeMidnight = new DateTime(2026, 1, 1, 16, 59, 0, DateTimeKind.Utc);
            var justAfterMidnight = justBeforeMidnight + TimeSpan.FromMinutes(2);

            Assert.AreEqual(0, ActiveDayRules.DayIndex(justBeforeMidnight, OffsetMinutes, justBeforeMidnight));
            Assert.AreEqual(1, ActiveDayRules.DayIndex(justBeforeMidnight, OffsetMinutes, justAfterMidnight),
                "Mốc là nửa đêm lịch địa phương, không phải đủ 24 giờ kể từ lúc cài.");
        }

        [Test]
        public void DayIndex_SkippedDays_CountsCalendarNotSessions()
        {
            Assert.AreEqual(7, Day(FirstOpen + TimeSpan.FromDays(7)),
                "Đếm số ngày lịch kể từ lúc cài, không phải số ngày có vào game.");
        }

        [Test]
        public void DayIndex_ClockSetBackwards_NeverGoesNegative()
        {
            Assert.AreEqual(0, Day(FirstOpen - TimeSpan.FromDays(30)));
        }

        [Test]
        public void DayIndex_AcrossMonthAndYearBoundary()
        {
            var installed = new DateTime(2025, 12, 31, 6, 0, 0, DateTimeKind.Utc);

            Assert.AreEqual(1, ActiveDayRules.DayIndex(installed, OffsetMinutes, installed + TimeSpan.FromDays(1)));
        }

        [Test]
        public void DayIndex_LeapDay()
        {
            var installed = new DateTime(2028, 2, 28, 6, 0, 0, DateTimeKind.Utc);

            Assert.AreEqual(2, ActiveDayRules.DayIndex(installed, OffsetMinutes, installed + TimeSpan.FromDays(2)),
                "29/02 là một ngày thật, không được bỏ qua.");
        }

        [Test]
        public void GuardFirstOpen_FirstEverOpen_ReturnsNow()
        {
            Assert.AreEqual(FirstOpen, ActiveDayRules.GuardFirstOpen(default, FirstOpen));
        }

        [Test]
        public void GuardFirstOpen_ClockRolledBack_KeepsTheStoredInstallMoment()
        {
            var rolledBack = FirstOpen - TimeSpan.FromDays(400);

            Assert.AreEqual(rolledBack, ActiveDayRules.GuardFirstOpen(FirstOpen, rolledBack),
                "Vặn giờ lùi thì mốc cài phải đi theo, nếu không ngày cài nằm ở tương lai và index âm.");
        }

        [Test]
        public void GuardFirstOpen_NormalCase_KeepsTheStoredValue()
        {
            Assert.AreEqual(FirstOpen, ActiveDayRules.GuardFirstOpen(FirstOpen, FirstOpen + TimeSpan.FromDays(3)));
        }
    }
}
