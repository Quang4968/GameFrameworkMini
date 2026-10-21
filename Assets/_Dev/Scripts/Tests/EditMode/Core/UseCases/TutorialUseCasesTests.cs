using System;
using System.Collections.Generic;
using NUnit.Framework;
using Game.Core.Domain;
using Game.Core.GameLogic;
using Game.Core.Services;
using Game.Core.Tests.GameLogic;
using Game.Core.UseCases;

namespace Game.Core.Tests.UseCases
{
    public class TutorialUseCasesTests
    {
        sealed class FakeLevelProgress : ILevelProgressService
        {
            public FakeLevelProgress(int levelShow) => LevelShow = levelShow;

            public int LevelShow { get; private set; }

            public int LevelLoad => LevelShow;

            public void SetLevelShow(int levelShow) => LevelShow = levelShow;

            public void AdvanceToNextLevel() => LevelShow++;
        }

        // Keeps the forward-only rule of the real service, so a use case that tries to walk the
        // stage backwards fails here instead of in a build.
        sealed class FakeTutorialState : I_TutorialStateService
        {
            readonly Dictionary<E_TutorialId, E_TutorialStage> stages = new();

            public event Action<E_TutorialId, E_TutorialStage> StageChanged;

            public E_TutorialStage GetStage(E_TutorialId id)
                => stages.TryGetValue(id, out var stage) ? stage : E_TutorialStage.NotStarted;

            public void SetStage(E_TutorialId id, E_TutorialStage stage)
            {
                if (stage <= GetStage(id)) return;

                stages[id] = stage;
                StageChanged?.Invoke(id, stage);
            }
        }

        sealed class FakeBoosterInventory : IBoosterInventoryService
        {
            readonly Dictionary<BoosterType, int> counts = new();

            public event Action<BoosterType, int> CountChanged;

            public int GetCount(BoosterType type) => counts.TryGetValue(type, out var count) ? count : 0;

            public void Add(BoosterType type, int amount)
            {
                if (amount <= 0) return;

                counts[type] = GetCount(type) + amount;
                CountChanged?.Invoke(type, counts[type]);
            }

            public bool TrySpend(BoosterType type, int amount)
            {
                if (amount <= 0 || GetCount(type) < amount) return false;

                counts[type] = GetCount(type) - amount;
                CountChanged?.Invoke(type, counts[type]);
                return true;
            }
        }

        static TutorialSchedule Schedule() => TutorialScheduleTests.ShippingSchedule();

        [Test]
        public void GetDue_NothingBeforeTheFirstUnlockLevel()
        {
            Assert.IsFalse(GetDueTutorialUseCase.Execute(
                Schedule(), new FakeLevelProgress(4), new FakeTutorialState(), out _));
        }

        [Test]
        public void GetDue_ReturnsTheLessonOfTheLevelBeingPlayed()
        {
            Assert.IsTrue(GetDueTutorialUseCase.Execute(
                Schedule(), new FakeLevelProgress(5), new FakeTutorialState(), out var due));

            Assert.AreEqual(Lessons.A, due.Id);
            Assert.AreEqual(Boosters.C, due.Booster);
        }

        [Test]
        public void GetDue_SkipsFinishedLessons()
        {
            var state = new FakeTutorialState();
            state.SetStage(Lessons.A, E_TutorialStage.Done);

            Assert.IsFalse(GetDueTutorialUseCase.Execute(
                Schedule(), new FakeLevelProgress(5), state, out _),
                "Level 6 has nothing left to teach once Dig is learned.");
        }

        [Test]
        public void GetDue_ClaimedLessonNeverComesBack()
        {
            // Taking the gift is what counts as "already shown". Quitting mid-lesson and coming
            // back must NOT replay it.
            var state = new FakeTutorialState();
            state.SetStage(Lessons.A, E_TutorialStage.RewardClaimed);

            Assert.IsFalse(GetDueTutorialUseCase.Execute(
                Schedule(), new FakeLevelProgress(5), state, out _));
        }

        [Test]
        public void GetDue_UnclaimedLessonComesBack()
        {
            // Quitting BEFORE claiming hands over nothing, so the gift must still be waiting.
            Assert.IsTrue(GetDueTutorialUseCase.Execute(
                Schedule(), new FakeLevelProgress(5), new FakeTutorialState(), out var due));

            Assert.AreEqual(Lessons.A, due.Id);
        }

        [Test]
        public void GetDue_ReturnsTheEarliestMissedLesson_NotTheOneOfThisLevel()
        {
            var state = new FakeTutorialState();
            state.SetStage(Lessons.B, E_TutorialStage.RewardClaimed);

            Assert.IsTrue(GetDueTutorialUseCase.Execute(
                Schedule(), new FakeLevelProgress(14), state, out var due));

            Assert.AreEqual(Lessons.A, due.Id,
                "A player who arrived at level 15 unlearned gets the backlog one level at a time.");
        }

        [Test]
        public void Claim_GrantsTheFreeBoostersOnce()
        {
            var boosters = new FakeBoosterInventory();
            var state = new FakeTutorialState();
            Schedule().TryGet(Lessons.D, out var tutorial);

            Assert.IsTrue(ClaimTutorialRewardUseCase.Execute(tutorial, boosters, state));
            Assert.AreEqual(2, boosters.GetCount(Boosters.E));
            Assert.AreEqual(E_TutorialStage.RewardClaimed, state.GetStage(Lessons.D));

            Assert.IsFalse(ClaimTutorialRewardUseCase.Execute(tutorial, boosters, state),
                "A reopened popup or a relaunch must not pay out twice.");
            Assert.AreEqual(2, boosters.GetCount(Boosters.E));
        }

        [Test]
        public void Claim_DoesNotDemoteAFinishedLesson()
        {
            var boosters = new FakeBoosterInventory();
            var state = new FakeTutorialState();
            state.SetStage(Lessons.D, E_TutorialStage.Done);
            Schedule().TryGet(Lessons.D, out var tutorial);

            Assert.IsFalse(ClaimTutorialRewardUseCase.Execute(tutorial, boosters, state));
            Assert.AreEqual(0, boosters.GetCount(Boosters.E));
            Assert.AreEqual(E_TutorialStage.Done, state.GetStage(Lessons.D));
        }
    }
}
