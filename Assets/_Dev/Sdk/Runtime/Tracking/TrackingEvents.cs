namespace MoodBoost.Sdk.Tracking
{
    // Names of the events in the tracking plan. Kept as constants in one place because event names CANNOT be
    // changed later: renaming after data has been sent loses the historical part on the dashboard.
    //
    // The set below mirrors the Databuckets tracking plan one-to-one, plus the SDK's own startup lifecycle events.
    // Anything else does not belong here - a game inventing its own event declares it on the game side.
    public static class TrackingEvents
    {
        public const string LevelStart = "level_start";
        public const string LevelEnd = "level_end";
        public const string LevelExit = "level_exit";
        public const string LevelReopen = "level_reopen";

        public const string ResourceEarn = "resource_earn";
        public const string ResourceSpend = "resource_spend";

        public const string IapShow = "iap_show";
        public const string IapClick = "iap_click";
        public const string IapPurchaseSuccess = "iap_purchase_success";
        public const string IapPurchaseFailed = "iap_purchase_failed";
        public const string IapClose = "iap_close";

        public const string AdRequest = "ad_request";
        public const string AdImpression = "ad_impression";
        public const string AdOfferShown = "ad_offer_shown";
        // Not "ad_click": Firebase reserves that name for the AdMob SDK and drops it when logged by hand.
        public const string AdClick = "mediation_ad_click";
        public const string AdComplete = "ad_complete";

        public const string NotiSend = "noti_send";
        public const string NotiReceive = "noti_receive";
        public const string NotiOpen = "noti_open";

        // NOT in the Databuckets plan: the SDK fires it itself whenever the notification permission changes, so opt-in
        // can be read without spending one of Firebase's 25 user properties. Add it to the sheet before relying on it.
        public const string NotiPermission = "noti_permission";

        public const string TutorialAction = "tutorial_action";

        // Startup funnel. Fired entirely inside the SDK: no line of game code names them, the game only calls
        // StartupFunnel. app_launch and splash_loaded happen before any provider is up, so they wait in
        // StartupFunnel's queue and are sent once tracking is live - their Firebase timestamp is therefore LATER than
        // the init events they precede. since_launch is the real clock.
        public const string AppLaunch = "app_launch";
        public const string SplashLoaded = "splash_loaded";
        public const string LoadingDone = "loading_done";

        // Prefixed onto an SdkInitIds value: "init_" + "crashlytics".
        public const string InitPrefix = "init_";

        // A standard GA4 name, so Firebase recognizes it as revenue. DO NOT change it to "iap_purchase" or any
        // custom name: doing so loses Firebase's built-in revenue reports.
        public const string Purchase = "purchase";
    }

    public static class TrackingKeys
    {
        // Level
        public const string Level = "level";
        public const string LoopBy = "loop_by";
        public const string PlayType = "play_type";
        public const string PlayIndex = "play_index";
        public const string LoseIndex = "lose_index";
        public const string ExitIndex = "exit_index";
        public const string Mode = "mode";
        public const string DurationTotalStart = "duration_total_start";
        public const string DurationTotalEnd = "duration_total_end";
        public const string DurationRemain = "duration_remain";
        public const string DurationPlay = "duration_play";
        public const string ItemsTotal = "items_total";
        public const string ItemsCleared = "items_cleared";
        public const string ActionSeq = "action_seq";
        public const string Result = "result";
        public const string LoseBy = "lose_by";

        // Resource
        public const string ResourceType = "resource_type";
        public const string ResourceName = "resource_name";
        public const string ResourceAmount = "resource_amount";
        public const string ResourceBalance = "resource_balance";
        public const string Reason = "reason";

        // Shared by resource, IAP and ads
        public const string Placement = "placement";
        public const string PlacementDetail = "placement_detail";

        // IAP
        public const string ShowType = "show_type";
        public const string TriggerType = "trigger_type";
        public const string PackName = "pack_name";
        public const string Price = "price";
        public const string FailReason = "fail_reason";
        public const string DurationIap = "duration_iap";

        // Ads
        public const string AdFormat = "ad_format";
        public const string AdPlatform = "ad_platform";
        public const string AdNetwork = "ad_network";
        public const string AdUnitId = "ad_unit_id";
        public const string IsLoad = "is_load";
        public const string IsShow = "is_show";
        public const string LoadTime = "load_time";
        public const string EndType = "end_type";
        public const string DurationAd = "duration_ad";
        public const string ErrorType = "error_type";
        public const string ErrorCode = "error_code";

        // Startup funnel
        public const string Sdk = "sdk";
        public const string InitScope = "init_scope";
        public const string DurationInit = "duration_init";
        public const string SinceLaunch = "since_launch";
        public const string WaitBy = "wait_by";
        public const string WasBackgrounded = "was_backgrounded";

        // Notifications
        public const string NotiCate = "noti_cate";
        public const string NotiName = "noti_name";

        // Tutorial
        public const string ActionCate = "action_cate";
        public const string ActionName = "action_name";
        public const string ActionIndex = "action_index";

        // Shared / legacy
        public const string Value = "value";
        public const string Currency = "currency";
        public const string Precision = "precision";
        public const string Error = "error";
        public const string ProductId = "product_id";
        public const string TransactionId = "transaction_id";
        public const string Quantity = "quantity";
    }
}
