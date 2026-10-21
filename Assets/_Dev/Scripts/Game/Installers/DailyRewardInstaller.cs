using Game.Core.Services;
using Game.Game.Data;
using Game.Game.Services;
using VContainer;

namespace Game.Game.Installers
{
    // The daily reward in one call. TGranter is the only game-specific piece: it pays out the item ids
    // of the remote config cycle. Needs IClockService (LifeSystemInstaller) and IRemoteConfigService
    // (SdkSystemInstaller) registered in the same scope.
    public static class DailyRewardInstaller
    {
        public static void RegisterDailyReward<TGranter>(this IContainerBuilder builder, DailyRewardSettingsSO settings)
            where TGranter : class, IDailyRewardGranter
        {
            builder.Register<IDailyRewardService, PlayerPrefsDailyRewardService>(Lifetime.Singleton);
            builder.Register<IDailyRewardSchedule, RemoteDailyRewardSchedule>(Lifetime.Singleton);
            builder.Register<IDailyRewardGranter, TGranter>(Lifetime.Singleton);

            // The popup draws from it; without it the scope must not register DailyRewardAutoOpen.
            if (settings != null) builder.RegisterInstance(settings);
        }
    }
}
