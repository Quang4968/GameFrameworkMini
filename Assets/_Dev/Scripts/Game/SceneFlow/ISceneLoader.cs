using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Game.SceneFlow
{
    public interface ISceneLoader
    {
        UniTask LoadSceneAsync(string sceneName, CancellationToken ct = default);
    }
}
