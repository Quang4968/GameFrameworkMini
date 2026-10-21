using System;

namespace Game.Game.Views.Popup
{
    public interface IPopupView
    {
        // View tự xin đóng (bấm nút X, bấm ra ngoài...) chứ không tự tắt mình,
        // vì chỉ PopupService mới biết còn popup nào đang mở để tắt overlay hay không.
        event Action<IPopupView> CloseRequested;

        void Open();
        void BringToFront();
    }
}
