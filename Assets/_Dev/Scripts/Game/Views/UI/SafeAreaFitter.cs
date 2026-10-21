using UnityEngine;

namespace Game.Game.Views.UI
{
    // Nẹp một RectTransform vào Screen.safeArea để UI né tai thỏ / home bar: đổi rect safe area
    // sang toạ độ chuẩn hoá rồi gán thẳng vào anchorMin/anchorMax.
    //
    // Cách dùng: gắn lên một panel stretch full con trực tiếp của Canvas, mọi UI cần né notch
    // để bên trong panel đó. Background full màn hình để NGOÀI panel — nó phải tràn ra sau
    // tai thỏ thì nhìn mới liền mạch.
    //
    // Vì gán bằng anchor nên panel này không được là đối tượng bị layout khác điều khiển anchor
    // (LayoutGroup cha, tween anchor...) — hai bên sẽ giẫm lên nhau.
    //
    // Refresh chạy trong Update nhưng early-out khi không có gì đổi; phải giữ vậy thay vì chỉ
    // Awake vì một số máy Samsung báo safe area trễ vài frame sau khi mở app / xoay màn hình.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool conformX = true;     // Nẹp theo trục ngang (tắt nếu panel muốn tràn hai mép)
        [SerializeField] private bool conformY = true;     // Nẹp theo trục dọc
        [SerializeField] private bool forceBottomZero;     // Bỏ nẹp đáy — UI sát đáy muốn đè lên vùng home bar

        private RectTransform panel;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            panel = (RectTransform)transform;
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            Rect safeArea = Screen.safeArea;

            if (safeArea == lastSafeArea
                && Screen.width == lastScreenSize.x
                && Screen.height == lastScreenSize.y
                && Screen.orientation == lastOrientation)
            {
                return;
            }

            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;

            Apply(safeArea);
        }

        private void Apply(Rect area)
        {
            lastSafeArea = area;

            if (!conformX)
            {
                area.x = 0f;
                area.width = Screen.width;
            }

            if (!conformY)
            {
                area.y = 0f;
                area.height = Screen.height;
            }

            // Một số máy Samsung trả kích thước 0 ở frame đầu — chia ra sẽ thành NaN.
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Vector2 anchorMin = area.position;
            Vector2 anchorMax = area.position + area.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            if (forceBottomZero)
            {
                anchorMin.y = 0f;
            }

            // Samsung (Note 10+, A71, S20...) có thể trả toạ độ âm/NaN ở lần gọi đầu; frame sau
            // sẽ có giá trị đúng nên bỏ qua lần này thay vì gán bậy.
            if (anchorMin.x < 0f || anchorMin.y < 0f || anchorMax.x < 0f || anchorMax.y < 0f)
            {
                return;
            }

            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;
        }
    }
}
