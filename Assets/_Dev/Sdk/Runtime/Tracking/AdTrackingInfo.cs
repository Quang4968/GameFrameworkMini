namespace MoodBoost.Sdk.Tracking
{
    // Identifies which ad, from which network, in which place - the four or five parameters every ad event repeats.
    //
    // Platform is the mediation (LevelPlay, AdMob); Network is whoever won that auction. Keeping them apart matters:
    // merging two networks by mistake gives wrong ROAS numbers with no way to separate them again.
    public readonly struct AdTrackingInfo
    {
        public readonly TrackedAdFormat Format;
        public readonly string Platform;
        public readonly string Network;
        public readonly string AdUnitId;
        public readonly string Placement;

        public AdTrackingInfo(
            TrackedAdFormat format,
            string platform,
            string network,
            string adUnitId,
            string placement)
        {
            Format = format;
            Platform = platform;
            Network = network;
            AdUnitId = adUnitId;
            Placement = placement;
        }
    }
}
