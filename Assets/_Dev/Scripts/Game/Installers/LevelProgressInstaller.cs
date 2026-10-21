using Game.Core.Services;
using Game.Game.Services;
using VContainer;

namespace Game.Game.Installers
{
    // Level progress registration shared by every scene. Home and MainGame must agree on the
    // PlayerPrefs key and the level count that wraps the index, or the number on the Play button
    // drifts from the level MainGame loads.
    public static class LevelProgressInstaller
    {
        // A null catalog means levelCount = 0: the service wraps every index to 0, so the game
        // shows "Level 1" instead of throwing.
        public static void RegisterLevelProgress(this IContainerBuilder builder, ILevelCatalog catalog)
        {
            if (catalog != null) builder.RegisterInstance(catalog);

            builder.Register<ILevelProgressService>(
                resolver =>
                {
                    var levelCount = catalog != null ? catalog.LevelCount : 0;
                    var config = resolver.Resolve<GameRemoteConfig>();
                    var order = config.LevelOrder(levelCount);
                    return new PlayerPrefsLevelProgressService(
                        order, config.LoopStartIndex(order.Length), config.LoopEndIndex(order.Length));
                },
                Lifetime.Singleton);
        }
    }
}
