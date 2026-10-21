using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk.Ads;
using Game.Core.Services;

namespace Game.Game.Services
{
    // Nối IRewardedAdService của Core vào bộ SDK.
    //
    // Đây là TOÀN BỘ chỗ tiếp xúc giữa game và hệ quảng cáo — đúng một file. Core vẫn không biết
    // MoodBoost.Sdk tồn tại, view vẫn chỉ thấy IRewardedAdService y như trước. Đổi mediation, hay
    // gỡ hẳn SDK ra, không đụng tới popup nào.
    public sealed class SdkRewardedAdService : IRewardedAdService
    {
        readonly IAdService ads;

        public bool IsReady => ads.IsReady(AdFormat.Rewarded);

        public SdkRewardedAdService(IAdService ads)
        {
            this.ads = ads;
        }

        public void Show(string placement, Action onRewarded, Action onClosed = null)
        {
            ShowAsync(placement, onRewarded, onClosed).Forget();
        }

        async UniTaskVoid ShowAsync(string placement, Action onRewarded, Action onClosed)
        {
            // CancellationToken.None là cố ý. Quảng cáo đã che màn hình thì không rút về được, nên
            // token duy nhất có thể làm ở đây là bỏ rơi kết quả — tức là người chơi xem hết mà
            // không được thưởng. Thà chạy nốt: SDK sống xuyên scene nên callback vẫn tới nơi.
            var result = await ads.ShowRewardedAsync(placement, CancellationToken.None);

            // Ranh giới của IRewardedAdService: chỉ xem hết mới cộng thưởng. Bỏ ngang, no-fill hay
            // lỗi mạng đều rơi vào đây và không gọi gì cả — đúng như popup đang chờ.
            if (result == AdResult.Completed) onRewarded?.Invoke();

            onClosed?.Invoke();
        }
    }
}
