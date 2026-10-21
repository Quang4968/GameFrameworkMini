using Game.Core.Services;

namespace Game.Core.UseCases
{
    // Có đủ điều kiện mua đầy tim không. Tách riêng khỏi RefillLivesWithCoinsUseCase để view hỏi
    // được câu "nút mua có bấm được không" mà KHÔNG phải tự viết lại điều kiện — cả hai bên nhìn
    // đúng một luật nên nút sáng thì bấm chắc chắn ăn.
    public static class CanRefillLivesUseCase
    {
        // Cố tình KHÔNG gọi Refresh(): đây là đường đọc để vẽ, chạy mỗi lần popup vẽ lại. Việc làm
        // mới số tim theo thời gian là của LifeTicker, còn lúc bấm mua thật thì
        // RefillLivesWithCoinsUseCase tự Refresh() trước khi trừ tiền.
        public static bool Execute(ICoinService coins, ILifeService lives, int coinCost)
        {
            if (coins == null || lives == null || coinCost <= 0) return false;

            return lives.Lives < lives.MaxLives && coins.Coins >= coinCost;
        }
    }
}
