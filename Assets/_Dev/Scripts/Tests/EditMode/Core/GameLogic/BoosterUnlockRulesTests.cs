using NUnit.Framework;
using Game.Core.Domain;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class BoosterUnlockRulesTests
    {
        static TutorialSchedule Schedule() => TutorialScheduleTests.ShippingSchedule();

        [Test]
        public void IsUnlocked_FalseBeforeTheUnlockLevel()
        {
            // Level 5 on screen is index 4; Dig opens on level 6, index 5.
            Assert.IsFalse(BoosterUnlockRules.IsUnlocked(Schedule(), Boosters.C, 4));
        }

        [Test]
        public void IsUnlocked_TrueOnTheUnlockLevelItself()
        {
            Assert.IsTrue(BoosterUnlockRules.IsUnlocked(Schedule(), Boosters.C, 5),
                "The player reaches the unlock level with the button already usable.");
        }

        [Test]
        public void IsUnlocked_StaysTrueAfterwards()
        {
            Assert.IsTrue(BoosterUnlockRules.IsUnlocked(Schedule(), Boosters.C, 40));
        }

        [Test]
        public void IsUnlocked_BoosterWithNoRow_IsOpenFromTheStart()
        {
            var schedule = new TutorialSchedule(new[]
            {
                new TutorialDefinition(Lessons.A, 5, Boosters.C, 2),
            });

            Assert.IsTrue(BoosterUnlockRules.IsUnlocked(schedule, Boosters.E, 0),
                "Silence in the config must not hide a button forever.");
        }

        [Test]
        public void IsUnlocked_NullSchedule_LeavesEverythingOpen()
        {
            Assert.IsTrue(BoosterUnlockRules.IsUnlocked(null, Boosters.D, 0));
        }

        [Test]
        public void HasAnyUnlocked_FalseBeforeTheFirstUnlockLevel()
        {
            Assert.IsFalse(BoosterUnlockRules.HasAnyUnlocked(Schedule(), 4),
                "Level 5 on screen: the whole row would be padlocks.");
        }

        [Test]
        public void HasAnyUnlocked_TrueOnTheFirstUnlockLevel()
        {
            Assert.IsTrue(BoosterUnlockRules.HasAnyUnlocked(Schedule(), 5));
        }

        [Test]
        public void HasAnyUnlocked_EmptySchedule_LeavesTheRowVisible()
        {
            Assert.IsTrue(BoosterUnlockRules.HasAnyUnlocked(TutorialSchedule.Empty, 0));
            Assert.IsTrue(BoosterUnlockRules.HasAnyUnlocked(null, 0));
        }

        [Test]
        public void HasAnyUnlocked_IgnoresALessonThatUnlocksNothing()
        {
            var schedule = new TutorialSchedule(new[]
            {
                new TutorialDefinition(E_TutorialId.HowToPlay, 0, Boosters.A, 0, false),
                new TutorialDefinition(Lessons.A, 5, Boosters.C, 2),
            });

            Assert.IsFalse(BoosterUnlockRules.HasAnyUnlocked(schedule, 0),
                "The level-1 lesson opens no button, so the row is still all padlocks.");

            Assert.IsTrue(BoosterUnlockRules.IsUnlocked(schedule, Boosters.A, 0),
                "Its booster field means nothing and must not gate the real button.");
        }

        [Test]
        public void TryGetUnlockLevelNumber_ReturnsTheNumberThePlayerReads()
        {
            Assert.IsTrue(BoosterUnlockRules.TryGetUnlockLevelNumber(
                Schedule(), Boosters.D, out var levelNumber));

            Assert.AreEqual(18, levelNumber, "Index 17 is level 18 on screen.");
        }

        [Test]
        public void TryGetUnlockLevelNumber_FalseWhenNothingToPromise()
        {
            Assert.IsFalse(BoosterUnlockRules.TryGetUnlockLevelNumber(
                TutorialSchedule.Empty, Boosters.C, out _));
        }
    }
}
