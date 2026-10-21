using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Game.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;

namespace Game.Game.Views.Popup
{
    // Nửa "đụng Unity" của hệ popup: nạp prefab qua Addressables, dựng instance và bật tắt overlay.
    //
    // Mỗi PopupId chỉ tồn tại tối đa 1 instance — đúng bằng sổ popup của PopupService — nên đóng
    // popup là tắt object rồi giữ lại dùng tiếp, không Destroy.
    //
    // Asset được giữ tới hết vòng đời scene rồi trả một lượt ở OnDestroy: mở lại popup là tức thì,
    // còn popup chưa bao giờ mở thì không tốn RAM. Riêng các popup khai trong prewarmPopups được
    // nạp + dựng sẵn (ẩn) ngay lúc vào scene, đổi một ít RAM lấy lần mở đầu không khựng.
    [DisallowMultipleComponent]
    public sealed class PopupHost : MonoBehaviour, IPopupHost
    {
        [SerializeField] PopupCatalogSO catalog;
        [SerializeField] PopupAnimationSettingsSO animationSettings;
        [Tooltip("Nơi popup được gắn vào — thường là 1 object rỗng nằm trong Canvas.")]
        [SerializeField] RectTransform container;

        [Tooltip("Container thứ hai, đặt NGOÀI safe area và stretch kín Canvas. Popup mở với " +
            "insideSafeArea = false vào đây, để nền phủ kín màn hình tràn ra sau tai thỏ. Bỏ " +
            "trống thì mọi popup dùng chung container trên.")]
        [SerializeField] RectTransform fullScreenContainer;
        [Tooltip("Lớp mờ chặn tương tác phía sau popup. Để trống nếu màn này không cần.")]
        [SerializeField] GameObject overlay;

        [Tooltip("Popup được nạp và dựng sẵn (ở trạng thái ẩn) ngay khi vào scene, để lần mở đầu " +
            "tiên không khựng vì phải chờ Addressables nạp prefab rồi Instantiate cả cây UI. " +
            "Chỉ khai những popup màn này thực sự dùng — popup ngoài danh sách vẫn nạp lười như cũ.")]
        [SerializeField] PopupId[] prewarmPopups = new PopupId[0];

        readonly Dictionary<PopupId, PopupView> instances = new();
        readonly Dictionary<PopupId, AsyncOperationHandle<GameObject>> handles = new();

        IObjectResolver resolver;

        [Inject]
        public void Construct(IObjectResolver objectResolver)
        {
            resolver = objectResolver;
        }

        void Awake()
        {
            if (overlay != null) overlay.SetActive(false);
        }

        void Start()
        {
            // Scene không đăng ký host vào scope thì resolver còn null — hệ popup coi như tắt,
            // prewarm cũng bỏ qua luôn.
            if (resolver == null || prewarmPopups.Length == 0) return;

            PrewarmAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        // Trả trước chi phí nạp prefab + Instantiate vào lúc vào scene thay vì lúc bấm nút.
        // Instance sinh ra từ LoadAsync đã ở trạng thái ẩn nên người chơi không thấy gì.
        async UniTaskVoid PrewarmAsync(CancellationToken ct)
        {
            foreach (var id in prewarmPopups)
            {
                if (instances.ContainsKey(id)) continue;

                // Prewarm luôn dựng vào container mặc định: lúc này chưa biết ai sẽ mở popup và
                // mở với cờ nào. AcquireAsync sẽ dời sang container kia nếu cần.
                await LoadAsync(id, container, ct);

                // Giãn mỗi popup một frame để chính bước prewarm không dồn mấy cú Instantiate
                // vào cùng một frame lúc scene vừa lên.
                await UniTask.Yield(ct);
            }
        }

        void OnDestroy()
        {
            // Destroy instance TRƯỚC khi Release, để không có instance nào sống sót qua thời điểm
            // asset gốc bị gỡ khỏi bộ nhớ.
            foreach (var instance in instances.Values)
            {
                if (instance != null) Destroy(instance.gameObject);
            }

            instances.Clear();

            foreach (var handle in handles.Values)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }

            handles.Clear();
        }

        public async UniTask<IPopupView> AcquireAsync(PopupId id, bool insideSafeArea = true,
            CancellationToken ct = default)
        {
            var parent = ContainerFor(insideSafeArea);

            if (!instances.TryGetValue(id, out var view))
            {
                view = await LoadAsync(id, parent, ct);
                if (view == null) return null;
            }
            else if (view.transform.parent != parent)
            {
                // Instance được dùng lại, mà prewarm luôn dựng vào container mặc định — nên chỗ
                // ngồi phải chốt ở đây chứ không phải lúc dựng.
                view.transform.SetParent(parent, false);
            }

            view.Show();
            view.BringToFront();
            return view;
        }

        public void Release(IPopupView view)
        {
            if (view is PopupView popupView) popupView.Hide();
        }

        public void SetOverlayVisible(bool visible)
        {
            if (overlay != null) overlay.SetActive(visible);
        }

        // Scene chưa gán container ngoài safe area thì popup vẫn mở được, chỉ là nó ngồi chung
        // container mặc định như trước — mất phần tràn ra sau tai thỏ, không hỏng gì khác.
        RectTransform ContainerFor(bool insideSafeArea)
        {
            if (insideSafeArea) return container;

            return fullScreenContainer != null ? fullScreenContainer : container;
        }

        async UniTask<PopupView> LoadAsync(PopupId id, RectTransform parent, CancellationToken ct)
        {
            var reference = catalog != null ? catalog.GetReference(id) : null;
            if (reference == null || !reference.RuntimeKeyIsValid())
            {
                Debug.LogError($"[PopupHost] Popup '{id}' chưa khai trong PopupCatalog, hoặc prefab " +
                    "chưa được đánh dấu Addressable.", this);
                return null;
            }

            var handle = Addressables.LoadAssetAsync<GameObject>(reference);

            GameObject prefab;
            try
            {
                prefab = await handle.ToUniTask(cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                Addressables.Release(handle);
                throw;
            }

            // Sau await, host có thể đã bị destroy vì đổi scene giữa chừng.
            if (this == null || prefab == null)
            {
                Addressables.Release(handle);
                return null;
            }

            // Cũng trong lúc await, một lời gọi khác có thể đã nạp xong đúng popup này.
            if (instances.TryGetValue(id, out var existing))
            {
                Addressables.Release(handle);
                return existing;
            }

            var prefabView = prefab.GetComponent<PopupView>();
            if (prefabView == null)
            {
                Debug.LogError($"[PopupHost] Prefab của popup '{id}' không có PopupView ở root.", this);
                Addressables.Release(handle);
                return null;
            }

            // Dựng qua container để mọi [Inject] trong prefab được scope của scene này rót vào.
            // Nhờ vậy host KHÔNG cần biết popup nào cần service gì: thêm popup mới không phải sửa
            // file này, và scene không mở popup đó cũng không bị bắt đăng ký service của nó.
            var view = resolver.Instantiate(prefabView, parent);
            view.Bind(animationSettings);

            // Instance sinh ra là ẨN — chỉ AcquireAsync (gọi Show ngay sau) mới làm nó hiện.
            // Nhờ vậy prewarm chạy song song với một cú mở thật cũng không giấu nhầm popup
            // mà người chơi vừa mở. Tắt thẳng, không qua animation đóng.
            view.HideInstant();

            instances.Add(id, view);
            handles.Add(id, handle);
            return view;
        }
    }
}
