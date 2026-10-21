using Game.Core.Services;
using Game.Game.Services;
using VContainer.Unity;

namespace Game.Game.Boot
{
    // Entry point of the MainGame scene. The framework has no board: a game replaces this class
    // (or adds its own IStartable) to load levelProgress.LevelLoad into its views and to wire the
    // GameplayBridge delegates before the first outcome can be reported.
    public sealed class MainGameBootstrap : IStartable
    {
        readonly ILevelProgressService levelProgress;
        readonly GameplayBridge bridge;

        public MainGameBootstrap(ILevelProgressService levelProgress, GameplayBridge bridge)
        {
            this.levelProgress = levelProgress;
            this.bridge = bridge;
        }

        public void Start()
        {
            // Nothing to load yet; the level index is levelProgress.LevelLoad and outcomes go
            // through bridge.ReportWon() / bridge.ReportLost().
        }
    }
}
