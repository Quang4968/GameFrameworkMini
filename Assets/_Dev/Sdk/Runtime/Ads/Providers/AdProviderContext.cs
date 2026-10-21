namespace MoodBoost.Sdk.Ads.Providers
{
    // Everything a mediation adapter needs at init, bundled in one place.
    //
    // Bundled into a struct rather than added as parameters to InitializeAsync: when a new network needs extra
    // information (app id, sdk key...), add a field here instead of changing every existing adapter's signature.
    public readonly struct AdProviderContext
    {
        // The mediation app key. LevelPlay calls it appKey, MAX calls it sdkKey, AdMob does not need one.
        public readonly string AppKey;

        public readonly string UserId;

        public readonly IConsentProvider Consent;

        // Enables the network's test mode (test suite, test device). Must NOT be on in a release build: test
        // impressions still count against the account and corrupt the data.
        public readonly bool TestMode;

        public AdProviderContext(string appKey, string userId, IConsentProvider consent, bool testMode)
        {
            AppKey = appKey ?? string.Empty;
            UserId = userId;
            Consent = consent ?? new DefaultConsentProvider();
            TestMode = testMode;
        }
    }
}
