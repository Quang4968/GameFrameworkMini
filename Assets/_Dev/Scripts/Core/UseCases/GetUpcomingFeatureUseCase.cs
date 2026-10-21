using Game.Core.Domain;
using Game.Core.GameLogic;
using Game.Core.Services;

namespace Game.Core.UseCases
{
    // Lấy cơ chế sắp được giới thiệu để popup thắng khoe, kèm điểm XUẤT PHÁT của thanh progress.
    //
    // Gọi lúc popup thắng đang mở, tức lúc LevelShow VẪN LÀ màn vừa thắng (AdvanceToNextLevel
    // chỉ chạy khi bấm Next). Nhờ vậy suy ra được cả hai đầu của animation mà không phải lưu thêm
    // gì: đầu này là màn vừa thắng, đầu kia là màn sắp vào.
    public static class GetUpcomingFeatureUseCase
    {
        public static bool Execute(FeatureIntroductionSchedule schedule, ILevelProgressService levelProgress,
            out FeatureIntroductionProgress progress, out float fillFrom)
        {
            progress = default;
            fillFrom = 0f;

            if (schedule == null || levelProgress == null) return false;

            var wonLevelIndex = levelProgress.LevelShow;
            if (!schedule.TryGetUpcoming(wonLevelIndex + 1, out progress)) return false;

            // Điểm xuất phát là tiến độ TRƯỚC cú thắng này. Vừa vượt qua một mốc thì chặng cũ đã
            // đóng, chặng mới phải chạy từ 0 chứ không phải từ chỗ thanh cũ đang đứng — nếu không
            // thanh sẽ nhảy giật lùi rồi mới bò lên.
            if (schedule.TryGetUpcoming(wonLevelIndex, out var before) && before.Feature == progress.Feature)
            {
                fillFrom = before.Normalized01;
            }

            return true;
        }
    }
}
