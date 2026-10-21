namespace MoodBoost.Sdk
{
    // The analytics id of each piece that initializes, one explicit literal each.
    //
    // These become event names ("init_" + id), so they can never change once data has shipped, and they must stay
    // snake_case: GA4 drops an event name outside [A-Za-z][A-Za-z0-9_]{0,39} silently. Deriving them from ModuleName
    // would produce "init_Ads (Fake)" and "init_IAP (Unity Purchasing)" - both illegal, and the first one changes with
    // the configured waterfall.
    public static class SdkInitIds
    {
        // Modules of SdkRuntime.
        public const string Crashlytics = "crashlytics";
        public const string Consent = "consent";
        public const string TrackingAuthorization = "att";
        public const string Tracking = "tracking";
        public const string UserProperties = "user_properties";
        public const string RemoteConfig = "remote_config";
        public const string Notifications = "notifications";
        public const string Ads = "ads";
        public const string Iap = "iap";

        // Providers inside a module. IAP and remote config have exactly one provider each, so their module id already
        // names it and there is nothing to report twice.
        public const string LogTracking = "log";
        public const string TikTokTracking = "tiktok_tracking";
        public const string FirebaseAnalytics = "firebase_analytics";
        public const string Mmp = "mmp";
        public const string LevelPlay = "levelplay";
        public const string FakeAds = "fake_ads";
        public const string TikTokAds = "tiktok_ads";
        public const string NullAds = "null_ads";
        public const string UnityNotifications = "unity_notifications";
        public const string LogNotifications = "log_notifications";
        public const string FirebaseMessaging = "firebase_messaging";
    }
}
