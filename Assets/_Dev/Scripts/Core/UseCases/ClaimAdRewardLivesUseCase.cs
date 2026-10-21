using Game.Core.Services;

namespace Game.Core.UseCases
{
    // Cộng tim thưởng sau khi người chơi xem xong quảng cáo. Tách riêng khỏi việc CHIẾU quảng cáo:
    // ở đây chỉ có luật cộng, còn quảng cáo chạy thế nào là chuyện của IRewardedAdService.
    public static class ClaimAdRewardLivesUseCase
    {
        // Trả về số tim THẬT SỰ được cộng — 0 khi đang đầy tim, vì Add đã kẹp trần sẵn.
        public static int Execute(ILifeService lives, int livesPerAd)
        {
            if (lives == null || livesPerAd <= 0) return 0;

            var before = lives.Lives;
            lives.Add(livesPerAd);
            return lives.Lives - before;
        }
    }
}
