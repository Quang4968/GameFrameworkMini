using Game.Core.Services;

namespace Game.Core.UseCases
{
    // Thắng một màn = cộng coin thưởng. Luật nằm ở đây, còn cộng bao nhiêu là dữ liệu do Game
    // đưa xuống (CoinSettingsSO), nên đổi con số không phải sửa code.
    public static class AwardWinCoinsUseCase
    {
        // Trả về số coin thật sự được cộng — popup thắng muốn hiện "+30" thì đọc chính giá trị
        // này, khỏi tự cộng lại một lần nữa.
        public static int Execute(ICoinService coins, int coinsPerWin)
        {
            if (coins == null || coinsPerWin <= 0) return 0;

            coins.Add(coinsPerWin);
            return coinsPerWin;
        }
    }
}
