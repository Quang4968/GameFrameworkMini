using System;
using LitMotion;
using LitMotion.Extensions;
using Game.Core.Services;
using Game.Game.Data;
using Game.Game.Views.UI;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Game.Views.Popup
{
    // Base của mọi popup: chỉ lo animation hiện ra và nút đóng.
    // Nội dung riêng của từng popup (điểm, text, nút chơi lại...) nằm ở component khác gắn
    // cùng prefab — tách như BoardView/PieceView, không kế thừa chồng lên nhau.
    public class PopupView : MonoBehaviour, IPopupView
    {
        [Tooltip("Khung popup được animate. KHÔNG phải object gốc — object gốc bị bật/tắt khi đóng.")]
        [SerializeField] RectTransform root;
        [SerializeField] CanvasGroup canvasGroup;
        [Tooltip("Không bắt buộc. Bấm vào sẽ phát CloseRequested để PopupService đóng popup.")]
        [SerializeField] Button closeButton;

        public event Action<IPopupView> CloseRequested;

        // The popup actually reaching the screen. Not OnEnable: PopupHost builds an instance with
        // resolver.Instantiate, which activates it AFTER injecting and then hides it again, so
        // OnEnable also runs for popups nobody opened - every prewarmed one included.
        public event Action<IPopupView> Opened;

        PopupAnimationSettingsSO settings;
        IHapticService haptics;
        MotionHandle motion;
        bool closing;

        // PopupHost dựng prefab qua container nên [Inject] ở đây được rót bình thường — khác
        // PopupAnimationSettings phải đưa tay vì nó là asset của host chứ không phải service.
        [Inject]
        public void Construct(IHapticService hapticService)
        {
            haptics = hapticService;
        }

        void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(RequestClose);
        }

        void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(RequestClose);
            motion.TryCancel();
        }

        // PopupHost gọi ngay sau khi Instantiate: prefab nằm ngoài scene nên không tự nhận
        // được dependency từ LifetimeScope, phải đưa tay như thế này.
        public void Bind(PopupAnimationSettingsSO animationSettings)
        {
            settings = animationSettings;
        }

        // Mọi popup trong game hiện ra bằng một kiểu duy nhất: phóng to từ nhỏ tại chỗ.
        public void Open()
        {
            motion.TryCancel();

            if (settings == null)
            {
                Debug.LogError($"[PopupView] '{name}' chưa được Bind PopupAnimationSettings.", this);
                motion = default;
                ResetToResting();
                return;
            }

            motion = PlayAppear();
        }

        public void BringToFront() => transform.SetAsLastSibling();

        // PopupHost bật/tắt popup chứ không Destroy, nên hai hàm này là "vòng đời" thật của view.
        public void Show()
        {
            motion.TryCancel();
            closing = false;
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            ResetToResting();

            Opened?.Invoke(this);
        }

        // Đóng bằng animation thu nhỏ + mờ dần, object chỉ tắt khi animation chạy xong.
        public void Hide()
        {
            // Đang chạy animation đóng dở thì để nó chạy tiếp, khởi động lại sẽ nháy về cỡ thật.
            if (closing) return;

            motion.TryCancel();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            foreach (var listener in GetComponentsInChildren<IPopupClosing>(true))
            {
                listener.OnPopupClosing();
            }

            // Chưa Bind settings, hoặc object đang tắt (LitMotion không chạy trên object tắt)
            // thì không có gì để animate.
            if (settings == null || !gameObject.activeInHierarchy)
            {
                HideInstant();
                return;
            }

            closing = true;
            motion = PlayDisappear();
        }

        // Tắt không animation — dùng lúc PopupHost vừa dựng instance và cần giấu nó ngay.
        public void HideInstant()
        {
            motion.TryCancel();
            closing = false;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        MotionHandle PlayAppear()
        {
            root.anchoredPosition = Vector2.zero;

            var duration = settings.AppearDuration;

            // Phóng vọt qua cỡ thật rồi lùi về 1 — cú nảy nhẹ đặc trưng của preset gốc.
            //
            // Unscaled: a popup over the level stops the clock, and a popup that animates on that clock
            // would never finish opening.
            var handle = LSequence.Create()
                .Append(LMotion.Create(settings.AppearScaleStart, settings.AppearScaleOvershoot, duration * 0.7f)
                    .WithEase(Ease.OutCubic)
                    .BindToLocalScaleXYZ(root))
                .Append(LMotion.Create(settings.AppearScaleOvershoot, 1f, duration * 0.3f)
                    .WithEase(Ease.OutQuad)
                    .BindToLocalScaleXYZ(root))
                .Run(builder => builder.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale))
                .AddTo(gameObject);

            // Đặt cỡ xuất phát SAU khi dựng sequence: lúc bind, đoạn thứ hai ghi luôn giá trị đầu
            // của nó (overshoot) vào scale, để nguyên thì popup nháy 1 frame ở cỡ phóng to.
            root.localScale = Vector3.one * settings.AppearScaleStart;
            return handle;
        }

        MotionHandle PlayDisappear()
        {
            var duration = settings.DisappearDuration;

            return LSequence.Create()
                .Append(LMotion.Create(1f, settings.DisappearScaleEnd, duration)
                    .WithEase(Ease.InQuad)
                    .BindToLocalScaleXYZ(root))
                .Join(LMotion.Create(1f, 0f, duration)
                    .WithEase(Ease.InQuad)
                    .BindToAlpha(canvasGroup))
                .Run(builder => builder
                    .WithOnComplete(HideInstant)
                    .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale))
                .AddTo(gameObject);
        }

        void ResetToResting()
        {
            root.anchoredPosition = Vector2.zero;
            root.localScale = Vector3.one;
        }

        void RequestClose()
        {
            haptics.PlayButtonTap();
            RequestCloseSilent();
        }

        // Cho component khác trên cùng prefab xin đóng — vd. nút Home đóng popup chứa nó khi
        // chuyển sang popup xác nhận. Không rung: chỗ gọi đã tự bắn haptic của cú bấm rồi.
        public void RequestCloseSilent() => CloseRequested?.Invoke(this);
    }
}
