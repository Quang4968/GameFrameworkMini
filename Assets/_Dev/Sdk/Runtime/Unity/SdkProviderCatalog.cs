using System.Collections.Generic;
using MoodBoost.Sdk.Ads.Providers;
using MoodBoost.Sdk.Haptics;
using MoodBoost.Sdk.Tracking;

namespace MoodBoost.Sdk.Unity
{
    // The ONLY FILE that names a provider. This build targets TikTok Mini Game: TTSDK on a WebGL device, the
    // simulated network and the log tracker in the Editor, the null implementations everywhere else.
    //
    // Why #if rather than a reflection scan: with MB_TIKTOK off, the TikTok assembly is NOT compiled, so its class
    // names do not exist either. Reflection would compile but move the failure to runtime.
    public static class SdkProviderCatalog
    {
        // A TikTok Mini Game runs under TikTok's own privacy terms: no consent form, no privacy-options door.
        // tagForUnderAgeOfConsent stays a publisher declaration from SdkSettings.
        public static IConsentProvider CreateConsent(SdkSettings settings, out IPrivacyOptions privacyOptions)
        {
            privacyOptions = new NullPrivacyOptions();
            return new DefaultConsentProvider(settings != null && settings.TagForUnderAgeOfConsent);
        }

        // The Editor always runs the simulated network: no real network fills an ad on a desktop, and the simulated
        // one names the format, the ad unit and the placement the game actually asked for. MB_ADS_FAKE selects it
        // for a device build too (AdService initializes it only on test devices from remote config).
        public static List<IAdProvider> CreateAdProviders()
        {
#if MB_ADS_FAKE || UNITY_EDITOR
            return new List<IAdProvider> { new FakeAdProvider() };
#else
            var providers = new List<IAdProvider>(1);
#if MB_TIKTOK && UNITY_WEBGL
            providers.Add(new MoodBoost.Sdk.Ads.Mediation.TikTokAdProvider());
#endif
            if (providers.Count == 0) providers.Add(new NullAdProvider());
            return providers;
#endif
        }

        public static IHapticsProvider CreateHaptics()
        {
#if MB_TIKTOK && UNITY_WEBGL && !UNITY_EDITOR
            return new MoodBoost.Sdk.Haptics.Providers.TikTokHapticsProvider();
#else
            return new NullHapticsProvider();
#endif
        }

        // Device only: the Editor prints instead, the only way to see whether a call site fires before a build.
        // TTSDK's own Editor mock needs Play mode, so it is never used there.
        public static List<ITrackingProvider> CreateTrackingProviders()
        {
            var providers = new List<ITrackingProvider>(1);
#if UNITY_EDITOR
            providers.Add(new LogTrackingProvider());
#elif MB_TIKTOK && UNITY_WEBGL
            providers.Add(new MoodBoost.Sdk.Tracking.Providers.TikTokTrackingProvider());
#endif
            return providers;
        }

        public static string AttributionName => "none";

        // No remote config backend on TikTok yet: the defaults in SdkSettings are the values. A backend plugs in
        // here as a (service, module) pair, the module being what SdkRuntime initializes.
        public static IRemoteConfigService CreateRemoteConfig(SdkSettings settings, out ISdkModule module)
        {
            module = null;
            return new NullRemoteConfigService();
        }
    }
}
