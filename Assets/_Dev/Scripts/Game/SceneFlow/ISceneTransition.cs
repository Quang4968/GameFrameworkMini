using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Game.SceneFlow
{
    // Lớp che phủ giữa hai scene. Close chạy trước khi đổi scene (che lại), Open chạy sau khi scene
    // mới đã load xong (mở ra) — mọi kiểu transition (màn loading, fade, wipe...) chỉ cần implement
    // hai nhịp này.
    public interface ISceneTransition
    {
        UniTask CloseAsync(CancellationToken ct = default);
        UniTask OpenAsync(CancellationToken ct = default);
    }
}
