using Game.Game.Views.Transition;
using UnityEngine;

namespace Game.Game.Data
{
    // Thông số của transition đổi scene, dùng CHUNG cho mọi lần đổi scene — chỉnh ở đây là
    // cả game đổi theo, không màn nào được tự khai nhịp riêng.
    [CreateAssetMenu(menuName = "Game/Scene Transition Settings", fileName = "SceneTransitionSettings")]
    public sealed class SceneTransitionSettingsSO : ScriptableObject
    {
        [Tooltip("Prefab màn loading. Chỉ Instantiate 1 lần rồi giữ xuyên scene, thay art là " +
            "thay trong prefab này chứ không đụng code.")]
        [SerializeField] LoadingTransitionView loadingPrefab;

        [Tooltip("Số giây màn loading hiện dần lên che scene cũ.")]
        [SerializeField] float fadeInDuration = 0.35f;

        [Tooltip("Số giây màn loading nhạt dần đi sau khi scene mới đã load xong.")]
        [SerializeField] float fadeOutDuration = 0.4f;

        [Tooltip("Màn loading phải ở trên màn hình ít nhất bấy nhiêu giây, tính từ lúc nó bắt đầu hiện. " +
            "Scene load xong sớm hơn thì đợi cho đủ rồi mới mở ra — không thì icon còn chưa phóng xong " +
            "đã sang scene mới, nhìn loé một cái. Đặt >= thời gian icon phóng; bằng một vòng dấu chấm " +
            "thì trọn vẹn nhất. Load lâu hơn số này thì không cộng thêm gì.")]
        [SerializeField] float minVisibleSeconds = 1.2f;

        [Tooltip("Giữ màn loading thêm bấy nhiêu giây sau khi scene mới load xong, cho scene mới " +
            "kịp dựng xong trước khi lộ ra.")]
        [SerializeField] float holdSeconds = 0.15f;

        [Tooltip("Số FRAME giữ màn loading thêm sau khi scene mới load xong, tính trước HoldSeconds. " +
            "Frame kích hoạt scene và frame ngay sau nó là hai frame nặng nhất của cả nhịp đổi scene; " +
            "đợi chúng đi qua rồi mới mở ra thì người chơi không thấy khựng.")]
        [SerializeField, Range(0, 10)] int holdFrames = 3;

        public LoadingTransitionView LoadingPrefab => loadingPrefab;
        public float FadeInDuration => fadeInDuration;
        public float FadeOutDuration => fadeOutDuration;
        public float MinVisibleSeconds => minVisibleSeconds;
        public float HoldSeconds => holdSeconds;
        public int HoldFrames => holdFrames;
    }
}
