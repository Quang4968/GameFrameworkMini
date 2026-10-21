using Game.Game.Data;
using Game.Game.Services;
using VContainer;

namespace Game.Game.Installers
{
    // Object pool plus the coin burst built on it, for every scene that pays out coins (Home shop,
    // MainGame win and shop). Coin counters and the shop live in both, so both must register this.
    public static class ObjectPoolInstaller
    {
        public static void RegisterObjectPool(this IContainerBuilder builder, PoolCatalogSO catalog)
        {
            // The service builds its own Pool Boss host and destroys it with the scope.
            builder.Register<IObjectPoolService>(_ => new PoolBossObjectPoolService(catalog), Lifetime.Singleton);

            // Built eagerly: a lazy pool would preload on the first spawn, mid-play.
            builder.RegisterBuildCallback(container => container.Resolve<IObjectPoolService>());
        }

        // Call after RegisterObjectPool and the IAudioService registration. A null settings asset
        // still registers the service, which then plays nothing.
        public static void RegisterCoinFlight(this IContainerBuilder builder, CoinFlightSettingsSO settings)
        {
            builder.Register<I_CoinFlightService>(resolver => new CoinFlightService(
                    settings,
                    resolver.Resolve<IObjectPoolService>(),
                    resolver.Resolve<Core.Services.IAudioService>()),
                Lifetime.Singleton);
        }
    }
}
