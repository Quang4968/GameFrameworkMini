using MoodBoost.Sdk.Tracking;
using Game.Core.GameLogic;
using Game.Core.Services;
using Game.Core.UseCases;
using Game.Game.Data;
using Game.Game.Services;
using VContainer.Unity;

namespace Game.Game.Views.Gameplay
{
    // Trừ tim NGAY khi màn mở lên, không đợi thua. Đặt ở đây chứ không ở nút Play của Home vì mọi
    // đường vào màn đều phải trả giá như nhau: Play, Replay ở popup Thua, Next ở popup Thắng, và
    // cả lúc bấm Play thẳng scene MainGame trong Editor.
    //
    // Bỏ màn giữa chừng nhờ vậy không cần đoạn code nào canh: tim đã bị trừ từ trước, người chơi
    // rời màn kiểu gì — bấm Home, tắt app, hay bị hệ thống kill — cũng đều không được hoàn lại.
    // Chỉ WinLifeRefundPresenter mới trả tim về.
    //
    // The onboarding run and an unlimited window are free. WinLifeRefundPresenter refunds Charged
    // rather than re-asking those rules: a window can run out mid-level, and re-asking at the win
    // would pay out a life nobody paid for.
    public sealed class LevelStartLifePresenter : IStartable
    {
        readonly ILifeService lives;
        readonly LifeSettingsSO settings;
        readonly ITrackingService tracking;
        readonly ILevelProgressService levelProgress;

        public LevelStartLifePresenter(ILifeService lives, LifeSettingsSO settings, ITrackingService tracking,
            ILevelProgressService levelProgress)
        {
            this.lives = lives;
            this.settings = settings;
            this.tracking = tracking;
            this.levelProgress = levelProgress;
        }

        public int Charged { get; private set; }

        public void Start()
        {
            if (!OnboardingRules.ChargesLife(levelProgress.LevelShow)) return;

            Charged = SpendLifeOnLevelStartUseCase.Execute(lives, settings.LivesPerLevel);
            if (Charged <= 0) return;

            tracking.LogResourceSpend(
                new ResourceChange(ResourceType.Item, ResourceNames.Life, Charged, lives.Lives),
                TrackingPlacements.LevelStart,
                SpendReason.Use);
        }
    }
}
