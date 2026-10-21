using Game.Core.Services;

namespace Game.Core.UseCases
{
    // Mua một phát ĐẦY BÌNH tim bằng coin. Giá cố định, không nhân theo số tim đang thiếu: thiếu 1
    // tim hay thiếu cả 5 đều trả đúng bằng nhau.
    //
    // Hệ quả cố ý: mua lúc chỉ thiếu 1 tim là lỗ, nên người chơi được đẩy về phía chờ hồi hoặc đợi
    // cạn tim rồi mới mua. Giá bao nhiêu là dữ liệu do Game đưa xuống (LifeSettingsSO).
    public static class RefillLivesWithCoinsUseCase
    {
        // Trả về số tim THẬT SỰ được cộng — 0 nghĩa là không mua được (đầy tim rồi, hoặc không đủ
        // coin). Đầy tim thì chặn từ đầu để không tiêu coin oan; không đủ coin thì không đụng tới
        // kho tim — hai ví không bao giờ đổi trạng thái nửa chừng nếu điều kiện không thoả.
        public static int Execute(ICoinService coins, ILifeService lives, int coinCost)
        {
            if (coins == null || lives == null || coinCost <= 0) return 0;

            // Refresh trước khi kiểm: tim có thể vừa hồi đủ trong lúc popup đang mở.
            lives.Refresh();
            if (!CanRefillLivesUseCase.Execute(coins, lives, coinCost)) return 0;

            if (!coins.TrySpend(coinCost)) return 0;

            var missing = lives.MaxLives - lives.Lives;
            lives.Add(missing);
            return missing;
        }
    }
}
