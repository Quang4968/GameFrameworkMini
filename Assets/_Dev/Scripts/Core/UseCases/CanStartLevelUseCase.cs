using System;
using Game.Core.Services;

namespace Game.Core.UseCases
{
    // Có được vào màn mới không (từ Home, hoặc bấm chơi lại/màn kế ở popup Thắng/Thua). Hết tim thì
    // không, kể cả khi tim đang trên đường hồi — chỗ gọi tự Refresh() trước để không chặn oan lúc
    // tim vừa hồi đủ xong.
    public static class CanStartLevelUseCase
    {
        public static bool Execute(ILifeService lives)
        {
            if (lives == null) return true;

            lives.Refresh();
            return lives.Lives > 0 || lives.UnlimitedTimeLeft > TimeSpan.Zero;
        }
    }
}
