#if MB_DEBUG_FPS
using System;
using Cysharp.Threading.Tasks;
using Game.Core.Services;

namespace Game.Game.Services
{
    // Cheat build (MB_DEBUG_FPS): no interstitial ever shows; onClosed runs a frame later, the
    // same path a capped or missing ad takes.
    public sealed class CheatInterstitialAdService : I_InterstitialAdService
    {
        public bool IsReady => false;

        public void Show(Action onClosed)
        {
            CloseAsync(onClosed).Forget();
        }

        static async UniTaskVoid CloseAsync(Action onClosed)
        {
            await UniTask.Yield();
            onClosed?.Invoke();
        }
    }
}
#endif
