namespace MoodBoost.Sdk.Tracking
{
    // Where the player was acquired from, normalized into one common set of fields.
    //
    // Every MMP returns a different shape (Tenjin returns Dictionary<string,string>, Adjust returns
    // AdjustAttribution, AppsFlyer returns Dictionary<string,object>). Normalizing here means game code learns
    // only one set of names - which is what makes swapping MMPs possible without editing the read sites.
    //
    // Fields the MMP does not provide are left empty rather than throwing: each ad network fills in a different
    // subset, and an organic install is almost entirely blank.
    public readonly struct AttributionData
    {
        // true when there is no ad source - the player found the game on the store themselves.
        public readonly bool IsOrganic;

        // Name of the ad source (facebook, googleadwords, unityads...).
        public readonly string Network;

        // The ad network's campaign id (Tenjin's remote_campaign_id) - an id rather than a display name, because the
        // id is what the UA sheet joins on and a name can be renamed in the network's console at any time.
        public readonly string Campaign;

        public readonly string AdGroup;
        public readonly string Creative;

        // Exactly the four fields that become ua_* user properties, and nothing else. Every field here costs one of
        // the 25 user properties Firebase allows a project for its whole lifetime, so an MMP field with no Firebase
        // column (Tenjin's campaign_name, click_id, advertising_id...) is left out rather than carried "just in case".
        public AttributionData(
            bool isOrganic,
            string network,
            string campaign,
            string adGroup,
            string creative)
        {
            IsOrganic = isOrganic;
            Network = network ?? string.Empty;
            Campaign = campaign ?? string.Empty;
            AdGroup = adGroup ?? string.Empty;
            Creative = creative ?? string.Empty;
        }

        public static AttributionData Organic => new AttributionData(true, null, null, null, null);
    }
}
