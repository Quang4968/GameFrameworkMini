using System.Collections.Generic;
using System.Text;

namespace MoodBoost.Sdk.Tracking
{
    // Converts the plan's enums into the exact strings the dashboard expects, and holds the plan's string
    // conventions in one place.
    //
    // Every mapping is an explicit literal rather than a PascalCase-to-snake_case splitter: these values are the
    // contract with the data team, so they must be greppable and must not shift when an enum member is renamed.
    public static class TrackingValues
    {
        // The plan asks for the literal string "null" on absent values, NOT an omitted parameter - a missing key and
        // a "null" key mean different things once the data is aggregated.
        public const string Null = "null";

        // action_seq uses "0" rather than an empty string when the player did nothing at all.
        public const string NoAction = "0";

        // Firebase silently truncates string parameter values at 100 characters.
        public const int MaxValueLength = 100;

        public static string OrNull(string value) => string.IsNullOrEmpty(value) ? Null : value;

        public static string ToParam(this PlayType value)
        {
            switch (value)
            {
                case PlayType.Home: return "home";
                case PlayType.Next: return "next";
                case PlayType.Restart: return "restart";
                default: return Null;
            }
        }

        public static string ToParam(this LevelResult value)
        {
            switch (value)
            {
                case LevelResult.Win: return "win";
                case LevelResult.Lose: return "lose";
                case LevelResult.Quit: return "quit";
                case LevelResult.Exit: return "exit";
                default: return Null;
            }
        }

        public static string ToParam(this ResourceType value)
        {
            switch (value)
            {
                case ResourceType.Currency: return "currency";
                case ResourceType.Item: return "item";
                case ResourceType.Booster: return "booster";
                default: return Null;
            }
        }

        public static string ToParam(this EarnReason value)
        {
            switch (value)
            {
                case EarnReason.Reward: return "reward";
                case EarnReason.Exchange: return "exchange";
                case EarnReason.Purchase: return "purchase";
                case EarnReason.WatchAds: return "watch_ads";
                default: return Null;
            }
        }

        public static string ToParam(this SpendReason value)
        {
            switch (value)
            {
                case SpendReason.Exchange: return "exchange";
                case SpendReason.Use: return "use";
                default: return Null;
            }
        }

        public static string ToParam(this IapShowType value)
        {
            switch (value)
            {
                case IapShowType.Shop: return "shop";
                case IapShowType.Pack: return "pack";
                default: return Null;
            }
        }

        public static string ToParam(this IapTriggerType value)
        {
            switch (value)
            {
                case IapTriggerType.Popup: return "popup";
                case IapTriggerType.Click: return "click";
                default: return Null;
            }
        }

        public static string ToParam(this AdEndType value)
        {
            switch (value)
            {
                case AdEndType.Quit: return "quit";
                case AdEndType.Done: return "done";
                default: return Null;
            }
        }

        public static string ToParam(this TrackedAdFormat value)
        {
            switch (value)
            {
                case TrackedAdFormat.Banner: return "banner";
                case TrackedAdFormat.Interstitial: return "interstitial";
                case TrackedAdFormat.Rewarded: return "video_rewarded";
                case TrackedAdFormat.RewardedInterstitial: return "rewarded_interstitial";
                case TrackedAdFormat.AppOpen: return "app_open";
                case TrackedAdFormat.Native: return "native";
                case TrackedAdFormat.Mrec: return "mrec";
                default: return Null;
            }
        }

        public static string ToParam(this AdErrorType value)
        {
            switch (value)
            {
                case AdErrorType.None: return Null;
                case AdErrorType.NoFill: return "no_fill";
                case AdErrorType.Network: return "network";
                case AdErrorType.Timeout: return "timeout";
                case AdErrorType.Config: return "config";
                case AdErrorType.NotReady: return "not_ready";
                case AdErrorType.Expired: return "expired";
                case AdErrorType.InvalidState: return "invalid_state";
                case AdErrorType.AlreadyUsed: return "already_used";
                case AdErrorType.RenderError: return "render_error";
                default: return "unknown";
            }
        }

        public static string ToParam(this ConnectionType value)
        {
            switch (value)
            {
                case ConnectionType.Offline: return "offline";
                case ConnectionType.Wifi: return "wifi";
                case ConnectionType.MobileData: return "mobile_data";
                default: return "unknown";
            }
        }

        // Startup funnel results. Success is shared by init_* and loading_done; Timeout belongs to loading_done
        // alone - an init step that ran out of time is reported as a plain fail by whoever was waiting on it.
        public const string Success = "success";
        public const string Timeout = "timeout";

        // Startup funnel: whether one init step came up, gave up, or was never built for this session.
        public static string ToParam(this SdkInitResult value)
        {
            switch (value)
            {
                case SdkInitResult.Success: return Success;
                case SdkInitResult.Fail: return "fail";
                case SdkInitResult.Skipped: return "skipped";
                default: return "fail";
            }
        }

        // Which half of the init a report describes: a module of SdkRuntime, or one provider inside it.
        public static string InitScope(bool isModule) => isModule ? "module" : "provider";

        // Which latch closed last on loading_done - the SDK finishing, or the Home screen becoming visible.
        public const string WaitBySdk = "sdk";
        public const string WaitByUi = "ui";

        // The plan's list format: comma separated, NO space after the comma, and index-aligned across the parallel
        // parameters (resource_type / resource_name / resource_amount / resource_balance).
        public static string Join(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0) return Null;
            if (values.Count == 1) return OrNull(values[0]);

            var builder = new StringBuilder();
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append(OrNull(values[i]));
            }

            return Truncate(builder.ToString());
        }

        // The ordered aliases of the actions the player took, "0" when they took none.
        public static string Sequence(IReadOnlyList<int> aliases)
        {
            if (aliases == null || aliases.Count == 0) return NoAction;

            var builder = new StringBuilder();
            for (var i = 0; i < aliases.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append(aliases[i]);
            }

            return Truncate(builder.ToString());
        }

        // Cuts on a comma so a truncated list is still parseable, rather than ending mid-number.
        public static string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= MaxValueLength) return value;

            var cut = value.LastIndexOf(',', MaxValueLength - 1);
            return cut > 0 ? value.Substring(0, cut) : value.Substring(0, MaxValueLength);
        }
    }
}
