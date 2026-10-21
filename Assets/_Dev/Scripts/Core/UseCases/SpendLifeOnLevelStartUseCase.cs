using System;
using Game.Core.Services;

namespace Game.Core.UseCases
{
    // Vào màn = mất tim NGAY, chứ không đợi thua mới trừ. Trừ trước rồi thắng mới hoàn lại
    // (RefundLifeOnWinUseCase) là cách duy nhất khiến việc bỏ màn giữa chừng phải trả giá kể cả
    // khi người chơi tắt hẳn app: lúc đó không còn đoạn code nào của mình chạy để mà trừ.
    public static class SpendLifeOnLevelStartUseCase
    {
        // Trả về số tim THẬT SỰ bị trừ — 0 nghĩa là người chơi đã hết tim từ trước. Chỗ gọi đã
        // chặn bằng CanStartLevelUseCase nên 0 gần như chỉ xảy ra khi mở thẳng scene MainGame
        // trong Editor; khi đó vẫn cho chơi tiếp chứ không kẹt màn.
        public static int Execute(ILifeService lives, int livesPerLevel)
        {
            if (lives == null || livesPerLevel <= 0) return 0;

            // Refresh first, or a window that ran out a second ago would still let this level in free.
            lives.Refresh();
            if (lives.UnlimitedTimeLeft > TimeSpan.Zero) return 0;

            return lives.TrySpend(livesPerLevel) ? livesPerLevel : 0;
        }
    }
}
