using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Game.Animation
{
    // Vệt sáng thi thoảng lướt chéo qua nút — gắn component vào object có nền nút là xong,
    // không cần dựng thêm object con hay chuẩn bị sprite: cả khuôn cắt lẫn vệt sáng đều dựng
    // lúc Awake.
    //
    // Khuôn cắt lấy CHÍNH sprite của nền nút làm mask, nên nút bo góc hay hình gì thì vệt cũng
    // dừng đúng viền. Mask nằm trên object con của riêng component, không đụng vào cây con sẵn
    // có của nút — thêm Mask thẳng lên nút sẽ cắt luôn cả chữ và icon.
    //
    // Nhịp nghỉ random trong một đoạn để nhiều nút trên cùng màn không loé thành một hàng.
    [RequireComponent(typeof(RectTransform))]
    public sealed class ButtonShine : MonoBehaviour
    {
        [Tooltip("Ảnh làm khuôn cắt — thường là nền nút. Bỏ trống thì lấy Image trên chính object " +
            "này; không có Image nào thì cắt theo khung chữ nhật.")]
        [SerializeField] Image maskSource;

        [Tooltip("Màu vệt sáng, alpha quyết định độ chói.")]
        [SerializeField] Color color = new Color(1f, 1f, 1f, 0.45f);

        [Tooltip("Bề rộng vệt, tính theo bề rộng nút.")]
        [SerializeField, Range(0.05f, 1f)] float widthRatio = 0.18f;

        [Tooltip("Độ nghiêng của vệt, tính bằng độ.")]
        [SerializeField, Range(-60f, 60f)] float angle = 20f;

        [Tooltip("Số giây một lần lướt.")]
        [SerializeField, Range(0.1f, 2f)] float sweepDuration = 0.79f;

        [Tooltip("Khoảng nghỉ giữa hai lần lướt, random trong đoạn min–max (giây).")]
        [SerializeField] Vector2 restRange = new Vector2(2.5f, 4.5f);

        [Tooltip("Nút đang khoá (interactable = false) thì không loé.")]
        [SerializeField] bool skipWhenDisabled = true;

        static Sprite sharedGradient;

        RectTransform rect;
        RectTransform streak;
        Image streakImage;
        Selectable selectable;
        MotionHandle motion;
        Vector2 lastSize;
        float travel;

        void Awake()
        {
            rect = (RectTransform)transform;
            selectable = GetComponent<Selectable>();
            if (maskSource == null) maskSource = GetComponent<Image>();

            Build();
        }

        void OnEnable() => Schedule();

        void OnDisable()
        {
            motion.TryCancel();
            Park();
        }

        void Build()
        {
            var clip = new GameObject("Shine", typeof(RectTransform));
            var clipRect = (RectTransform)clip.transform;
            clipRect.SetParent(rect, false);
            clipRect.anchorMin = Vector2.zero;
            clipRect.anchorMax = Vector2.one;
            clipRect.offsetMin = Vector2.zero;
            clipRect.offsetMax = Vector2.zero;

            // Không có sprite để làm khuôn thì đành cắt theo khung chữ nhật: vệt sẽ chạm tới góc
            // nút thay vì dừng ở viền cong.
            if (maskSource != null && maskSource.sprite != null)
            {
                var maskImage = clip.AddComponent<Image>();
                maskImage.sprite = maskSource.sprite;
                maskImage.type = maskSource.type;
                maskImage.pixelsPerUnitMultiplier = maskSource.pixelsPerUnitMultiplier;
                maskImage.raycastTarget = false;
                clip.AddComponent<Mask>().showMaskGraphic = false;
            }
            else
            {
                clip.AddComponent<RectMask2D>();
            }

            var streakObject = new GameObject("Streak", typeof(RectTransform));
            streak = (RectTransform)streakObject.transform;
            streak.SetParent(clipRect, false);
            streak.anchorMin = streak.anchorMax = streak.pivot = new Vector2(0.5f, 0.5f);
            streak.localRotation = Quaternion.Euler(0f, 0f, angle);

            streakImage = streakObject.AddComponent<Image>();
            streakImage.sprite = Gradient();
            streakImage.color = color;
            streakImage.raycastTarget = false;

            Park();
        }

        void Schedule()
        {
            motion.TryCancel();
            Park();

            var rest = Random.Range(Mathf.Min(restRange.x, restRange.y), Mathf.Max(restRange.x, restRange.y));

            // Vệt chỉ hiện trong lúc Bind chạy: LitMotion không ghi giá trị suốt quãng delay nên
            // nút đứng yên hoàn toàn giữa hai lần loé.
            motion = LMotion.Create(0f, 1f, sweepDuration)
                .WithDelay(rest)
                .WithEase(Ease.InOutSine)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(Schedule)
                .Bind(this, static (t, self) => self.ApplySweep(t))
                .AddTo(gameObject);
        }

        void ApplySweep(float t)
        {
            if (skipWhenDisabled && selectable != null && !selectable.interactable)
            {
                Park();
                return;
            }

            Resize();
            streakImage.enabled = true;
            streak.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-travel, travel, t), 0f);
        }

        // Đo lại theo cỡ thật của nút: lúc Awake layout thường chưa chạy, và nút trong layout group
        // còn đổi cỡ sau đó.
        void Resize()
        {
            var size = rect.rect.size;
            if (size == lastSize) return;

            lastSize = size;

            var width = Mathf.Max(2f, size.x * widthRatio);

            // Dài bằng đường chéo nút thì nghiêng góc nào cũng phủ hết chiều cao.
            streak.sizeDelta = new Vector2(width, size.magnitude);
            travel = size.x * 0.5f + width;
        }

        void Park() => streakImage.enabled = false;

        // Dải trắng mờ dần về hai mép. Một texture 64x1 dùng chung cho mọi nút, khỏi phải cắt
        // thêm sprite bên art chỉ để có một vệt gradient.
        static Sprite Gradient()
        {
            if (sharedGradient != null) return sharedGradient;

            const int width = 64;
            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[width];
            for (var i = 0; i < width; i++)
            {
                var falloff = Mathf.Sin((i + 0.5f) / width * Mathf.PI);
                pixels[i] = new Color(1f, 1f, 1f, falloff * falloff);
            }

            texture.SetPixels(pixels);
            texture.Apply();

            sharedGradient = Sprite.Create(texture, new Rect(0f, 0f, width, 1f), new Vector2(0.5f, 0.5f));
            sharedGradient.hideFlags = HideFlags.HideAndDontSave;
            return sharedGradient;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (streak == null) return;

            streak.localRotation = Quaternion.Euler(0f, 0f, angle);
            streakImage.color = color;
            lastSize = Vector2.zero;
        }
#endif
    }
}
