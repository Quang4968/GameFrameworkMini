namespace MoodBoost.Sdk
{
    // Initialization order, kept as plain numbers rather than an enum so an external module (added later) can slot
    // in between without editing this file.
    public static class SdkModuleOrder
    {
        // The crash reporter goes first, before consent itself: its whole value is being alive before something else
        // dies, and everything below can crash while initializing. It sends no ad request and asks no network for
        // inventory, so the rule that gates the networks behind consent does not apply to it.
        public const int Crashlytics = -100;

        // Consent runs before everything else: the ad networks must know the GDPR/CCPA state BEFORE init; setting it
        // afterwards means the first request already went out with the wrong flags.
        public const int Consent = 0;

        // ATT must finish before either the MMP or mediation opens its first session on iOS.
        public const int TrackingAuthorization = 50;

        // Tracking comes before ads so the first impression/revenue event has somewhere to land.
        public const int Tracking = 100;

        // User properties come after tracking (the providers must be live to receive them) and before ads, so an
        // impression logged seconds later already carries the player's segment.
        public const int UserProperties = 150;

        // Remote config comes before ads because frequency/capping is read from it.
        public const int RemoteConfig = 200;

        // Notifications only register a channel and read the permission - no prompt, no network - so they go before
        // the slow ad and store connections rather than making the launch-tap report wait behind them.
        public const int Notifications = 250;

        public const int Ads = 300;

        // IAP goes last: connecting to the store is the slowest network call in the whole set, and no layer has to
        // wait on it. Placing it earlier only adds seconds to the loading screen for nothing.
        public const int Iap = 400;
    }
}
