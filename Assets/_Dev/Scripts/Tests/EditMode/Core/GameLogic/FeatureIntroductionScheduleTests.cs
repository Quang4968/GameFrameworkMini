using NUnit.Framework;
using Game.Core.Domain;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class FeatureIntroductionScheduleTests
    {
        // Bốn mốc mặc định của game, viết bằng CHỈ SỐ đếm từ 0: màn 8, 13, 30, 40.
        static FeatureIntroductionSchedule DefaultSchedule() => new(new[]
        {
            new FeatureIntroduction(Features.A, 7),
            new FeatureIntroduction(Features.B, 12),
            new FeatureIntroduction(Features.C, 29),
            new FeatureIntroduction(Features.D, 39),
        });

        [Test]
        public void Constructor_SortsByLevel_AndKeepsEarliestOfDuplicates()
        {
            var schedule = new FeatureIntroductionSchedule(new[]
            {
                new FeatureIntroduction(Features.C, 29),
                new FeatureIntroduction(Features.A, 20),
                new FeatureIntroduction(Features.A, 7),
            });

            Assert.AreEqual(2, schedule.Count, "Cơ chế khai hai lần vẫn chỉ là một mốc.");
            Assert.AreEqual(Features.A, schedule[0].Feature);
            Assert.AreEqual(7, schedule[0].LevelIndex, "Giữ màn nhỏ nhất — lần đầu gặp mới đáng khoe.");
            Assert.AreEqual(Features.C, schedule[1].Feature);
        }

        [Test]
        public void Constructor_DropsNegativeLevels()
        {
            var schedule = new FeatureIntroductionSchedule(new[]
            {
                new FeatureIntroduction(Features.C, -1),
                new FeatureIntroduction(Features.A, 7),
            });

            Assert.AreEqual(1, schedule.Count);
        }

        [Test]
        public void TryGetUpcoming_FirstMilestone_MeasuresFromLevelZero()
        {
            // Vừa thắng màn 5 -> sắp vào màn 6 -> chỉ số 5. Chặng đầu chạy từ 0 tới 7.
            Assert.IsTrue(DefaultSchedule().TryGetUpcoming(5, out var progress));

            Assert.AreEqual(Features.A, progress.Feature);
            Assert.AreEqual(0, progress.FromLevelIndex);
            Assert.AreEqual(7, progress.TargetLevelIndex);
            Assert.AreEqual(2, progress.LevelsRemaining);
            Assert.AreEqual(5f / 7f, progress.Normalized01, 0.0001f);
            Assert.IsFalse(progress.IsReadyNow);
        }

        [Test]
        public void TryGetIntroductionAt_ReturnsOnlyTheExactMilestone()
        {
            Assert.IsTrue(DefaultSchedule().TryGetIntroductionAt(12, out var introduction));
            Assert.AreEqual(Features.B, introduction.Feature);
            Assert.IsFalse(DefaultSchedule().TryGetIntroductionAt(11, out _));
            Assert.IsFalse(DefaultSchedule().TryGetIntroductionAt(13, out _));
        }

        [Test]
        public void TryGetUpcoming_MiddleMilestone_MeasuresFromPreviousMilestone()
        {
            // Sắp vào màn 11 (chỉ số 10): chặng chạy từ mốc 7 tới mốc 12 -> 3/5.
            Assert.IsTrue(DefaultSchedule().TryGetUpcoming(10, out var progress));

            Assert.AreEqual(Features.B, progress.Feature);
            Assert.AreEqual(7, progress.FromLevelIndex);
            Assert.AreEqual(0.6f, progress.Normalized01, 0.0001f);
        }

        [Test]
        public void TryGetUpcoming_NextLevelIsTheIntroLevel_IsReadyAndFull()
        {
            Assert.IsTrue(DefaultSchedule().TryGetUpcoming(7, out var progress));

            Assert.AreEqual(Features.A, progress.Feature);
            Assert.AreEqual(0, progress.LevelsRemaining);
            Assert.IsTrue(progress.IsReadyNow, "Màn sắp vào chính là màn ra mắt -> đã mở khoá.");
            Assert.AreEqual(1f, progress.Normalized01, 0.0001f);
        }

        [Test]
        public void TryGetUpcoming_PastLastMilestone_ReturnsFalse()
        {
            Assert.IsFalse(DefaultSchedule().TryGetUpcoming(40, out _),
                "Giới thiệu hết rồi thì không còn mốc nào để vẽ — view lấy đó làm tín hiệu tự ẩn.");
        }

        [Test]
        public void TryGetUpcoming_EmptySchedule_ReturnsFalse()
        {
            Assert.IsFalse(FeatureIntroductionSchedule.Empty.TryGetUpcoming(0, out _));
        }

        [Test]
        public void TryGetUpcoming_MilestoneAtLevelZero_IsFullInsteadOfDividingByZero()
        {
            var schedule = new FeatureIntroductionSchedule(new[]
            {
                new FeatureIntroduction(Features.A, 0),
            });

            Assert.IsTrue(schedule.TryGetUpcoming(0, out var progress));
            Assert.AreEqual(1f, progress.Normalized01);
        }

        [Test]
        public void TryGetUpcoming_PlayerJumpedBackBeforePreviousMilestone_ClampsToZero()
        {
            // Sửa tay PlayerPrefs về màn 2 sau khi đã qua mốc 7: chặng của LinkedGroup tính ra âm.
            var schedule = new FeatureIntroductionSchedule(new[]
            {
                new FeatureIntroduction(Features.A, 7),
                new FeatureIntroduction(Features.B, 12),
            });

            Assert.IsTrue(schedule.TryGetUpcoming(2, out var progress));
            Assert.AreEqual(Features.A, progress.Feature,
                "Nhảy lùi thì cơ chế kế tiếp cũng lùi theo, không kẹt ở mốc cũ.");
            Assert.GreaterOrEqual(progress.Normalized01, 0f);
        }
    }
}
