using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Game.Views.Popup
{
    // Scaffolding để test hệ popup bằng tay: đổi popupId ngay trên Inspector rồi bấm
    // nút lúc Play. Xoá cả file này, PopupTest.prefab và PopupId.Test khi đã có popup thật.
    public sealed class PopupTestTrigger : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] PopupId popupId = PopupId.Test;
        [SerializeField] bool hasOverlay = true;

        IPopupService popupService;

        // Method injection thay vì constructor: đây là MonoBehaviour có sẵn trong scene, được
        // LifetimeScope đăng ký bằng RegisterComponent nên VContainer gọi hàm này lúc build.
        [Inject]
        public void Construct(IPopupService popupService)
        {
            this.popupService = popupService;
        }

        void Awake()
        {
            button.onClick.AddListener(OpenPopup);
        }

        void OnDestroy()
        {
            button.onClick.RemoveListener(OpenPopup);
        }

        void OpenPopup()
        {
            if (popupService == null)
            {
                Debug.LogError("[PopupTestTrigger] Chưa được inject IPopupService — kiểm tra field " +
                    "Popup Test Trigger trên LifetimeScope của scene.", this);
                return;
            }

            // Lần đầu bấm sẽ nạp prefab nên popup hiện trễ vài frame; đây là nút test nên không
            // chờ kết quả, chỉ huỷ theo vòng đời của chính object này.
            popupService.OpenAsync(popupId, hasOverlay, ct: this.GetCancellationTokenOnDestroy()).Forget();
        }
    }
}
