using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Domain;
using Game.Core.Services;
using Game.Core.UseCases;
using Game.Game.SceneFlow;

namespace Game.Game.Boot
{
    // Home's Play action: whatever play button the game's Home UI draws calls RequestPlay.
    public sealed class HomeBootstrap : IDisposable
    {
        readonly ISceneLoader sceneLoader;
        readonly ILifeService lives;
        readonly I_LevelSessionService session;
        readonly CancellationTokenSource cts = new();

        bool isLoading;

        // Bắn ra khi bấm Play mà hết tim. Không tự mở popup ở đây — cố tình tách khỏi hệ popup
        // (như GameResultPopupPresenter tách khỏi WinCoinRewardPresenter) để nút Play không phụ
        // thuộc IPopupService: scene không gán PopupHost thì bấm Play lúc hết tim chỉ không làm gì,
        // không văng lỗi. OutOfLivesGatePresenter nghe sự kiện này để mở popup.
        public event Action LivesExhausted;

        public HomeBootstrap(ISceneLoader sceneLoader, ILifeService lives, I_LevelSessionService session)
        {
            this.sceneLoader = sceneLoader;
            this.lives = lives;
            this.session = session;
        }

        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
        }

        // Public vì OutOfLivesGatePresenter gửi chính hàm này vào gate làm "việc đang chờ": mua tim
        // xong là chạy lại đúng cú bấm Play vừa bị chặn. Không sợ lặp vô hạn — lần chạy lại đã có
        // tim nên đi thẳng vào nhánh load scene.
        public void RequestPlay()
        {
            if (isLoading) return;

            if (!CanStartLevelUseCase.Execute(lives))
            {
                LivesExhausted?.Invoke();
                return;
            }

            // play_type của lần chơi sắp tới, ghi TRƯỚC khi đổi scene vì MainGame đọc lại từ đĩa.
            session?.SetNextPlayType(E_LevelPlayType.Home);

            isLoading = true;
            LoadMainGameAsync().Forget();
        }

        async UniTaskVoid LoadMainGameAsync()
        {
            await sceneLoader.LoadSceneAsync(GameScenes.MainGame, cts.Token);
        }
    }
}
