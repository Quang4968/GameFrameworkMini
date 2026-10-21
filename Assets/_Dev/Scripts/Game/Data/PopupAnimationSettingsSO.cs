using UnityEngine;

namespace Game.Game.Data
{
    // Thông số animation dùng CHUNG cho mọi popup — chỉnh ở đây là cả game đổi theo,
    // không popup nào được tự khai nhịp riêng.
    [CreateAssetMenu(menuName = "Game/Popup Animation Settings", fileName = "PopupAnimationSettings")]
    public sealed class PopupAnimationSettingsSO : ScriptableObject
    {
        [Tooltip("Số giây của animation phóng to lúc popup hiện ra.")]
        [SerializeField] float appearDuration = 0.4f;

        [Tooltip("Tỉ lệ scale lúc popup bắt đầu hiện ra.")]
        [SerializeField, Range(0.1f, 1f)] float appearScaleStart = 0.7f;

        [Tooltip("Cỡ phóng vọt qua 1 trong khoảnh khắc trước khi lùi về cỡ thật.")]
        [SerializeField, Range(1f, 1.2f)] float appearScaleOvershoot = 1.08f;

        [Tooltip("Số giây của animation thu nhỏ + mờ dần lúc popup đóng.")]
        [SerializeField] float disappearDuration = 0.18f;

        [Tooltip("Tỉ lệ scale lúc popup biến mất hẳn.")]
        [SerializeField, Range(0.1f, 1f)] float disappearScaleEnd = 0.85f;

        public float AppearDuration => appearDuration;
        public float AppearScaleStart => appearScaleStart;
        public float AppearScaleOvershoot => appearScaleOvershoot;
        public float DisappearDuration => disappearDuration;
        public float DisappearScaleEnd => disappearScaleEnd;
    }
}
