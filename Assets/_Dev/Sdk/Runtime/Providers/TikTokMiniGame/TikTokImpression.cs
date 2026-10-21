namespace MoodBoost.Sdk.Ads.Mediation
{
    // TTSDK reports no impression-level revenue, so the impression carries 0: it still counts shows for ad_impression,
    // while revenue comes from the TikTok console, not from the device.
    static class TikTokImpression
    {
        const string Network = "tiktok";

        public static AdImpression Create(AdFormat format, string placement, string adUnitId)
            => new AdImpression(format, placement, adUnitId, Network, 0d, "USD", "none", null);
    }
}
