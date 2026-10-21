using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using Game.Game.Animation;
using Game.Game.Data;
using Game.Game.SceneFlow;
using Game.Game.Views.UI;
using UnityEngine;

namespace Game.Game.Views.Transition
{
    // Màn loading che giữa hai scene. Object này sống xuyên scene (DontDestroyOnLoad, do
    // SceneTransitionProvider dựng) vì nó phải che đúng lúc scene cũ bị huỷ và scene mới dựng lên.
    //
    // Việc của class này chỉ là điều phối: mờ vào, mở lời cho các hiệu ứng con, giữ đủ lâu, mờ ra.
    // Dấu chấm nằm ở LoadingDotsLabel, cú bung icon nằm ở PopInEffect — cả hai đều dùng lại được ở
    // chỗ khác và tự lo phần của mình.
    //
    // Nhịp của một lần đổi scene, và lý do nó xếp theo đúng thứ tự này:
    //
    //   CloseAsync   mờ vào -> icon bung ra (đợi xong)     <- scene cũ còn chạy 60fps, animation mượt
    //   ---------------------------------------------------------------------------------------
    //   loader       load scene, kích hoạt, hold vài frame <- toàn frame dài cả trăm ms
    //   ---------------------------------------------------------------------------------------
    //   OpenAsync    đợi cho đủ MinVisibleSeconds -> mờ ra <- scene mới đã dựng xong, lại mượt
    //
    // Mọi animation phải nằm NGOÀI khúc giữa. Thứ duy nhất chạy trong lúc load là dấu chấm, vì nó chỉ
    // đổi text chứ không phải chuyển động liên tục nên frame dài không lộ.
    //
    // Prefab được lưu ở trạng thái HIỆN RÕ (canvas bật, alpha = 1) để mở Prefab Mode ra là thấy và
    // chỉnh được như một màn hình bình thường. Đừng lưu nó ở trạng thái tắt cho "an toàn": Awake gọi
    // SnapOpen ngay trong lần Instantiate, trước frame đầu tiên, nên lúc chạy không có nhịp nào bị lộ.
    [DisallowMultipleComponent]
    public sealed class LoadingTransitionView : MonoBehaviour, ISceneTransition
    {
        [Header("Thành phần")]
        [Tooltip("Canvas của màn loading. Bị tắt lúc rảnh để không tốn draw call.")]
        [SerializeField] Canvas canvas;

        [Tooltip("Vừa là thứ được mờ vào/mờ ra, vừa chặn tương tác xuống scene phía sau.")]
        [SerializeField] CanvasGroup canvasGroup;

        [Header("Hiệu ứng")]
        [Tooltip("Chữ 'Loading' chạy dấu chấm. Bật/tắt theo màn loading, bỏ trống thì chữ đứng yên.")]
        [SerializeField] LoadingDotsLabel dots;

        [Tooltip("Icon bung ra sau khi nền đã che kín. Bỏ trống thì bỏ qua nhịp này.")]
        [SerializeField] PopInEffect iconPop;

        SceneTransitionSettingsSO settings;
        MotionHandle fadeMotion;
        float shownSince;

        public bool IsClosed { get; private set; }

        void Awake()
        {
            if (!HasCanvas()) return;

            SnapOpen();
        }

        void OnDestroy()
        {
            fadeMotion.TryCancel();
        }

        // Provider gọi ngay sau Instantiate: prefab nằm ngoài scene nên không tự nhận được
        // dependency từ LifetimeScope, phải đưa tay như PopupView.Bind.
        public void Bind(SceneTransitionSettingsSO transitionSettings)
        {
            settings = transitionSettings;
        }

        public async UniTask CloseAsync(CancellationToken ct = default)
        {
            if (IsClosed || !IsWiredUp()) return;

            SetVisible(true);
            shownSince = Time.realtimeSinceStartup;

            await FadeAsync(1f, settings.FadeInDuration, ct);

            // Icon bung SAU khi nền che kín — bung chồng vào nhịp mờ vào thì đoạn nó còn bé xíu rơi đúng
            // lúc màn hình còn trong suốt, coi như mất cú bung. Và phải bung XONG rồi mới trả về: hàm này
            // trả về là loader bắt đầu load scene, từ đó toàn frame dài, animation nào rơi vào đấy cũng
            // giật từng nấc.
            if (iconPop != null) await iconPop.PlayAsync(ct);

            IsClosed = true;
        }

        public async UniTask OpenAsync(CancellationToken ct = default)
        {
            if (!IsClosed || !IsWiredUp()) return;

            // Sàn thời gian hiển thị. Scene nhẹ load xong trong vài trăm ms, mở ra ngay thì màn loading
            // chỉ loé một cái. Load lâu hơn sàn này thì đoạn chờ bằng 0, không cộng thêm gì.
            var remaining = settings.MinVisibleSeconds - (Time.realtimeSinceStartup - shownSince);
            if (remaining > 0f)
            {
                // Chỉ rõ DelayType của UniTask: LitMotion cũng có một enum trùng tên.
                await UniTask.Delay(TimeSpan.FromSeconds(remaining),
                    Cysharp.Threading.Tasks.DelayType.UnscaledDeltaTime, cancellationToken: ct);
            }

            // Bỏ chặn ngay từ đầu nhịp mờ ra: người chơi bấm được vào scene mới trong lúc màn loading
            // còn đang nhạt dần, không phải đợi hết animation.
            canvasGroup.blocksRaycasts = false;
            await FadeAsync(0f, settings.FadeOutDuration, ct);

            SnapOpen();
        }

        // Luôn xuất phát từ độ mờ hiện tại, để lệnh che/mở chen ngang lúc animation còn đang chạy thì
        // nó nối tiếp chứ không nháy về đầu.
        UniTask FadeAsync(float targetAlpha, float duration, CancellationToken ct)
        {
            fadeMotion.TryCancel();
            fadeMotion = LMotion.Create(canvasGroup.alpha, targetAlpha, duration)
                // Màn loading phải chạy cả khi game đang dừng (timeScale = 0) — ví dụ bấm nút thoát
                // trong popup pause.
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToAlpha(canvasGroup)
                .AddTo(gameObject);

            return UniTask.WaitWhile(() => fadeMotion.IsActive(), cancellationToken: ct);
        }

        void SnapOpen()
        {
            fadeMotion.TryCancel();
            canvasGroup.alpha = 0f;

            if (iconPop != null) iconPop.ResetToStart();

            IsClosed = false;
            SetVisible(false);
        }

        void SetVisible(bool value)
        {
            canvas.enabled = value;
            canvasGroup.blocksRaycasts = value;

            // Dấu chấm chỉ chạy lúc màn loading đang hiện; bật lại là nó tự đếm từ chữ trần.
            if (dots != null) dots.enabled = value;
        }

        bool IsWiredUp()
        {
            if (!HasCanvas()) return false;

            if (settings == null)
            {
                Debug.LogError($"[LoadingTransitionView] '{name}' chưa được Bind SceneTransitionSettings.", this);
                return false;
            }

            return true;
        }

        // Tách riêng khỏi IsWiredUp vì Awake cần kiểm tra hai ref này lúc chưa ai kịp Bind settings.
        bool HasCanvas()
        {
            if (canvas != null && canvasGroup != null) return true;

            Debug.LogError($"[LoadingTransitionView] '{name}' thiếu Canvas hoặc CanvasGroup.", this);
            return false;
        }
    }
}
