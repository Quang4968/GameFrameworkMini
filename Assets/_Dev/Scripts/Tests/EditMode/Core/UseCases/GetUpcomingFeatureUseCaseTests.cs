using NUnit.Framework;
using Game.Core.Domain;
using Game.Core.GameLogic;
using Game.Core.Services;
using Game.Core.UseCases;

namespace Game.Core.Tests.UseCases
{
    public class GetUpcomingFeatureUseCaseTests
    {
        sealed class FakeLevelProgress : ILevelProgressService
        {
            public FakeLevelProgress(int levelShow) => LevelShow = levelShow;

            public int LevelShow { get; private set; }

            public int LevelLoad => LevelShow;

            public void SetLevelShow(int levelShow) => LevelShow = levelShow;

            public void AdvanceToNextLevel() => LevelShow++;
        }

        static FeatureIntroductionSchedule Schedule() => new(new[]
        {
            new FeatureIntroduction(Features.A, 7),
            new FeatureIntroduction(Features.B, 12),
        });

        [Test]
        public void Execute_ReadsNextLevel_NotTheLevelJustWon()
        {
            // Vừa thắng màn 6 (chỉ số 5) -> sắp vào màn 7 (chỉ số 6), còn đúng màn 7 nữa là tới màn 8.
            var found = GetUpcomingFeatureUseCase.Execute(
                Schedule(), new FakeLevelProgress(5), out var progress, out _);

            Assert.IsTrue(found);
            Assert.AreEqual(6, progress.NextLevelIndex);
            Assert.AreEqual(1, progress.LevelsRemaining);
        }

        [Test]
        public void Execute_FillStartsAtProgressBeforeThisWin()
        {
            var found = GetUpcomingFeatureUseCase.Execute(
                Schedule(), new FakeLevelProgress(5), out var progress, out var fillFrom);

            Assert.IsTrue(found);
            Assert.AreEqual(5f / 7f, fillFrom, 0.0001f, "Điểm xuất phát là tiến độ của màn vừa thắng.");
            Assert.AreEqual(6f / 7f, progress.Normalized01, 0.0001f);
        }

        [Test]
        public void Execute_JustCrossedAMilestone_FillRestartsFromZero()
        {
            // Vừa thắng màn 8 (chỉ số 7) — chính là màn ra mắt Camouflage. Chặng mới là LinkedGroup,
            // thanh phải chạy từ 0 chứ không phải từ chỗ thanh cũ đang đứng.
            var found = GetUpcomingFeatureUseCase.Execute(
                Schedule(), new FakeLevelProgress(7), out var progress, out var fillFrom);

            Assert.IsTrue(found);
            Assert.AreEqual(Features.B, progress.Feature);
            Assert.AreEqual(0f, fillFrom, "Vượt mốc thì chặng cũ đóng lại, thanh không được giật lùi.");
        }

        [Test]
        public void Execute_NoScheduleOrService_ReturnsFalse()
        {
            Assert.IsFalse(GetUpcomingFeatureUseCase.Execute(null, new FakeLevelProgress(0), out _, out _));
            Assert.IsFalse(GetUpcomingFeatureUseCase.Execute(Schedule(), null, out _, out _));
        }
    }
}
