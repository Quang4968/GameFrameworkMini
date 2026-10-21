using Game.Core.Domain;
using Game.Core.Services;

namespace Game.Core.UseCases
{
    // Mua một lô booster bằng coin. Giá và số lượng mỗi lô là dữ liệu do Game đưa xuống
    // (BoosterSettingsSO), Core chỉ ghép hai cái ví lại với nhau.
    public static class BuyBoosterWithCoinsUseCase
    {
        // Trả về số booster THẬT SỰ được cộng — 0 nghĩa là không mua được (không đủ coin, hoặc
        // loại này đang tắt bán). Không đủ coin thì không đụng tới kho booster: hai ví không bao
        // giờ đổi trạng thái nửa chừng nếu điều kiện không thoả.
        public static int Execute(ICoinService coins, IBoosterInventoryService boosters, BoosterType type,
            int coinCost, int amount)
        {
            if (boosters == null || amount <= 0) return 0;
            if (!CanBuyBoosterUseCase.Execute(coins, coinCost)) return 0;

            if (!coins.TrySpend(coinCost)) return 0;

            boosters.Add(type, amount);
            return amount;
        }
    }
}
