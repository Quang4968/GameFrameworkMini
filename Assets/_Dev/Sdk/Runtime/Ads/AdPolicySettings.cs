namespace MoodBoost.Sdk.Ads
{
    // The parameters of the ad display rules. Plain C# rather than a ScriptableObject so AdFrequencyPolicy is testable
    // under NUnit without opening the Editor - the same reason the game's Core does not use SOs.
    //
    // This struct is a SNAPSHOT at one moment, not the source of truth. The source is AdsRemoteConfig: AdService
    // re-reads it and pushes down a new snapshot every time remote config changes.
    public readonly struct AdPolicySettings
    {
        // Minimum pause between two interstitials.
        public readonly double InterstitialCooldownSeconds;

        // The pause after the player has just watched a rewarded ad. Separate because this is the most common UX
        // mistake: finish a rewarded ad, close it, and immediately eat an interstitial.
        public readonly double InterstitialCooldownAfterRewardedSeconds;

        // The pause before the first interstitial of a session, counted from when AdService is built (app launch), so a
        // player does not open the app and hit an ad on the very first tap.
        public readonly double InterstitialFirstCooldownSeconds;

        // The first level that can show an interstitial, counted from 1 and compared against IPlayerLevel.
        public readonly long InterstitialFirstLevel;

        // Minimum pause between two app opens. Guards against a player switching apps repeatedly.
        public readonly double AppOpenCooldownSeconds;

        // Retry cadence after a no-fill: doubles on each failure, capped at Max.
        public readonly double RetryBaseSeconds;
        public readonly double RetryMaxSeconds;

        // Whether to forward the placement name down to the ad network.
        //
        // Off by default, and that is a deliberate choice: the game's placement names ("extra_life",
        // "double_reward") are analytics labels, whereas a mediation placement is an entity that must be declared on
        // the dashboard first. Sending an undeclared name down means, depending on the network, that it is ignored or
        // returns a display error - and that error only surfaces on a real device.
        //
        // Turn it on only once every placement is correctly declared on the mediation dashboard. On or off, the SDK's
        // events and tracking data always carry the game's placement name.
        public readonly bool ForwardPlacementToNetwork;

        // A switch to turn banners off for every player without a new build. When off, ShowBanner does nothing and any
        // visible banner is removed at the next config read.
        public readonly bool BannerEnabled;

        public AdPolicySettings(
            double interstitialCooldownSeconds,
            double interstitialCooldownAfterRewardedSeconds,
            double interstitialFirstCooldownSeconds,
            long interstitialFirstLevel,
            double appOpenCooldownSeconds,
            double retryBaseSeconds,
            double retryMaxSeconds,
            bool forwardPlacementToNetwork = false,
            bool bannerEnabled = true)
        {
            ForwardPlacementToNetwork = forwardPlacementToNetwork;
            BannerEnabled = bannerEnabled;
            InterstitialCooldownSeconds = interstitialCooldownSeconds;
            InterstitialCooldownAfterRewardedSeconds = interstitialCooldownAfterRewardedSeconds;
            InterstitialFirstCooldownSeconds = interstitialFirstCooldownSeconds;
            InterstitialFirstLevel = interstitialFirstLevel;
            AppOpenCooldownSeconds = appOpenCooldownSeconds;

            // Clamped here rather than trusting the values read back: these two come from the console, and a mistyped
            // zero there would become a non-stop reload loop on every player's device.
            RetryBaseSeconds = retryBaseSeconds <= 0d ? 2d : retryBaseSeconds;
            RetryMaxSeconds = retryMaxSeconds <= 0d ? 64d : retryMaxSeconds;
        }

        // The configuration used before anything can be read: fetch not finished, no network, or MB_REMOTE_CONFIG off
        // entirely.
        //
        // Taken straight from each ConfigKey's default so there is NO way to diverge from the template exported to the
        // console - previously these two sets of numbers lived in two files and had to be kept equal by hand.
        public static AdPolicySettings Default => new AdPolicySettings(
            AdsRemoteConfig.InterstitialCooldownSeconds.Default,
            AdsRemoteConfig.InterstitialCooldownAfterRewardedSeconds.Default,
            AdsRemoteConfig.InterstitialFirstCooldownSeconds.Default,
            AdsRemoteConfig.InterstitialFirstLevel.Default,
            AdsRemoteConfig.AppOpenCooldownSeconds.Default,
            AdsRemoteConfig.RetryBaseSeconds.Default,
            AdsRemoteConfig.RetryMaxSeconds.Default,
            AdsRemoteConfig.ForwardPlacementToNetwork.Default,
            AdsRemoteConfig.BannerEnabled.Default);
    }
}
