using System;

namespace Game.Core.Services
{
    // Quảng cáo có thưởng. Core chỉ biết interface; nối SDK nào (AdMob, Unity Ads, Max) là việc
    // của Game — y hệt ILifeService với PlayerPrefs.
    //
    // Game nối nó bằng SdkRewardedAdService (Game/Services). Chưa bật define symbol của mediation
    // nào thì IsReady luôn false nên nút "Free" hiện ra ở trạng thái mờ, đúng như lúc chưa có quảng
    // cáo nào tải xong — không có nhánh UI riêng cho trường hợp đó.
    public interface IRewardedAdService
    {
        // Có sẵn quảng cáo để xem ngay không. View đọc cái này để bật/tắt nút.
        bool IsReady { get; }

        // onRewarded CHỈ được gọi khi người chơi xem hết và SDK xác nhận thưởng — bỏ ngang thì
        // không gọi. Đây là ranh giới quan trọng nhất của interface này: chỗ gọi không bao giờ tự
        // cộng thưởng trước.
        //
        // placement is the analytics label of where the ad was offered; the constants live in SdkSystemInstaller.
        // onClosed runs after EVERY outcome - watched, skipped, not loaded - and after onRewarded when the ad paid
        // out, so a caller that locked its buttons for the ad knows when to unlock them.
        void Show(string placement, Action onRewarded, Action onClosed = null);
    }
}
