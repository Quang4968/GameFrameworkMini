using Game.Core.Services;
using Game.Game.Data;
using Game.Game.Services;
using VContainer;

namespace Game.Game.Installers
{
    // Every scope registers the same static service, so the language never resets on scene change.
    public static class LocalizationInstaller
    {
        public static void RegisterLocalization(this IContainerBuilder builder, LocalizationSettingsSO settings)
        {
            builder.Register<ILocalizationService>(_ => LocalizationProvider.GetOrCreate(settings), Lifetime.Singleton);

            // Built with the scope rather than on first resolve, so scene labels bind at once.
            builder.RegisterBuildCallback(resolver => resolver.Resolve<ILocalizationService>());
        }
    }
}
