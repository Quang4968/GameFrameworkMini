using NUnit.Framework;
using Game.Core.Domain;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class TutorialScheduleTests
    {
        // The shipping schedule written as INDEXES counted from 0: levels 6, 9, 12, 15, 18.
        internal static TutorialSchedule ShippingSchedule() => new(new[]
        {
            new TutorialDefinition(Lessons.A, 5, Boosters.C, 2),
            new TutorialDefinition(Lessons.B, 8, Boosters.A, 2),
            new TutorialDefinition(Lessons.C, 11, Boosters.B, 2),
            new TutorialDefinition(Lessons.D, 14, Boosters.E, 2),
            new TutorialDefinition(Lessons.E, 17, Boosters.D, 2),
        });

        [Test]
        public void Constructor_SortsByLevel()
        {
            var schedule = new TutorialSchedule(new[]
            {
                new TutorialDefinition(Lessons.D, 14, Boosters.E, 2),
                new TutorialDefinition(Lessons.A, 5, Boosters.C, 2),
            });

            Assert.AreEqual(Lessons.A, schedule[0].Id);
            Assert.AreEqual(Lessons.D, schedule[1].Id);
        }

        [Test]
        public void Constructor_KeepsEarliestOfDuplicateIds()
        {
            var schedule = new TutorialSchedule(new[]
            {
                new TutorialDefinition(Lessons.A, 20, Boosters.C, 2),
                new TutorialDefinition(Lessons.A, 5, Boosters.C, 2),
            });

            Assert.AreEqual(1, schedule.Count, "One lesson declared twice is still one lesson.");
            Assert.AreEqual(5, schedule[0].LevelIndex, "Earliest wins: the first meeting is the one to teach.");
        }

        [Test]
        public void Constructor_KeepsOneRowPerBooster_EvenUnderDifferentIds()
        {
            // Two unlock levels for one button would leave BoosterUnlockRules with no single answer.
            var schedule = new TutorialSchedule(new[]
            {
                new TutorialDefinition(Lessons.A, 5, Boosters.C, 2),
                new TutorialDefinition(Lessons.C, 11, Boosters.C, 2),
            });

            Assert.AreEqual(1, schedule.Count);
            Assert.AreEqual(5, schedule[0].LevelIndex);
        }

        [Test]
        public void Constructor_LessonThatUnlocksNothing_LeavesItsBoosterFieldAlone()
        {
            // Both rows carry Boosters.A, but only one of them means it.
            var schedule = new TutorialSchedule(new[]
            {
                new TutorialDefinition(E_TutorialId.HowToPlay, 0, Boosters.A, 0, false),
                new TutorialDefinition(Lessons.B, 8, Boosters.A, 2),
            });

            Assert.AreEqual(2, schedule.Count, "Rows that unlock nothing cannot collide on a booster.");

            Assert.IsTrue(schedule.TryGetForBooster(Boosters.A, out var tutorial));
            Assert.AreEqual(8, tutorial.LevelIndex, "Only the row that really unlocks Shuffle answers for it.");
        }

        [Test]
        public void Constructor_DropsNegativeLevelsAndRewards()
        {
            var schedule = new TutorialSchedule(new[]
            {
                new TutorialDefinition(Lessons.A, -1, Boosters.C, 2),
                new TutorialDefinition(Lessons.B, 8, Boosters.A, -2),
                new TutorialDefinition(Lessons.C, 11, Boosters.B, 2),
            });

            Assert.AreEqual(1, schedule.Count);
            Assert.AreEqual(Lessons.C, schedule[0].Id);
        }

        [Test]
        public void TryGetForBooster_FindsTheRowThatUnlocksThatButton()
        {
            Assert.IsTrue(ShippingSchedule().TryGetForBooster(Boosters.E, out var tutorial));

            Assert.AreEqual(Lessons.D, tutorial.Id);
            Assert.AreEqual(14, tutorial.LevelIndex);
        }

        [Test]
        public void Empty_HasNoRows()
        {
            Assert.AreEqual(0, TutorialSchedule.Empty.Count);
            Assert.IsFalse(TutorialSchedule.Empty.TryGet(Lessons.A, out _));
        }
    }
}
