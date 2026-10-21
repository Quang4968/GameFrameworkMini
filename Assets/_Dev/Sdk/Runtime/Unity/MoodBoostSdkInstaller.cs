using MoodBoost.Sdk.Ads;
using MoodBoost.Sdk.Tracking;
using VContainer;

namespace MoodBoost.Sdk.Unity
{
    // Registers the SDK's facades in a scene's container. The SDK itself is built once (MoodBoostSdk); every scope
    // only re-registers references to the same instances.
    public static class MoodBoostSdkInstaller
    {
        public static void RegisterMoodBoostSdk(this IContainerBuilder builder, SdkSettings settings)
        {
            var sdk = MoodBoostSdk.GetOrCreate(settings);

            builder.RegisterInstance(sdk.Runtime);
            builder.RegisterInstance(sdk.Ads);
            builder.RegisterInstance(sdk.AdEvents);
            builder.RegisterInstance(sdk.Tracking);
            builder.RegisterInstance(sdk.Attribution);
            builder.RegisterInstance(sdk.RemoteConfig);
            builder.RegisterInstance(sdk.AppLifecycle);
            builder.RegisterInstance(sdk.PlayerLevel);
            builder.RegisterInstance(sdk.LevelTracking);
            builder.RegisterInstance(sdk.Consent);
            builder.RegisterInstance(sdk.PrivacyOptions);
            builder.RegisterInstance(sdk.Haptics);
        }
    }
}
