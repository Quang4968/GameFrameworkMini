using System.Collections.Generic;
using MoodBoost.Sdk.Ads;
using MoodBoost.Sdk.Haptics;
using MoodBoost.Sdk.Tracking;

namespace MoodBoost.Sdk.Unity
{
    // The SDK's service cluster, built exactly once per play session.
    public sealed class SdkContext
    {
        public ISdkRuntime Runtime { get; }
        public IAdService Ads { get; }
        public IAdEventStream AdEvents { get; }
        public ITrackingService Tracking { get; }
        public IAttributionService Attribution { get; }
        public IRemoteConfigService RemoteConfig { get; }
        public IAppLifecycle AppLifecycle { get; }
        public IConsentProvider Consent { get; }
        public IPrivacyOptions PrivacyOptions { get; }
        public IPlayerLevel PlayerLevel { get; }
        public ILevelTracking LevelTracking { get; }
        public IHapticsService Haptics { get; }

        internal SdkContext(
            ISdkRuntime runtime,
            IAdService ads,
            IAdEventStream adEvents,
            ITrackingService tracking,
            IAttributionService attribution,
            IRemoteConfigService remoteConfig,
            IAppLifecycle appLifecycle,
            IConsentProvider consent,
            IPrivacyOptions privacyOptions,
            IPlayerLevel playerLevel,
            ILevelTracking levelTracking,
            IHapticsService haptics)
        {
            Runtime = runtime;
            Ads = ads;
            AdEvents = adEvents;
            Tracking = tracking;
            Attribution = attribution;
            RemoteConfig = remoteConfig;
            AppLifecycle = appLifecycle;
            Consent = consent;
            PrivacyOptions = privacyOptions;
            PlayerLevel = playerLevel;
            LevelTracking = levelTracking;
            Haptics = haptics;
        }
    }

    // The SDK's construction door. Static plus DontDestroyOnLoad, built lazily on first use - the same shape as the
    // game's SceneTransitionProvider, and for the same reason: the container is destroyed with the scene while the SDK
    // has to live for the app's lifetime.
    //
    // Outside this file, SdkHost and StartupFunnel, nothing else in the system may use static. StartupFunnel is the
    // third because it has to record the launch before any of this exists.
    public static class MoodBoostSdk
    {
        const string Tag = "Sdk";

        static SdkContext current;
        static SdkHost host;

        public static SdkContext GetOrCreate(SdkSettings settings)
        {
            if (current != null) return current;

            if (settings == null)
            {
                SdkLog.Warn(Tag, "SdkSettings is not assigned - the SDK runs in empty mode, the game is unaffected.");
            }

            host = SdkHost.Create();

            var clock = new RealtimeSdkClock();
            var consent = SdkProviderCatalog.CreateConsent(settings, out var privacyOptions);

            var tracking = new TrackingService(SdkProviderCatalog.CreateTrackingProviders());
            var attribution = new NullAttributionService();

            // From here the funnel has somewhere to send to; it still queues until the flush.
            StartupFunnel.Attach(tracking);

            // Not written as a ternary: an out variable declared inside a ternary branch is not treated as definitely
            // assigned by the compiler below.
            IRemoteConfigService remoteConfig;
            ISdkModule remoteConfigModule = null;
            if (settings != null)
            {
                remoteConfig = SdkProviderCatalog.CreateRemoteConfig(settings, out remoteConfigModule);
            }
            else
            {
                remoteConfig = new NullRemoteConfigService();
            }

            var slots = AdSlotBuilder.Build(SdkProviderCatalog.CreateAdProviders(), settings, consent);

            // Empty until the game calls SetProvider at boot; level-gated ad rules are skipped until then.
            var playerLevel = new PlayerLevel();

            var ads = new AdService(slots, remoteConfig, playerLevel, clock, host.LifetimeToken);

            // Level events read the level from the same source as the ad rules, so the game never passes it.
            var levelTracking = new LevelTracking(tracking, playerLevel);

            // Joins ads -> tracking. The only place the two layers meet.
            var bridge = new AdTrackingBridge(ads, tracking);

            // Joins device reachability -> tracking user properties, same shape.
            var userProperties = new UserPropertyBridge(tracking, attribution, host, host.LifetimeToken);

            // No init and no module: a motor is ready the moment it exists.
            var haptics = new HapticsService(SdkProviderCatalog.CreateHaptics(), clock);

            var modules = new List<ISdkModule> { tracking, ads, userProperties };
            if (remoteConfigModule != null) modules.Add(remoteConfigModule);
            else modules.Add(new AbsentSdkModule("RemoteConfig", SdkModuleOrder.RemoteConfig));

            var runtime = new SdkRuntime(modules);

            // Joins the init reports -> tracking, same shape as the bridges above.
            var initBridge = new SdkInitTrackingBridge(runtime, tracking, new ISdkInitReporter[] { runtime, tracking, ads });

            host.Disposing += () =>
            {
                bridge.Dispose();
                initBridge.Dispose();
                userProperties.Dispose();
                ads.Dispose();
                tracking.Dispose();
                current = null;
                host = null;
            };

            current = new SdkContext(
                runtime, ads, ads, tracking, attribution, remoteConfig, host, consent, privacyOptions, playerLevel,
                levelTracking, haptics);
            return current;
        }
    }
}
