using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Game.Views.Gameplay
{
    // Tự dựng HUD gameplay (prefab PopupGameplay — các vùng anchor của GameplayScreenAligner) vào
    // canvas lúc scene khởi động. Nhờ dựng qua resolver.Instantiate mà aligner trong prefab nhận
    // được [Inject] để tự lấy các Target 3D — điều mà prefab đặt tay không làm được vì ref tới object
    // trong scene không serialize được vào asset.
    //
    // Hàng nút booster (BoosterBar + BoosterPrompt) nằm trong chính prefab này, nên nó cũng được
    // rót qua cú Instantiate ở đây thay vì qua autoInjectGameObjects của MainGameLifetimeScope.
    //
    // Đặt làm con đầu (SetAsFirstSibling) của container để mọi popup thật (Win/Lose/Settings) dựng
    // sau đó vẫn nằm trên HUD. PopupOverlay cũng nằm trong container, ngay sau HUD: nhờ vậy lớp mờ
    // vẫn phủ lên hàng nút booster đúng như hồi nó còn nằm rời dưới MainGameCanvas.
    public sealed class GameplayHudPresenter : IStartable
    {
        readonly IObjectResolver resolver;
        readonly GameObject hudPrefab;
        readonly Transform container;

        public GameplayHudPresenter(IObjectResolver resolver, GameObject hudPrefab, Transform container)
        {
            this.resolver = resolver;
            this.hudPrefab = hudPrefab;
            this.container = container;
        }

        public void Start()
        {
            if (hudPrefab == null)
            {
                Debug.LogWarning($"[{nameof(GameplayHudPresenter)}] Chưa gán HUD prefab — bỏ qua việc dựng HUD.");
                return;
            }

            var parent = container != null ? container : ResolveFallbackParent();
            if (parent == null)
            {
                Debug.LogError($"[{nameof(GameplayHudPresenter)}] Không tìm được canvas để đặt HUD.");
                return;
            }

            var hud = resolver.Instantiate(hudPrefab, parent);
            hud.transform.SetAsFirstSibling();
        }

        static Transform ResolveFallbackParent()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            return canvas != null ? canvas.transform : null;
        }
    }
}
