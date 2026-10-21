namespace MoodBoost.Sdk.Tracking
{
    // The closed value sets of the tracking plan. Only sets the plan fixes are enums; open sets that differ per game
    // (placement, resource_name, pack_name, lose_by, mode, noti_cate) stay plain strings.
    //
    // Never reorder or insert - these map to strings by an explicit switch in TrackingValues, so the numbers
    // themselves are not stored anywhere, but a reordered enum silently changes what every existing call site means.

    public enum PlayType
    {
        Home = 0,
        Next = 1,
        Restart = 2,
    }

    public enum LevelResult
    {
        Win = 0,
        Lose = 1,
        Quit = 2,
        Exit = 3,
    }

    public enum ResourceType
    {
        Currency = 0,
        Item = 1,
        Booster = 2,
    }

    public enum EarnReason
    {
        Reward = 0,
        Exchange = 1,
        Purchase = 2,
        WatchAds = 3,
    }

    public enum SpendReason
    {
        Exchange = 0,
        Use = 1,
    }

    public enum IapShowType
    {
        Shop = 0,
        Pack = 1,
    }

    public enum IapTriggerType
    {
        Popup = 0,
        Click = 1,
    }

    public enum AdEndType
    {
        Quit = 0,
        Done = 1,
    }

    // A tracking-local twin of MoodBoost.Sdk.Ads.AdFormat: the Tracking assembly does not reference Ads, for the same
    // reason AdRevenue keeps its format as a string. AdTrackingBridge is the one place that converts between them.
    public enum TrackedAdFormat
    {
        Banner = 0,
        Interstitial = 1,
        Rewarded = 2,
        RewardedInterstitial = 3,
        AppOpen = 4,
        Native = 5,
        Mrec = 6,
    }

    // The normalized failure buckets of the plan, covering both load failures and show failures. None means success,
    // and logs as the literal string "null".
    public enum AdErrorType
    {
        None = 0,
        NoFill = 1,
        Network = 2,
        Timeout = 3,
        Config = 4,
        NotReady = 5,
        Expired = 6,
        InvalidState = 7,
        AlreadyUsed = 8,
        RenderError = 9,
        Unknown = 10,
    }

    // How the device reaches the internet, for the connection_type user property.
    //
    // Unknown is what gets published before the first probe runs; a successful probe always lands on one of the other
    // three. Beware two things the platform does not distinguish: Wifi also covers ethernet and the simulator, and
    // none of these values means requests will actually succeed - a captive portal still reports Wifi.
    public enum ConnectionType
    {
        Unknown = 0,
        Offline = 1,
        Wifi = 2,
        MobileData = 3,
    }
}
