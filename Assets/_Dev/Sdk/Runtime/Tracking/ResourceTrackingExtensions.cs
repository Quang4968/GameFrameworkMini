using System.Collections.Generic;

namespace MoodBoost.Sdk.Tracking
{
    // resource_earn and resource_spend.
    //
    // resource_amount and resource_balance are logged as STRINGS even for a single resource. The plan requires that
    // once any log uses the comma separated form, every value of that parameter is a string - so locking it now
    // avoids a schema break the first time two resources move together.
    public static class ResourceTrackingExtensions
    {
        public static void LogResourceEarn(
            this ITrackingService tracking,
            in ResourceChange change,
            string placement,
            EarnReason reason,
            string placementDetail = null)
        {
            if (tracking == null) return;

            tracking.Log(TrackingEvents.ResourceEarn,
                Single(change, placement, placementDetail).With(TrackingKeys.Reason, reason.ToParam()));
        }

        public static void LogResourceEarn(
            this ITrackingService tracking,
            IReadOnlyList<ResourceChange> changes,
            string placement,
            EarnReason reason,
            string placementDetail = null)
        {
            if (tracking == null || changes == null || changes.Count == 0) return;

            tracking.Log(TrackingEvents.ResourceEarn,
                Many(changes, placement, placementDetail).With(TrackingKeys.Reason, reason.ToParam()));
        }

        public static void LogResourceSpend(
            this ITrackingService tracking,
            in ResourceChange change,
            string placement,
            SpendReason reason,
            string placementDetail = null)
        {
            if (tracking == null) return;

            tracking.Log(TrackingEvents.ResourceSpend,
                Single(change, placement, placementDetail).With(TrackingKeys.Reason, reason.ToParam()));
        }

        public static void LogResourceSpend(
            this ITrackingService tracking,
            IReadOnlyList<ResourceChange> changes,
            string placement,
            SpendReason reason,
            string placementDetail = null)
        {
            if (tracking == null || changes == null || changes.Count == 0) return;

            tracking.Log(TrackingEvents.ResourceSpend,
                Many(changes, placement, placementDetail).With(TrackingKeys.Reason, reason.ToParam()));
        }

        static TrackingParams Single(in ResourceChange change, string placement, string placementDetail)
        {
            return new TrackingParams()
                .With(TrackingKeys.ResourceType, change.Type.ToParam())
                .With(TrackingKeys.ResourceName, TrackingValues.OrNull(change.Name))
                .With(TrackingKeys.ResourceAmount, change.Amount.ToString())
                .With(TrackingKeys.ResourceBalance, change.Balance.ToString())
                .With(TrackingKeys.Placement, TrackingValues.OrNull(placement))
                .With(TrackingKeys.PlacementDetail, TrackingValues.OrNull(placementDetail));
        }

        // The four parallel lists must stay index aligned, so they are built in one pass.
        static TrackingParams Many(
            IReadOnlyList<ResourceChange> changes, string placement, string placementDetail)
        {
            var types = new string[changes.Count];
            var names = new string[changes.Count];
            var amounts = new string[changes.Count];
            var balances = new string[changes.Count];

            for (var i = 0; i < changes.Count; i++)
            {
                types[i] = changes[i].Type.ToParam();
                names[i] = changes[i].Name;
                amounts[i] = changes[i].Amount.ToString();
                balances[i] = changes[i].Balance.ToString();
            }

            return new TrackingParams()
                .With(TrackingKeys.ResourceType, TrackingValues.Join(types))
                .With(TrackingKeys.ResourceName, TrackingValues.Join(names))
                .With(TrackingKeys.ResourceAmount, TrackingValues.Join(amounts))
                .With(TrackingKeys.ResourceBalance, TrackingValues.Join(balances))
                .With(TrackingKeys.Placement, TrackingValues.OrNull(placement))
                .With(TrackingKeys.PlacementDetail, TrackingValues.OrNull(placementDetail));
        }
    }
}
