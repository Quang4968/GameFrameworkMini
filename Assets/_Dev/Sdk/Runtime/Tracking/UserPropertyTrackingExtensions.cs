using System.Globalization;

namespace MoodBoost.Sdk.Tracking
{
    // The typed surface over ITrackingService.SetUserProperty, one method per area of the plan.
    //
    // Lives here rather than on ITrackingService for the same reason as every other *TrackingExtensions file: the
    // interface is the contract EVERY provider has to implement, so it stays at six methods.
    //
    // Numbers are formatted with the invariant culture throughout. A bare ToString() under a locale with its own
    // digit shapes publishes characters the dashboard cannot aggregate, and the device that does it is never the
    // one being tested.
    public static class UserPropertyTrackingExtensions
    {
        public static void SetAttribution(this ITrackingService tracking, in AttributionData data)
        {
            if (tracking == null) return;

            // "organic" rather than "null": the player arriving on their own is an answer, not a missing value.
            tracking.SetUserProperty(UserProperties.UaNetwork,
                data.IsOrganic ? "organic" : TrackingValues.OrNull(data.Network));

            tracking.SetUserProperty(UserProperties.UaCampaign, TrackingValues.OrNull(data.Campaign));
            tracking.SetUserProperty(UserProperties.UaAdGroup, TrackingValues.OrNull(data.AdGroup));
            tracking.SetUserProperty(UserProperties.UaCreative, TrackingValues.OrNull(data.Creative));
        }

        public static void SetAttributionId(this ITrackingService tracking, string installId)
        {
            if (tracking == null) return;

            tracking.SetUserProperty(UserProperties.AttributionId, TrackingValues.OrNull(installId));
        }

        public static void SetConnectionType(this ITrackingService tracking, ConnectionType type)
        {
            if (tracking == null) return;

            tracking.SetUserProperty(UserProperties.ConnectionType, type.ToParam());
        }

        public static void SetActiveDay(this ITrackingService tracking, int dayIndex)
        {
            if (tracking == null) return;

            tracking.SetUserProperty(UserProperties.ActiveDay, Number(dayIndex));
        }

        // Both keys move together: a count without the flag, or the other way round, is a pair that can disagree.
        public static void SetIapUser(this ITrackingService tracking, bool hasPurchased, int purchaseCount)
        {
            if (tracking == null) return;

            tracking.SetUserProperty(UserProperties.IsIapUser, hasPurchased ? "1" : "0");
            tracking.SetUserProperty(UserProperties.IapCount, Number(purchaseCount));
        }

        public static void SetCurrentLevel(this ITrackingService tracking, int level)
        {
            if (tracking == null) return;

            tracking.SetUserProperty(UserProperties.CurrentLevel, Number(level));
        }

        public static void SetCurrentMode(this ITrackingService tracking, string mode)
        {
            if (tracking == null) return;

            tracking.SetUserProperty(UserProperties.CurrentMode, TrackingValues.OrNull(mode));
        }

        public static void SetCurrentEvent(this ITrackingService tracking, string liveOpsEvent)
        {
            if (tracking == null) return;

            tracking.SetUserProperty(UserProperties.CurrentEvent, TrackingValues.OrNull(liveOpsEvent));
        }

        // Both streaks in one call: a win zeroes the lose streak, so publishing half the pair leaves the two keys
        // describing a state the player was never in.
        public static void SetStreaks(this ITrackingService tracking, int winStreak, int loseStreak)
        {
            if (tracking == null) return;

            tracking.SetUserProperty(UserProperties.WinStreak, Number(winStreak));
            tracking.SetUserProperty(UserProperties.LoseStreak, Number(loseStreak));
        }

        public static void SetResourceBalance(this ITrackingService tracking, string resource, int balance)
        {
            if (tracking == null) return;

            var key = UserProperties.Balance(resource);
            if (key == null) return;

            tracking.SetUserProperty(key, Number(balance));
        }

        // The plan's _n properties are counts, so a negative is a bug upstream rather than a value worth reporting.
        static string Number(int value)
            => (value < 0 ? 0 : value).ToString(CultureInfo.InvariantCulture);
    }
}
