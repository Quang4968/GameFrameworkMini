namespace MoodBoost.Sdk.Tracking
{
    // The revenue of a single ad impression, as the TRACKING LAYER sees it.
    //
    // Deliberately its own type, meaning the same thing as AdImpression on the ads side but NOT shared: sharing
    // would force the Tracking assembly to reference the Ads assembly, and from then on removing either one from
    // the project breaks the other. The bridge between the two types is AdTrackingBridge, exactly one file.
    public readonly struct AdRevenue
    {
        // Mediation name (levelplay, max, admob) - where the number came from.
        public readonly string MediationName;

        // The network that won the auction.
        public readonly string AdNetwork;

        public readonly string AdUnitId;
        public readonly string AdFormat;
        public readonly string Placement;
        public readonly double Value;
        public readonly string Currency;
        public readonly string Precision;

        public AdRevenue(
            string mediationName,
            string adNetwork,
            string adUnitId,
            string adFormat,
            string placement,
            double value,
            string currency,
            string precision)
        {
            MediationName = mediationName ?? string.Empty;
            AdNetwork = adNetwork ?? string.Empty;
            AdUnitId = adUnitId ?? string.Empty;
            AdFormat = adFormat ?? string.Empty;
            Placement = placement ?? string.Empty;
            Value = value;
            Currency = string.IsNullOrEmpty(currency) ? "USD" : currency;
            Precision = precision ?? string.Empty;
        }
    }
}
