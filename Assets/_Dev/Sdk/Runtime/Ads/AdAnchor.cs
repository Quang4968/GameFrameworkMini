namespace MoodBoost.Sdk.Ads
{
    // The anchor position of a framed ad (banner, MREC). Deliberately only a few coarse anchors: every ad network
    // interprets coordinates its own way, so the adapter maps from this anchor into its own form, and the game never
    // speaks in pixels.
    public enum AdAnchor
    {
        Top = 0,
        Center = 1,
        Bottom = 2,
    }
}
