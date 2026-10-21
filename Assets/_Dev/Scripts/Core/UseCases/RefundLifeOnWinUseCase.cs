using Game.Core.Services;

namespace Game.Core.UseCases
{
    // Thắng màn thì hoàn lại đúng số tim đã trả lúc vào màn (SpendLifeOnLevelStartUseCase). Chơi
    // giỏi thì không tốn tim; thua hoặc bỏ ngang thì mất — luật "mất tim khi thua" nằm ở đây chứ
    // không cần một cú trừ riêng lúc thua.
    public static class RefundLifeOnWinUseCase
    {
        // Returns what landed: 0 on a full bar, since Add clamps. charged is what entry actually cost,
        // not LivesPerLevel, so a free entry (onboarding, unlimited window) refunds nothing even when
        // the window ran out before the win.
        public static int Execute(ILifeService lives, int charged)
        {
            if (lives == null || charged <= 0) return 0;

            var before = lives.Lives;
            lives.Add(charged);
            return lives.Lives - before;
        }
    }
}
