using NUnit.Framework;
using Game.Core.GameLogic;
using Game.Game.Services;

namespace Game.Core.Tests.Game.Services
{
    // The shipped cycle is the fallback for every broken remote value, so it must parse on its own:
    // two weeks, a chest on odd day 3 and even day 5, day 7 of each week paying three items.
    public class RemoteDailyRewardScheduleTests
    {
        [Test]
        public void TryParse_ShippedDefault_IsAValidTwoWeekCycle()
        {
            Assert.IsTrue(RemoteDailyRewardSchedule.TryParse(DailyRewardRemoteConfig.Cycle.Default, out var days, out var unlock));

            Assert.AreEqual(14, days.Count);
            Assert.AreEqual(0, unlock);
            Assert.AreEqual(3, days[6].Items.Count, "Odd week day 7 pays three items.");
            Assert.AreEqual(3, days[13].Items.Count, "Even week day 7 pays three items.");

            for (var slot = 0; slot < days.Count; slot++)
                Assert.AreEqual(slot == 2 || slot == 11, days[slot].IsChest, $"Slot {slot}");

            foreach (var day in days)
                foreach (var item in day.Items)
                    Assert.IsTrue(DailyRewardPayoutRules.TryToShopReward(item, out _), $"'{item.Id}' pays nothing.");
        }

        [Test]
        public void TryParse_PartialWeek_IsRejected()
        {
            const string json = "{\"days\":[{\"items\":[{\"id\":\"coins\",\"amount\":10}]}]}";

            Assert.IsFalse(RemoteDailyRewardSchedule.TryParse(json, out _, out _));
        }

        [Test]
        public void TryParse_NotJson_IsRejected()
        {
            Assert.IsFalse(RemoteDailyRewardSchedule.TryParse("not json", out _, out _));
            Assert.IsFalse(RemoteDailyRewardSchedule.TryParse("", out _, out _));
        }

        [Test]
        public void TryParse_ChestFlagMissing_IsAnOrdinaryDay()
        {
            var week = "";
            for (var i = 0; i < 7; i++)
                week += (i > 0 ? "," : "") + (i == 1 ? "{\"chest\":true," : "{") + "\"items\":[{\"id\":\"coins\",\"amount\":10}]}";

            Assert.IsTrue(RemoteDailyRewardSchedule.TryParse("{\"days\":[" + week + "]}", out var days, out _));
            Assert.IsFalse(days[0].IsChest);
            Assert.IsTrue(days[1].IsChest);
        }

        [Test]
        public void TryParse_NegativeUnlockLevel_ClampsToZero()
        {
            var week = "";
            for (var i = 0; i < 7; i++) week += (i > 0 ? "," : "") + "{\"items\":[{\"id\":\"coins\",\"amount\":10}]}";

            Assert.IsTrue(RemoteDailyRewardSchedule.TryParse(
                "{\"unlock_level\":-4,\"days\":[" + week + "]}", out var days, out var unlock));
            Assert.AreEqual(7, days.Count);
            Assert.AreEqual(0, unlock);
        }
    }
}
