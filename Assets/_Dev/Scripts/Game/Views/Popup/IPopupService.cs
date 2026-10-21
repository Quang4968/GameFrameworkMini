using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Game.Views.Popup
{
    public interface IPopupService
    {
        // Raised when a popup goes from closed to open and back; reopening a popup that is already open raises
        // neither, so each Opened is matched by exactly one Closed.
        event Action<PopupId> Opened;
        event Action<PopupId> Closed;

        // Trả null khi: catalog chưa khai popup này, prefab hỏng, hoặc popup đang được mở dở
        // bởi một lời gọi khác.
        //
        // insideSafeArea = false đẩy popup sang container ngoài safe area: dành cho popup phủ kín
        // màn hình, để nền của nó tràn ra sau tai thỏ thay vì hở một dải ở mép trên.
        UniTask<IPopupView> OpenAsync(PopupId id, bool hasOverlay = true, bool insideSafeArea = true,
            CancellationToken ct = default);

        void Close(PopupId id);
        bool IsOpen(PopupId id);

        // How many popups are on screen right now. Whoever reacts to "a popup covers the game" reads this
        // instead of keeping its own tally, so an entry point that starts late still sees the popup already up.
        int OpenCount { get; }
    }
}
