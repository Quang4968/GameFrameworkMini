using Game.Game.Data;
using Game.Game.Views.Transition;
using UnityEngine;

namespace Game.Game.SceneFlow
{
    // Màn loading phải sống lâu hơn scene đã tạo ra nó, nên instance được giữ ở static và
    // DontDestroyOnLoad: mỗi scene có LifetimeScope riêng, container không giữ hộ qua scene được.
    //
    // Instance dựng lười ở lần đổi scene đầu tiên, nên vào thẳng PlayMode từ scene nào cũng có
    // transition, không cần kéo sẵn object vào từng scene.
    public static class SceneTransitionProvider
    {
        static LoadingTransitionView instance;

        // True while the loading screen hides the scene; anything that pops up on scene entry waits it out.
        public static bool IsCovering => instance != null && instance.IsClosed;

        public static ISceneTransition GetOrCreate(SceneTransitionSettingsSO settings)
        {
            // So sánh null kiểu UnityEngine.Object: bắt được cả trường hợp tắt Domain Reload,
            // instance cũ đã bị huỷ nhưng static vẫn còn trỏ tới.
            if (instance != null) return instance;

            if (settings == null || settings.LoadingPrefab == null)
            {
                Debug.LogWarning("[SceneTransition] Chưa gán SceneTransitionSettings hoặc prefab màn loading " +
                    "-> đổi scene sẽ chạy thẳng, không có transition.");
                return null;
            }

            instance = Object.Instantiate(settings.LoadingPrefab);
            instance.name = settings.LoadingPrefab.name;
            Object.DontDestroyOnLoad(instance.gameObject);
            instance.Bind(settings);
            return instance;
        }
    }
}
