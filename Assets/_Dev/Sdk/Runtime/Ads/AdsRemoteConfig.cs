namespace MoodBoost.Sdk.Ads
{
    // The ads layer's remote config parameters, and the SINGLE SOURCE OF TRUTH for AdPolicySettings.
    //
    // These numbers used to be fields on SdkSettings. They lived there because there was no remote config yet, so
    // changing the ad cadence meant shipping a new build - while ad frequency is precisely the thing that has to be
    // tuned against data after release, not something settled while making the game.
    //
    // Now they live here: the defaults in code are the offline configuration, the console is the real one. There is no
    // longer any field on an asset that can diverge from the template.
    //
    // This is also the pattern for declaring the game's own keys: one static class, one `public static readonly` per
    // key, one shared Group string. The Editor template exporter finds them on its own, with no list to register in -
    // forgetting to register is the kind of bug nothing can detect until somebody changes a number on the console and
    // sees the game ignore it.
    public static class AdsRemoteConfig
    {
        const string Group = "Ads";

        public static readonly ConfigKey<double> InterstitialCooldownSeconds = ConfigKey.Double(
            "ads_interstitial_cooldown_seconds",
            30d,
            "Minimum pause between two interstitials, in seconds.",
            Group);

        public static readonly ConfigKey<double> InterstitialCooldownAfterRewardedSeconds = ConfigKey.Double(
            "ads_interstitial_cooldown_after_rewarded_seconds",
            45d,
            "The pause after the player has just watched a rewarded ad, in seconds. Guards against the most common UX " +
            "mistake: finish a rewarded ad, close it, and immediately eat an interstitial.",
            Group);

        public static readonly ConfigKey<double> InterstitialFirstCooldownSeconds = ConfigKey.Double(
            "ads_interstitial_first_cooldown_seconds",
            30d,
            "The pause before the first interstitial of a session, counted from app launch, in seconds.",
            Group);

        public static readonly ConfigKey<long> InterstitialFirstLevel = ConfigKey.Long(
            "ads_interstitial_first_level",
            20L,
            "The first level (as the player sees it, from 1) that can show an interstitial. Skipped when the game " +
            "has not given IPlayerLevel a provider.",
            Group);

        public static readonly ConfigKey<double> AppOpenCooldownSeconds = ConfigKey.Double(
            "ads_app_open_cooldown_seconds",
            60d,
            "Minimum pause between two app opens, in seconds.",
            Group);

        public static readonly ConfigKey<bool> BannerEnabled = ConfigKey.Bool(
            "ads_banner_enabled",
            true,
            "Turns banners off for every player without shipping a new build. Turning it off while a banner is " +
            "visible removes it immediately.",
            Group);

        public static readonly ConfigKey<double> RetryBaseSeconds = ConfigKey.Double(
            "ads_retry_base_seconds",
            2d,
            "The retry cadence after the first no-fill, in seconds. It doubles on each subsequent failure.",
            Group);

        public static readonly ConfigKey<double> RetryMaxSeconds = ConfigKey.Double(
            "ads_retry_max_seconds",
            64d,
            "The cap on the retry cadence, in seconds. Lowering this while fill rate is poor burns battery and " +
            "invites the ad network to block you.",
            Group);

        public static readonly ConfigKey<bool> ForwardPlacementToNetwork = ConfigKey.Bool(
            "ads_forward_placement_to_network",
            false,
            "Forwards the game's placement name down to mediation. Turn on ONLY after every placement is declared " +
            "on the dashboard - sending an undeclared name is a bug that only surfaces on a real device.",
            Group);

        public static readonly ConfigKey<string> TestDeviceIds = ConfigKey.String(
            "ads_test_device_ids",
            "",
            "Comma-separated test device ids (\"id1, id2\"). A device whose id is in the list runs the test ad unit " +
            "ids declared in SdkSettings instead of the real ones. The device id is printed to logcat as soon as " +
            "this list is non-empty. Left blank in normal operation.",
            Group);

        // Reads the whole set into one struct. AdService calls this at init and on every config change.
        public static AdPolicySettings BuildPolicy(IRemoteConfigService config)
        {
            if (config == null) return AdPolicySettings.Default;

            return new AdPolicySettings(
                config.Get(InterstitialCooldownSeconds),
                config.Get(InterstitialCooldownAfterRewardedSeconds),
                config.Get(InterstitialFirstCooldownSeconds),
                config.Get(InterstitialFirstLevel),
                config.Get(AppOpenCooldownSeconds),
                config.Get(RetryBaseSeconds),
                config.Get(RetryMaxSeconds),
                config.Get(ForwardPlacementToNetwork),
                config.Get(BannerEnabled));
        }
    }
}
