namespace MoodBoost.Sdk.Ads
{
    // The end of one attempt to show a full-screen ad, whether or not anything reached the screen.
    //
    // Raised for EVERY outcome, including the ones that never showed (NotReady, Blocked, Disabled), because how often
    // a show was asked for and did not happen is exactly what the ad funnel is measured on.
    public readonly struct AdCompletion
    {
        public readonly AdFormat Format;
        public readonly string Placement;
        public readonly string AdUnitId;
        public readonly AdResult Result;

        // Time the ad held the screen. 0 when it never showed.
        public readonly long DurationMs;

        public AdCompletion(AdFormat format, string placement, string adUnitId, AdResult result, long durationMs)
        {
            Format = format;
            Placement = placement ?? string.Empty;
            AdUnitId = adUnitId ?? string.Empty;
            Result = result;
            DurationMs = durationMs;
        }
    }
}
