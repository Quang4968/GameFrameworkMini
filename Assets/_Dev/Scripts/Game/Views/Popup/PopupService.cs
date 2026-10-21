using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Views.Popup
{
    // "Popup manager" của game: giữ sổ popup đang mở, quyết định khi nào bật/tắt overlay.
    // Cố tình là C# thuần (không MonoBehaviour) — mọi thứ đụng prefab đẩy hết qua IPopupHost.
    public sealed class PopupService : IPopupService
    {
        readonly IPopupHost host;
        readonly IAudioService audio;
        readonly Dictionary<PopupId, IPopupView> openPopups = new();
        readonly HashSet<PopupId> opening = new();

        public event Action<PopupId> Opened;
        public event Action<PopupId> Closed;

        public PopupService(IPopupHost host, IAudioService audio)
        {
            this.host = host;
            this.audio = audio;
        }

        public bool IsOpen(PopupId id) => openPopups.ContainsKey(id);

        public int OpenCount => openPopups.Count;

        public async UniTask<IPopupView> OpenAsync(PopupId id, bool hasOverlay = true,
            bool insideSafeArea = true, CancellationToken ct = default)
        {
            var newlyOpened = false;
            if (!openPopups.TryGetValue(id, out var view))
            {
                // Lần đầu mở phải nạp prefab qua Addressables nên mất vài frame. Chặn lời gọi thứ
                // hai chen vào giữa (bấm nút hai lần) để không dựng 2 instance cho cùng 1 popup.
                if (!opening.Add(id)) return null;

                IPopupView acquired;
                try
                {
                    acquired = await host.AcquireAsync(id, insideSafeArea, ct);
                }
                finally
                {
                    opening.Remove(id);
                }

                if (acquired == null) return null;

                // Sau await, scene có thể đã unload và instance đã bị destroy. Qua interface thì
                // toán tử == của Unity không chạy, phải ép về MonoBehaviour mới kiểm tra được.
                if (acquired is MonoBehaviour behaviour && behaviour == null) return null;

                view = acquired;
                view.CloseRequested += OnCloseRequested;
                openPopups.Add(id, view);
                newlyOpened = true;
            }

            if (hasOverlay) host.SetOverlayVisible(true);

            // Gọi Open lên popup đang mở vẫn kéo nó lên trên cùng và chạy lại animation:
            // mở lại luôn có nghĩa là "đưa popup này ra trước mặt".
            view.BringToFront();
            view.Open();

            // Tiếng đi kèm animation mở, nên nằm sau view.Open() chứ không phải sau await: lúc
            // await xong prefab mới nạp xong, popup còn chưa hiện. Prewarm không đi qua đây nên
            // popup dựng sẵn lúc vào scene không kêu.
            audio.Play(AudioCue.PopupOpen);

            if (newlyOpened) Opened?.Invoke(id);
            return view;
        }

        public void Close(PopupId id)
        {
            if (!openPopups.TryGetValue(id, out var view)) return;

            view.CloseRequested -= OnCloseRequested;
            openPopups.Remove(id);
            host.Release(view);

            audio.Play(AudioCue.PopupClose);

            // Overlay chỉ tắt khi KHÔNG còn popup nào mở, bất kể popup nào đã bật nó lên.
            if (openPopups.Count == 0) host.SetOverlayVisible(false);

            Closed?.Invoke(id);
        }

        void OnCloseRequested(IPopupView view)
        {
            foreach (var pair in openPopups)
            {
                if (!ReferenceEquals(pair.Value, view)) continue;

                Close(pair.Key);
                return;
            }
        }
    }
}
