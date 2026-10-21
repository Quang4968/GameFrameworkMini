namespace MoodBoost.Sdk.Ads
{
    // One ad display, with the estimated revenue reported by mediation.
    //
    // A readonly struct because it travels through an event on every impression - a banner refreshes every 30 seconds
    // for the whole level, which is not worth generating garbage for.
    //
    // This type belongs to the ads layer ONLY. Tracking has its own AdRevenue type, and the bridge between the two is
    // AdTrackingBridge - which is how the two assemblies avoid knowing about each other (see ARCHITECTURE.md).
    public readonly struct AdImpression
    {
        // The mediation network that served this display. The adapter does NOT fill this in - it does not know which
        // rung of the waterfall it sits on. The stamping happens in AdWaterfall*AdUnit, see WithMediation.
        public readonly MediationId Mediation;

        public readonly AdFormat Format;
        public readonly string Placement;
        public readonly string AdUnitId;

        // Name of the network that won the auction (applovin, unityads, admob...), not the mediation name.
        public readonly string Network;

        public readonly double Revenue;
        public readonly string Currency;

        // How much to trust the revenue number mediation assigned (exact, publisher_defined, estimated...).
        public readonly string Precision;

        public readonly string Country;

        public AdImpression(
            AdFormat format,
            string placement,
            string adUnitId,
            string network,
            double revenue,
            string currency,
            string precision,
            string country)
            : this(MediationId.None, format, placement, adUnitId, network, revenue, currency, precision, country)
        {
        }

        AdImpression(
            MediationId mediation,
            AdFormat format,
            string placement,
            string adUnitId,
            string network,
            double revenue,
            string currency,
            string precision,
            string country)
        {
            Mediation = mediation;
            Format = format;
            Placement = placement ?? string.Empty;
            AdUnitId = adUnitId ?? string.Empty;
            Network = network ?? string.Empty;
            Revenue = revenue;
            Currency = string.IsNullOrEmpty(currency) ? "USD" : currency;
            Precision = precision ?? string.Empty;
            Country = country ?? string.Empty;
        }

        public AdImpression WithMediation(MediationId mediation) => new AdImpression(
            mediation, Format, Placement, AdUnitId, Network, Revenue, Currency, Precision, Country);

        // The adapter only learns a placement when the game's name is forwarded to the network, which is off by
        // default - so AdService stamps the placement it was actually shown for. See AdPolicySettings.
        public AdImpression WithPlacement(string placement) => new AdImpression(
            Mediation, Format, placement, AdUnitId, Network, Revenue, Currency, Precision, Country);
    }
}
