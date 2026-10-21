#if MB_DEBUG_FPS
using System;
using Cysharp.Threading.Tasks;
using Game.Core.Services;

namespace Game.Game.Services
{
    // Cheat build (MB_DEBUG_FPS): every rewarded button pays out without showing an ad. The reward
    // lands a frame later, as a real ad's would: call sites lock their buttons before Show and
    // expect the callbacks after it returns.
    public sealed class CheatRewardedAdService : IRewardedAdService
    {
        public bool IsReady => true;

        public void Show(string placement, Action onRewarded, Action onClosed = null)
        {
            PayOutAsync(onRewarded, onClosed).Forget();
        }

        static async UniTaskVoid PayOutAsync(Action onRewarded, Action onClosed)
        {
            await UniTask.Yield();
            onRewarded?.Invoke();
            onClosed?.Invoke();
        }
    }
}
#endif
