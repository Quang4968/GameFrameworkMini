using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Game.Views.Popup
{
    // Tách khỏi IPopupService vì lấy/trả một popup nghĩa là đụng tới Addressables, Canvas và
    // Instantiate — những thứ PopupService (C# thuần) không được biết.
    public interface IPopupHost
    {
        // Async vì lần đầu mở một popup phải nạp prefab qua Addressables. Từ lần thứ hai trở đi
        // instance đã nằm sẵn trong cache nên hàm này trả về ngay trong cùng frame.
        //
        // insideSafeArea = false thì popup được gắn vào container NGOÀI safe area, dành cho popup
        // phủ kín màn hình (Shop) — trong container mặc định nó sẽ hở một dải ở mép trên.
        UniTask<IPopupView> AcquireAsync(PopupId id, bool insideSafeArea = true,
            CancellationToken ct = default);

        void Release(IPopupView view);
        void SetOverlayVisible(bool visible);
    }
}
