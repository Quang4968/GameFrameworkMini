using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Domain;

namespace Game.Game.Views.Popup
{
    // Cửa chặn khi hết booster, và là nơi DUY NHẤT mở popup Mua booster. Cùng khuôn với
    // OutOfLivesGate, thêm đúng một thứ: loại booster đang bán.
    //
    // Ai bị chặn thì gửi kèm việc đang định làm; mua xong là gate tự đóng popup rồi chạy tiếp
    // đúng việc đó. Nhờ vậy bấm nút Shuffle lúc hết booster, mua xong là XÁO LUÔN — người chơi
    // không phải bấm lần thứ hai.
    public sealed class BuyBoosterGate : IDisposable
    {
        readonly IPopupService popups;
        readonly CancellationTokenSource cts = new();

        Action pendingAction;

        public BuyBoosterGate(IPopupService popups)
        {
            this.popups = popups;
        }

        // Loại booster popup đang bán. Popup đọc lúc nó vẽ lại, nên gate phải đặt TRƯỚC khi mở.
        public BoosterType Type { get; private set; }

        // onBought = việc bị chặn, sẽ chạy NGAY sau khi mua thành công. Bỏ trống nghĩa là chỉ mở
        // popup để mua, không nối tiếp gì.
        public void Open(BoosterType type, Action onBought = null)
        {
            Type = type;

            // Ghi đè chứ không cộng dồn: lần mở sau luôn là ý định mới nhất của người chơi.
            pendingAction = onBought;

            popups.OpenAsync(PopupId.BuyBooster, hasOverlay: true, ct: cts.Token).Forget();
        }

        // PopupBuyBoosterController gọi khi cú mua thật sự ăn.
        public void NotifyBought()
        {
            // Nhấc việc ra TRƯỚC khi chạy: việc đó có thể mở lại chính cái gate này, không được
            // để nó nhặt lại đúng việc vừa chạy.
            var action = pendingAction;
            pendingAction = null;

            popups.Close(PopupId.BuyBooster);
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
