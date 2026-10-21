using System;
using NUnit.Framework;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class UnlimitedLifeRulesTests
    {
        static readonly DateTime Now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        static readonly TimeSpan OneHour = TimeSpan.FromHours(1);

        [Test]
        public void TimeLeft_NoWindowEver_IsZero()
        {
            Assert.AreEqual(TimeSpan.Zero, UnlimitedLifeRules.TimeLeft(DateTime.MinValue, Now));
        }

        [Test]
        public void TimeLeft_AppClosedMidWindow_StillCountsRealTime()
        {
            var end = Now + OneHour;

            Assert.AreEqual(TimeSpan.FromMinutes(20), UnlimitedLifeRules.TimeLeft(end, Now + TimeSpan.FromMinutes(40)));
        }

        [Test]
        public void TimeLeft_AppClosedPastTheEnd_IsZero()
        {
            var end = Now + OneHour;

            Assert.AreEqual(TimeSpan.Zero, UnlimitedLifeRules.TimeLeft(end, Now + TimeSpan.FromHours(2)));
        }

        [Test]
        public void Extend_FirstGrant_StartsNow()
        {
            Assert.AreEqual(Now + OneHour, UnlimitedLifeRules.Extend(DateTime.MinValue, Now, OneHour));
        }

        [Test]
        public void Extend_WhileRunning_StacksOntoTheEnd()
        {
            var end = Now + TimeSpan.FromMinutes(30);

            Assert.AreEqual(end + OneHour, UnlimitedLifeRules.Extend(end, Now, OneHour));
        }

        [Test]
        public void Extend_AfterItRanOut_StartsNow()
        {
            var end = Now - TimeSpan.FromMinutes(30);

            Assert.AreEqual(Now + OneHour, UnlimitedLifeRules.Extend(end, Now, OneHour));
        }

        [Test]
        public void Extend_NonPositiveDuration_KeepsTheEnd()
        {
            var end = Now + TimeSpan.FromMinutes(30);

            Assert.AreEqual(end, UnlimitedLifeRules.Extend(end, Now, TimeSpan.Zero));
            Assert.AreEqual(end, UnlimitedLifeRules.Extend(end, Now, -OneHour));
        }

        [Test]
        public void Extend_PastTheLastRepresentableDate_Clamps()
        {
            var end = DateTime.MaxValue - TimeSpan.FromMinutes(1);

            Assert.AreEqual(DateTime.MaxValue, UnlimitedLifeRules.Extend(end, Now, OneHour));
        }
    }
}
