using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Game.Views.Popup
{
    // Cửa chặn khi hết tim, và là nơi DUY NHẤT mở popup Hết tim.
    //
    // Ai bị chặn thì gửi kèm việc đang định làm; nạp tim xong là gate tự đóng popup rồi chạy tiếp
    // đúng việc đó. Nhờ vậy bấm Play lúc hết tim, mua tim xong là VÀO MÀN LUÔN — người chơi không
    // phải bấm Play lần thứ hai.
    //
    // Mở từ nút "+" trên HUD thì không kèm việc gì: mua xong chỉ đóng popup, đứng yên ở Home.
    public sealed class OutOfLivesGate : IDisposable
    {
        readonly IPopupService popups;
        readonly CancellationTokenSource cts = new();

        Action pendingAction;

        public OutOfLivesGate(IPopupService popups)
        {
            this.popups = popups;
        }

        // onRefilled = việc bị chặn, sẽ chạy NGAY sau khi mua tim thành công. Bỏ trống nghĩa là
        // chỉ mở popup để mua, không nối tiếp gì.
        public void Open(Action onRefilled = null)
        {
            // Ghi đè chứ không cộng dồn: lần mở sau luôn là ý định mới nhất của người chơi.
            pendingAction = onRefilled;

            popups.OpenAsync(PopupId.OutOfLives, hasOverlay: true, ct: cts.Token).Forget();
        }

        // PopupOutOfLivesController gọi khi cú mua thật sự ăn.
        public void NotifyRefilled()
        {
            // Nhấc việc ra TRƯỚC khi chạy: việc đó có thể mở lại chính cái gate này (đổi scene,
            // mở popup khác), không được để nó nhặt lại đúng việc vừa chạy.
            var action = pendingAction;
            pendingAction = null;

            popups.Close(PopupId.OutOfLives);
            action?.Invoke();
        }

        // Người chơi tự đóng popup mà không mua thì việc đang chờ coi như bỏ — không để nó nổ ra
        // ở một lần mở sau chẳng liên quan.
        public void Cancel()
        {
            pendingAction = null;
        }

        public void Dispose()
        {
            pendingAction = null;
            cts.Cancel();
            cts.Dispose();
        }
    }
}
