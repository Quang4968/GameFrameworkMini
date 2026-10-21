using System;
using MoodBoost.Sdk.Tracking;
using Game.Core.Services;
using Game.Core.UseCases;
using Game.Game.Services;
using VContainer.Unity;

namespace Game.Game.Views.Gameplay
{
    // Thắng màn thì hoàn lại số tim đã trả lúc vào màn (LevelStartLifePresenter). Tách khỏi
    // GameResultPopupPresenter để phần hoàn tim không phụ thuộc vào hệ popup: scene không gán
    // PopupHost thì người chơi vẫn được hoàn — đối xứng với WinCoinRewardPresenter.
    //
    // Nguồn sự thật vẫn là ILevelOutcomeSource — nó chốt kết quả và chỉ bắn Won ĐÚNG MỘT LẦN mỗi
    // màn, nên không có chuyện hoàn hai lần dù người chơi để popup thắng mở bao lâu.
    public sealed class WinLifeRefundPresenter : IStartable, IDisposable
    {
        readonly ILevelOutcomeSource outcome;
        readonly ILifeService lives;
        readonly LevelStartLifePresenter levelStart;
        readonly ITrackingService tracking;

        public WinLifeRefundPresenter(
            ILevelOutcomeSource outcome, ILifeService lives, LevelStartLifePresenter levelStart,
            ITrackingService tracking)
        {
            this.outcome = outcome;
            this.lives = lives;
            this.levelStart = levelStart;
            this.tracking = tracking;
        }

        public void Start()
        {
            outcome.Won += RefundLife;
        }

        public void Dispose()
        {
            outcome.Won -= RefundLife;
        }

        void RefundLife()
        {
            // Refunds what entry cost, so a free entry (onboarding, unlimited window) mints no life.
            var refunded = RefundLifeOnWinUseCase.Execute(lives, levelStart.Charged);
            if (refunded <= 0) return;

            tracking.LogResourceEarn(
                new ResourceChange(ResourceType.Item, ResourceNames.Life, refunded, lives.Lives),
                TrackingPlacements.LevelWin,
                EarnReason.Reward);
        }
    }
}
