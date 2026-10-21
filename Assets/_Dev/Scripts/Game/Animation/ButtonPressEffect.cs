using System;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Game.Animation
{
    // Hiệu ứng nhún khi bấm, dùng CHUNG cho mọi nút trong game: gắn component vào object có
    // Button là xong, không cần gán reference hay đăng ký gì thêm.
    //
    // Nhịp để mặc định ngay trong code chứ không tách ScriptableObject: nút nằm rải khắp scene
    // lẫn prefab nạp bằng Addressables, mỗi chỗ phải gán tay một asset thì sớm muộn cũng lệch
    // nhau. Muốn đổi nhịp chung cho cả game thì sửa giá trị mặc định ở đây.
    //
    // Component chỉ động vào visual target, không đụng tới onClick — nút vẫn tự lo hành vi của nó.
    //
    // Scale style shrinks the object that also carries the raycast graphic, so the hit rect shrinks
    // with it: a tap on the outer edge presses down but releases outside, and Unity drops the click.
    // Buttons whose art fills the rect need negative m_RaycastPadding on that graphic to keep a
    // margin (see the booster buttons in PopupGameplay).
    public sealed class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        enum PressStyle
        {
            Scale,
            MoveDown,
            CoreScale
        }

        [Tooltip("Visual bị scale/dịch xuống. Bỏ trống thì lấy chính object này, riêng Core Scale " +
            "lấy con đầu tiên vì kiểu đó nhắm vào lõi nút chứ không phải nút cha.")]
        [SerializeField] Transform target;

        [Tooltip("Scale: thu nhỏ target. Move Down: dịch riêng RectTransform target xuống như lõi nút 3D. " +
            "Core Scale: thu nhỏ lõi nút kèm mờ đi, dùng cho nút có phần cha và phần lõi tách riêng.")]
        [SerializeField] PressStyle pressStyle;

        [Tooltip("Khoảng cách lõi nút đi xuống khi dùng Move Down.")]
        [SerializeField, Min(0f)] float pressedOffset = 14f;

        [Tooltip("Cỡ lúc đang giữ nút, tính theo cỡ gốc của target.")]
        [SerializeField, Range(0.5f, 1f)] float pressedScale = 0.92f;

        [Tooltip("Độ mờ của lõi nút lúc đang giữ khi dùng Core Scale, để theo thang 0-255 cho khớp " +
            "bảng màu bên thiết kế.")]
        [SerializeField, Range(0, 255)] int pressedAlpha = 120;

        [Tooltip("Số giây nhún xuống lúc bấm.")]
        [SerializeField, Range(0.01f, 0.3f)] float pressDuration = 0.06f;

        [Tooltip("Số giây bật lại cỡ gốc lúc nhả tay.")]
        [SerializeField, Range(0.05f, 0.5f)] float releaseDuration = 0.16f;

        [Tooltip("Cỡ phóng vọt qua cỡ gốc trong khoảnh khắc trước khi lùi về — cú nảy nhẹ.")]
        [SerializeField, Range(1f, 1.2f)] float releaseOvershoot = 1.05f;

        [Tooltip("Nút bị tắt interactable thì mờ đi cả nút — viền, lõi, decor, icon. Tắt cho nút " +
            "cần giữ nguyên vẻ ngoài lúc khoá.")]
        [SerializeField] bool fadesWhenDisabled = true;

        [Tooltip("Độ đục của nút lúc khoá.")]
        [SerializeField, Range(0f, 1f)] float disabledAlpha = 0.4f;

        [Tooltip("Số giây mờ đi và sáng lại.")]
        [SerializeField, Range(0f, 0.5f)] float fadeDuration = 0.1f;

        Selectable selectable;
        Vector3 baseScale;
        RectTransform rectTarget;
        Vector2 baseAnchoredPosition;
        CanvasGroup coreGroup;
        Graphic[] fadeGraphics;
        bool fadedOut;
        float baseAlpha = 1f;
        float pressAmount;
        MotionHandle motion;
        bool isPressed;

        void Awake()
        {
            if (target == null) target = DefaultTarget();

            // Cỡ gốc đọc một lần lúc Awake: mọi motion sau đó nhân vào giá trị này, nên nút có
            // scale khác 1 trong prefab vẫn nhún đúng tỉ lệ.
            baseScale = target.localScale;
            rectTarget = target as RectTransform;
            if (rectTarget != null) baseAnchoredPosition = rectTarget.anchoredPosition;
            selectable = GetComponent<Selectable>();

            if (pressStyle == PressStyle.CoreScale) CacheCoreGroup();

            CacheFadeGraphics();
        }

        // Core Scale nhắm vào lõi nút — phần con nằm trong khung cha — nên bỏ trống thì lấy con đầu
        // tiên, đúng như hai kiểu kia bỏ trống thì lấy chính nút.
        Transform DefaultTarget()
        {
            if (pressStyle == PressStyle.CoreScale && transform.childCount > 0) return transform.GetChild(0);
            return transform;
        }

        // Lõi nút gồm cả nền lẫn chữ, mờ đi bằng CanvasGroup thì cả cụm mờ đều một lượt, khỏi phải
        // dò từng Graphic con. Thiếu thì tự thêm để preset này giữ đúng tinh thần gắn vào là chạy.
        void CacheCoreGroup()
        {
            coreGroup = target.GetComponent<CanvasGroup>();
            if (coreGroup == null) coreGroup = target.gameObject.AddComponent<CanvasGroup>();
            baseAlpha = coreGroup.alpha;
        }

        // ColorTint already owns targetGraphic; sharing it makes the two writers fight over one
        // CanvasRenderer, so hand that layer the same alpha and fade the rest here.
        void CacheFadeGraphics()
        {
            if (!fadesWhenDisabled) return;

            var owned = selectable != null && selectable.transition == Selectable.Transition.ColorTint
                ? selectable.targetGraphic
                : null;

            if (owned != null)
            {
                var colors = selectable.colors;
                colors.disabledColor = new Color(1f, 1f, 1f, disabledAlpha);
                selectable.colors = colors;
            }

            var all = GetComponentsInChildren<Graphic>(true);
            fadeGraphics = owned == null ? all : Array.FindAll(all, graphic => graphic != owned);
        }

        // CrossFadeAlpha writes the CanvasRenderer, not Graphic.color, so a button that recolours
        // itself at runtime keeps its own colour.
        void RefreshDisabledFade(bool instant)
        {
            if (fadeGraphics == null) return;

            // Not IsInteractable(): that counts the parent CanvasGroup, which PopupView switches
            // off as the close animation starts.
            var disabled = selectable != null && !selectable.interactable;
            if (!instant && disabled == fadedOut) return;

            fadedOut = disabled;

            var alpha = disabled ? disabledAlpha : 1f;
            var duration = instant ? 0f : fadeDuration;

            foreach (var graphic in fadeGraphics)
            {
                if (graphic != null) graphic.CrossFadeAlpha(alpha, duration, true);
            }
        }

        // A reopened popup can come back with the button already locked.
        void OnEnable() => RefreshDisabledFade(instant: true);

        // Selectable raises nothing when interactable changes.
        void Update() => RefreshDisabledFade(instant: false);

        // Popup bị SetActive(false) chứ không Destroy, mà motion đang chạy thì bị cắt giữa chừng.
        // Trả cỡ gốc ngay ở đây để lần mở sau nút không hiện ra ở cỡ đang nhún.
        void OnDisable()
        {
            motion.TryCancel();
            isPressed = false;
            RestoreTarget();
        }

        void OnDestroy()
        {
            motion.TryCancel();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanPlay()) return;

            isPressed = true;
            motion.TryCancel();

            if (pressStyle == PressStyle.MoveDown)
                motion = PlayOffset(CurrentOffset(), pressedOffset, pressDuration, Ease.OutQuad);
            else if (pressStyle == PressStyle.CoreScale)
                motion = PlayCore(pressAmount, 1f, pressDuration, Ease.OutQuad);
            else
                motion = PlayScale(CurrentScaleRatio(), pressedScale, pressDuration, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Nhả tay ngoài nút vẫn nhận được sự kiện này (EventSystem gửi về object đã bấm),
            // nên nút không bao giờ kẹt ở cỡ nhún.
            if (!isPressed) return;

            isPressed = false;
            motion.TryCancel();

            if (pressStyle == PressStyle.MoveDown)
            {
                motion = PlayOffset(CurrentOffset(), 0f, releaseDuration, Ease.OutBack);
                return;
            }

            // Core Scale nhả tay là về thẳng trạng thái gốc, không nảy: cú nảy hợp với nút nhún cả
            // khối, còn ở đây lõi nút đang mờ nên phóng quá cỡ trông như lỗi hiển thị.
            if (pressStyle == PressStyle.CoreScale)
            {
                motion = PlayCore(pressAmount, 0f, releaseDuration, Ease.OutQuad);
                return;
            }

            var from = CurrentScaleRatio();

            motion = LSequence.Create()
                .Append(LMotion.Create(from, releaseOvershoot, releaseDuration * 0.6f)
                    .WithEase(Ease.OutQuad)
                    .Bind(this, static (value, self) => self.ApplyScale(value)))
                .Append(LMotion.Create(releaseOvershoot, 1f, releaseDuration * 0.4f)
                    .WithEase(Ease.OutQuad)
                    .Bind(this, static (value, self) => self.ApplyScale(value)))
                .Run(builder => builder.WithScheduler(MotionScheduler.UpdateIgnoreTimeScale))
                .AddTo(gameObject);

            // Đặt cỡ xuất phát SAU khi dựng sequence: lúc bind, đoạn thứ hai ghi luôn giá trị đầu
            // của nó (overshoot) vào scale — để nguyên thì nút nháy 1 frame ở cỡ phóng to.
            ApplyScale(from);
        }

        bool CanPlay()
        {
            if (target == null) return false;

            // Nút xám (interactable = false) thì không nhún: phản hồi cho cú bấm không ăn thua
            // trông như nút vẫn dùng được.
            return selectable == null || selectable.IsInteractable();
        }

        MotionHandle PlayScale(float from, float to, float duration, Ease ease)
        {
            var handle = LMotion.Create(from, to, duration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(this, static (value, self) => self.ApplyScale(value))
                .AddTo(gameObject);

            ApplyScale(from);
            return handle;
        }

        // Chạy theo mức nhấn 0..1 thay vì theo cỡ: một motion kéo cả scale lẫn alpha nên hai thứ
        // luôn khớp nhau, và bấm dồn dập thì chạy tiếp từ mức đang hiển thị.
        MotionHandle PlayCore(float from, float to, float duration, Ease ease)
        {
            var handle = LMotion.Create(from, to, duration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(this, static (value, self) => self.ApplyCore(value))
                .AddTo(gameObject);

            ApplyCore(from);
            return handle;
        }

        MotionHandle PlayOffset(float from, float to, float duration, Ease ease)
        {
            if (rectTarget == null) return default;

            var handle = LMotion.Create(from, to, duration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(this, static (value, self) => self.ApplyOffset(value))
                .AddTo(gameObject);

            ApplyOffset(from);
            return handle;
        }

        // Bấm dồn dập thì motion cũ bị cắt giữa chừng — lấy cỡ đang hiển thị làm điểm xuất phát
        // để nút chạy tiếp từ đó thay vì giật về cỡ gốc.
        float CurrentScaleRatio()
        {
            if (Mathf.Approximately(baseScale.x, 0f)) return 1f;
            return target.localScale.x / baseScale.x;
        }

        float CurrentOffset()
        {
            if (rectTarget == null) return 0f;
            return baseAnchoredPosition.y - rectTarget.anchoredPosition.y;
        }

        void ApplyScale(float ratio)
        {
            target.localScale = baseScale * ratio;
        }

        void ApplyOffset(float offset)
        {
            if (rectTarget == null) return;
            rectTarget.anchoredPosition = baseAnchoredPosition + Vector2.down * offset;
        }

        void ApplyCore(float amount)
        {
            pressAmount = amount;
            target.localScale = baseScale * Mathf.Lerp(1f, pressedScale, amount);

            if (coreGroup == null) return;
            coreGroup.alpha = Mathf.Lerp(baseAlpha, pressedAlpha / 255f, amount);
        }

        void RestoreTarget()
        {
            if (target == null) return;

            pressAmount = 0f;
            target.localScale = baseScale;
            if (rectTarget != null) rectTarget.anchoredPosition = baseAnchoredPosition;
            if (coreGroup != null) coreGroup.alpha = baseAlpha;
        }
    }
}
