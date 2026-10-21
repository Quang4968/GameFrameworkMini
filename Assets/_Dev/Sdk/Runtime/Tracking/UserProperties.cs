namespace MoodBoost.Sdk.Tracking
{
    // The tracking plan's user property names, one const per key.
    //
    // Unlike an event parameter, a user property name is bound by Firebase to 24 characters, and Firebase caps a
    // PROJECT at 25 custom properties for its whole lifetime - a ceiling that cannot be raised. The catalog below
    // spends 21 of them, so every addition is a real decision. UserPropertyCatalogTests asserts both limits.
    public static class UserProperties
    {
        // ---- Filled by the SDK from the MMP, no game code involved ----

        // Install-scoped id created by the MMP, used to join Firebase data against Tenjin's.
        public const string AttributionId = "attribution_id";

        public const string UaNetwork = "ua_network";
        public const string UaCampaign = "ua_campaign";
        public const string UaAdGroup = "ua_adgroup";
        public const string UaCreative = "ua_creative";

        // ---- Filled by the SDK from the device ----

        public const string ConnectionType = "connection_type";

        // ---- Filled by the game ----

        // Calendar days since the player first opened the game, in their install-local timezone. D0 is install day.
        public const string ActiveDay = "active_day_n";

        public const string IsIapUser = "is_iap_user_n";
        public const string IapCount = "iap_count_n";
        public const string CurrentLevel = "current_level";
        public const string CurrentMode = "current_mode";
        public const string CurrentEvent = "current_event";
        public const string WinStreak = "win_streak_n";
        public const string LoseStreak = "lose_streak_n";

        // ---- Resource balances ----

        public const string BalancePrefix = "balance_";
        public const string BalanceSuffix = "_n";

        // Which resources matter differs per game, so the name stays a string here - the same rule TrackingKeys
        // already applies to resource_name. "coin" -> "balance_coin_n".
        public static string Balance(string resource)
            => string.IsNullOrEmpty(resource) ? null : BalancePrefix + resource + BalanceSuffix;
    }
}
