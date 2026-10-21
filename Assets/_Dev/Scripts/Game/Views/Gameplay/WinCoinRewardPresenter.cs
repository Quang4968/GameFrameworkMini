using System;
using MoodBoost.Sdk.Tracking;
using Game.Core.Services;
using Game.Core.UseCases;
using Game.Game.Data;
using Game.Game.Services;
using VContainer.Unity;

namespace Game.Game.Views.Gameplay
{
    // Thắng màn thì cộng coin. Tách khỏi GameResultPopupPresenter để tiền thưởng không phụ thuộc
    // vào hệ popup: scene không gán PopupHost thì người chơi vẫn được thưởng.
    //
    // Nguồn sự thật vẫn là ILevelOutcomeSource — nó chốt kết quả và chỉ bắn Won ĐÚNG MỘT LẦN mỗi
    // màn, nên không có chuyện cộng hai lần. Chơi lại màn cũ rồi thắng tiếp thì được thưởng tiếp,
    // đúng ý đồ: phần thưởng gắn với lượt thắng chứ không phải với chỉ số màn.
    public sealed class WinCoinRewardPresenter : IStartable, IDisposable
    {
        readonly ILevelOutcomeSource outcome;
        readonly ICoinService coins;
        readonly CoinSettingsSO settings;
        readonly ITrackingService tracking;

        public WinCoinRewardPresenter(
            ILevelOutcomeSource outcome, ICoinService coins, CoinSettingsSO settings,
            ITrackingService tracking)
        {
            this.outcome = outcome;
            this.coins = coins;
            this.settings = settings;
            this.tracking = tracking;
        }

        public void Start()
        {
            outcome.Won += AwardCoins;
        }

        public void Dispose()
        {
            outcome.Won -= AwardCoins;
        }

        void AwardCoins()
        {
            // Use case tự kẹp về 0 khi số thưởng khai sai, nên nó mới là nguồn của số thật.
            var awarded = AwardWinCoinsUseCase.Execute(coins, settings.CoinsPerWin);
            if (awarded <= 0) return;

            tracking.LogResourceEarn(
                new ResourceChange(ResourceType.Currency, ResourceNames.Coin, awarded, coins.Coins),
                TrackingPlacements.LevelWin,
                EarnReason.Reward);
        }
    }
}
