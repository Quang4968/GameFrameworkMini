using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Game.Views.Tutorial
{
    // The gift flying into the booster bar, then the button popping once as it unlocks. One
    // instance in the HUD prefab, shown and hidden — not an effect spawned per use, so it needs no
    // pool entry. Owns NO completion callback on purpose: the step counts TotalDuration itself, so
    // tearing this down mid-flight cannot shove the lesson forward while the scene is destroyed.
    public sealed class TutorialRewardFlight : MonoBehaviour
    {
        [Tooltip("Ảnh bay. Sprite được đặt lúc chạy, lấy từ chính icon của booster trên thanh.")]
        [SerializeField] Image icon;

        [Tooltip("Chỗ ảnh xuất phát, tính từ tâm container — nơi tấm quà vừa biến mất.")]
        [SerializeField] Vector2 startPosition = new(0f, 120f);

        [SerializeField] float flyDuration = 0.55f;
        [SerializeField] float startScale = 1.8f;
        [SerializeField] float endScale = 0.7f;

        [Tooltip("Cú nảy của nút booster lúc ảnh đáp xuống — dấu hiệu 'vừa mở khoá'.")]
        [SerializeField] float punchScale = 1.35f;
        [SerializeField] float punchDuration = 0.3f;

        RectTransform self;
        Canvas canvas;
        RectTransform iconRect;
        RectTransform punchTarget;

        MotionHandle moveMotion;
        MotionHandle scaleMotion;
        MotionHandle punchMotion;

        // Flight plus punch. The step that called Play counts this to know when it is done.
        public float TotalDuration => flyDuration + punchDuration;

        // The moment the icon reaches the bar, before the punch. The step drops the padlock here.
        public float FlyDuration => flyDuration;

        // Resolved on demand: disabled in the prefab, so Awake does not run until shown.
        RectTransform Self => self != null ? self : self = (RectTransform)transform;

        Canvas ParentCanvas => canvas != null ? canvas : canvas = GetComponentInParent<Canvas>(true);

        RectTransform IconRect =>
            iconRect != null ? iconRect : iconRect = icon != null ? (RectTransform)icon.transform : null;

        public void Play(Sprite sprite, Rect targetScreenRect, RectTransform target)
        {
            if (icon == null || sprite == null
                || !TutorialScreenRect.TryToLocalRect(targetScreenRect, Self, ParentCanvas, out var local))
            {
                return;
            }

            punchTarget = target;

            gameObject.SetActive(true);
            icon.sprite = sprite;
            icon.enabled = true;

            IconRect.anchoredPosition = startPosition;
            IconRect.localScale = Vector3.one * startScale;

            moveMotion.TryCancel();
            scaleMotion.TryCancel();

            // Unscaled: the gift flies off the unlock popup, which has stopped the level's clock.
            moveMotion = LMotion.Create(startPosition, local.center, flyDuration)
                .WithEase(Ease.InOutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(Land)
                .Bind(this, static (value, self) => self.SetIconPosition(value))
                .AddTo(gameObject);

            scaleMotion = LMotion.Create(startScale, endScale, flyDuration)
                .WithEase(Ease.InCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(this, static (value, self) => self.SetIconScale(value))
                .AddTo(gameObject);
        }

        void OnDisable()
        {
            moveMotion.TryCancel();
            scaleMotion.TryCancel();
            punchMotion.TryCancel();

            // Reset the button even if the punch was cut short, or it stays enlarged.
            if (punchTarget != null) punchTarget.localScale = Vector3.one;
        }

        void Land()
        {
            icon.enabled = false;

            if (punchTarget == null)
            {
                gameObject.SetActive(false);
                return;
            }

            punchMotion.TryCancel();
            punchMotion = LMotion.Create(0f, 1f, punchDuration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() => gameObject.SetActive(false))
                .Bind(this, static (value, self) => self.SetPunch(value))
                .AddTo(gameObject);
        }

        void SetIconPosition(Vector2 value) => IconRect.anchoredPosition = value;

        void SetIconScale(float value) => IconRect.localScale = Vector3.one * value;

        // One rise and fall over the whole motion, so the button ends exactly where it started.
        void SetPunch(float t)
        {
            if (punchTarget == null) return;

            var amount = 1f + Mathf.Sin(t * Mathf.PI) * (punchScale - 1f);
            punchTarget.localScale = Vector3.one * amount;
        }
    }
}
