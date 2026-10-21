using System;
using Game.Game.Boot;
using VContainer.Unity;

namespace Game.Game.Views.Popup
{
    // Nối nút Play của Home với cửa chặn hết tim. Tách khỏi HomeBootstrap vì lý do y hệt
    // GameResultPopupPresenter: nút Play không được phụ thuộc hệ popup, chỉ đăng ký presenter này
    // khi scene thật sự có PopupHost.
    //
    // Việc gửi kèm là chính RequestPlay: mua tim xong, gate gọi lại nó và người chơi vào màn ngay,
    // không phải bấm Play lần thứ hai.
    public sealed class OutOfLivesGatePresenter : IStartable, IDisposable
    {
        readonly HomeBootstrap homeBootstrap;
        readonly OutOfLivesGate gate;

        public OutOfLivesGatePresenter(HomeBootstrap homeBootstrap, OutOfLivesGate gate)
        {
            this.homeBootstrap = homeBootstrap;
            this.gate = gate;
        }

        public void Start()
        {
            homeBootstrap.LivesExhausted += OpenGate;
        }

        public void Dispose()
        {
            homeBootstrap.LivesExhausted -= OpenGate;
        }

        void OpenGate() => gate.Open(homeBootstrap.RequestPlay);
    }
}
