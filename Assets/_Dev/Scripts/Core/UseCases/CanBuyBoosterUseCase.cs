using Game.Core.Services;

namespace Game.Core.UseCases
{
    // Có đủ tiền mua booster không. Tách riêng khỏi BuyBoosterWithCoinsUseCase để nút mua và cú
    // mua thật nhìn đúng MỘT luật — nút sáng thì bấm chắc chắn ăn. Cùng lối với
    // CanRefillLivesUseCase.
    //
    // Cố tình KHÔNG chặn trần kho: booster không có giới hạn số lượng như tim, mua thêm lúc đang
    // còn là chuyện bình thường.
    public static class CanBuyBoosterUseCase
    {
        public static bool Execute(ICoinService coins, int coinCost)
        {
            if (coins == null || coinCost <= 0) return false;

            return coins.Coins >= coinCost;
        }
    }
}
