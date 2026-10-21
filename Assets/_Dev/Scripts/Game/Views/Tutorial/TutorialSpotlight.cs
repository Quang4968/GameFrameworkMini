using UnityEngine;
using UnityEngine.UI;

namespace Game.Game.Views.Tutorial
{
    // A full-screen dim with ONE hole, built from four panels rather than a shader cutout. The
    // panels also do the input gating, which is the whole point: BoardInputController already drops
    // any tap landing on UI, so the hole lets exactly one tap through — no flag in the input code.
    public sealed class TutorialSpotlight : MonoBehaviour
    {
        [Tooltip("Bốn mảnh quây quanh lỗ: trái, phải, trên, dưới. Cả bốn PHẢI bật Raycast Target, " +
            "nếu không lớp phủ không chặn được cú chạm nào.")]
        [SerializeField] RectTransform left;
        [SerializeField] RectTransform right;
        [SerializeField] RectTransform top;
        [SerializeField] RectTransform bottom;

        [Tooltip("Nới rộng lỗ ra mỗi phía, tính bằng pixel màn hình — để viền nút không bị mảnh " +
            "phủ liếm vào.")]
        [SerializeField] float holePadding = 12f;

        [Tooltip("Bốn miếng bo góc, theo thứ tự: dưới-trái, dưới-phải, trên-trái, trên-phải. Dùng " +
            "sprite SpotlightCorner, và cả bốn cùng một hướng — code lật bằng scale. Bỏ trống thì " +
            "lỗ vẫn chạy, chỉ là góc vuông.")]
        [SerializeField] RectTransform[] corners;

        [Tooltip("Bán kính bo góc của lỗ, tính bằng pixel màn hình. Tự thu lại khi lỗ quá nhỏ.")]
        [SerializeField] float cornerRadius = 34f;

        RectTransform self;
        Canvas canvas;

        // Resolved on demand, NOT in Awake: disabled in the prefab, so Awake does not run until
        // OpenAt shows it — and OpenAt needs these first. Caching in Awake left it invisible.
        RectTransform Self => self != null ? self : self = (RectTransform)transform;

        Canvas ParentCanvas => canvas != null ? canvas : canvas = GetComponentInParent<Canvas>(true);

        // Reapplied every frame: the target may be a piece in motion, and a stale hole
        // would block the very tap it exists to allow.
        public void OpenAt(Rect screenRect)
        {
            if (!TutorialScreenRect.TryToLocalRect(screenRect, Self, ParentCanvas, out var hole)) return;

            gameObject.SetActive(true);

            hole = Rect.MinMaxRect(hole.xMin - holePadding, hole.yMin - holePadding,
                hole.xMax + holePadding, hole.yMax + holePadding);

            var bounds = Self.rect;

            SetLocalRect(left, bounds.xMin, bounds.yMin, hole.xMin, bounds.yMax);
            SetLocalRect(right, hole.xMax, bounds.yMin, bounds.xMax, bounds.yMax);
            SetLocalRect(top, hole.xMin, hole.yMax, hole.xMax, bounds.yMax);
            SetLocalRect(bottom, hole.xMin, bounds.yMin, hole.xMax, hole.yMin);

            RoundCorners(hole);
        }

        // Bốn mảnh trên cắt ra một lỗ vuông; bốn miếng này phủ lại đúng phần góc nằm ngoài cung
        // tròn, nên lỗ thành hình chữ nhật bo góc. Chúng nằm TRONG lỗ nên không đè lên mảnh nào.
        void RoundCorners(Rect hole)
        {
            if (corners == null || corners.Length < 4) return;

            // Lỗ nhỏ hơn hai lần bán kính thì bốn góc chồng lên nhau và bịt kín luôn cái lỗ.
            var radius = Mathf.Min(cornerRadius, hole.width * 0.5f, hole.height * 0.5f);
            var half = radius * 0.5f;

            PlaceCorner(0, hole.xMin + half, hole.yMin + half, radius, 1f, 1f);
            PlaceCorner(1, hole.xMax - half, hole.yMin + half, radius, -1f, 1f);
            PlaceCorner(2, hole.xMin + half, hole.yMax - half, radius, 1f, -1f);
            PlaceCorner(3, hole.xMax - half, hole.yMax - half, radius, -1f, -1f);
        }

        // Sprite vẽ sẵn cho góc dưới-trái; ba góc kia là chính nó lật lại bằng scale âm.
        void PlaceCorner(int index, float centerX, float centerY, float radius, float flipX, float flipY)
        {
            var corner = corners[index];
            if (corner == null) return;

            corner.gameObject.SetActive(radius > 0f);

            corner.anchorMin = new Vector2(0.5f, 0.5f);
            corner.anchorMax = new Vector2(0.5f, 0.5f);
            corner.pivot = new Vector2(0.5f, 0.5f);
            corner.sizeDelta = new Vector2(radius, radius);
            corner.anchoredPosition = new Vector2(centerX, centerY);
            corner.localScale = new Vector3(flipX, flipY, 1f);
        }

        void HideCorners()
        {
            if (corners == null) return;

            foreach (var corner in corners)
                if (corner != null) corner.gameObject.SetActive(false);
        }

        // No target: cover the whole screen rather than leave a hole where it last was.
        public void CloseHole()
        {
            gameObject.SetActive(true);

            var bounds = Self.rect;
            SetLocalRect(left, bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax);
            SetLocalRect(right, 0f, 0f, 0f, 0f);
            SetLocalRect(top, 0f, 0f, 0f, 0f);
            SetLocalRect(bottom, 0f, 0f, 0f, 0f);

            HideCorners();
        }

        public void Close() => gameObject.SetActive(false);

        static void SetLocalRect(RectTransform panel, float xMin, float yMin, float xMax, float yMax)
        {
            if (panel == null) return;

            // A hole past a screen edge inverts a panel; a negative size paints the wrong half.
            var width = Mathf.Max(0f, xMax - xMin);
            var height = Mathf.Max(0f, yMax - yMin);

            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(width, height);
            panel.anchoredPosition = new Vector2(xMin + width * 0.5f, yMin + height * 0.5f);
        }

        // The four panels are meaningless without a raycast target.
        void OnValidate()
        {
            foreach (var panel in new[] { left, right, top, bottom })
            {
                if (panel == null) continue;

                var image = panel.GetComponent<Image>();
                if (image != null && !image.raycastTarget) image.raycastTarget = true;
            }
        }
    }
}
