using System;
using Game.Core.Services;
using VContainer.Unity;

namespace Game.Game.Views.Gameplay
{
    // Nối kết quả màn vào chuỗi thắng/thua. Tách khỏi hệ popup như WinLifeRefundPresenter: scene
    // không gán PopupHost thì chuỗi vẫn đúng.
    //
    // ILevelOutcomeSource.Lost KHÔNG phải kết quả cuối — LevelReviveController gọi
    // ResumeAfterRevive() và màn quay lại InProgress, nên Resumed phải gỡ lần thua vừa ghi.
    public sealed class LevelStreakPresenter : IStartable, IDisposable
    {
        readonly ILevelOutcomeSource outcome;
        readonly I_LevelStreakService streaks;

        public LevelStreakPresenter(ILevelOutcomeSource outcome, I_LevelStreakService streaks)
        {
            this.outcome = outcome;
            this.streaks = streaks;
        }

        public void Start()
        {
            streaks.BeginLevel();

            outcome.Won += streaks.RecordWin;
            outcome.Lost += streaks.RecordLoss;
            outcome.Resumed += streaks.CancelLoss;
        }

        public void Dispose()
        {
            outcome.Won -= streaks.RecordWin;
            outcome.Lost -= streaks.RecordLoss;
            outcome.Resumed -= streaks.CancelLoss;

            // Rời màn giữa chừng (bấm Home) cũng là một lần thua. Cờ đã tắt khi màn kết thúc bình
            // thường, nên chỉ đường bỏ dở mới rơi vào đây.
            streaks.AbandonLevel();
        }
    }
}
