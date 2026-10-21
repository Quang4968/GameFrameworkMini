using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Game.Game.SceneFlow
{
    public sealed class SceneLoader : ISceneLoader
    {
        public async UniTask LoadSceneAsync(string sceneName, CancellationToken ct = default)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            await operation.ToUniTask(cancellationToken: ct);
        }
    }
}
