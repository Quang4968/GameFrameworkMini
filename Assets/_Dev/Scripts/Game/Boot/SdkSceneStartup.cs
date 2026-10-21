using System.Threading;
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk;
using Game.Core.Services;
using VContainer.Unity;

namespace Game.Game.Boot
{
    // Initializes the SDK for a scene entered without passing through Boot - which is every Editor session that hits
    // Play straight into Home or MainGame. Without it no ad network ever initializes, so IAdService.IsReady stays false
    // for the whole session and the rewarded buttons never light up.
    //
    // Registered by SdkSystemInstaller, so every scene that has the SDK also has this. In the Boot scene it is
    // dispatched BEFORE BootstrapRunner (registration order), so it is the one that starts the init there and
    // InitializeSdkBootStep then awaits that same init - see SdkRuntime.InitializeAsync.
    public sealed class SdkSceneStartup : IStartable
    {
        readonly ISdkRuntime sdk;
        readonly IPlayerLevel playerLevel;
        readonly ILevelProgressService levelProgress;

        public SdkSceneStartup(ISdkRuntime sdk, IPlayerLevel playerLevel, ILevelProgressService levelProgress)
        {
            this.sdk = sdk;
            this.playerLevel = playerLevel;
            this.levelProgress = levelProgress;
        }

        public void Start()
        {
            if (sdk.IsReady) return;

            // The same function InitializeSdkBootStep hands over. Set here too because the order between this and the
            // Boot scene's own step is not fixed, and whichever initializes first must not leave the level unknown.
            playerLevel.SetProvider(() => levelProgress.LevelShow + 1);

            InitializeAsync().Forget();
        }

        // The SDK lives across scenes, so its init must not be cancelled by this scope going away.
        async UniTaskVoid InitializeAsync() => await sdk.InitializeAsync(CancellationToken.None);
    }
}
