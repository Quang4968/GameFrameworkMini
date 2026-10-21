namespace MoodBoost.Sdk.Ads
{
    // The outcome of one load pass over a format's whole waterfall: how long every network together took to answer,
    // and what the last of them said when none had inventory.
    public readonly struct AdLoadReport
    {
        public readonly AdFormat Format;

        // The network holding the ad after a successful load; the network that will be asked first after a failure.
        public readonly string AdUnitId;

        public readonly bool Loaded;

        // Measured across the whole waterfall, from the first request to the answer that ended the pass.
        public readonly long ElapsedMs;

        // The combined per-network messages when the pass failed, empty when it succeeded.
        public readonly string Error;

        public AdLoadReport(AdFormat format, string adUnitId, bool loaded, long elapsedMs, string error)
        {
            Format = format;
            AdUnitId = adUnitId ?? string.Empty;
            Loaded = loaded;
            ElapsedMs = elapsedMs;
            Error = error ?? string.Empty;
        }
    }
}
