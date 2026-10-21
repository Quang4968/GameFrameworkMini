namespace MoodBoost.Sdk.Tracking
{
    // The five ad events of the tracking plan.
    //
    // is_load and is_show are numbers, not booleans: the plan defines them as 0 or 1.
    public static class AdTrackingExtensions
    {
        public static void LogAdRequest(
            this ITrackingService tracking,
            in AdTrackingInfo info,
            bool loaded,
            long loadTimeMs,
            AdErrorType errorType = AdErrorType.None,
            string errorCode = null)
        {
            if (tracking == null) return;

            tracking.Log(TrackingEvents.AdRequest, Identity(info)
                .With(TrackingKeys.IsLoad, loaded)
                .With(TrackingKeys.ErrorType, errorType.ToParam())
                .With(TrackingKeys.ErrorCode, TrackingValues.OrNull(errorCode))
                .With(TrackingKeys.LoadTime, loadTimeMs));
        }

        // currency is not in the tracking plan, but Firebase needs it beside value to read this event as ad revenue -
        // and this is the only ad_impression the SDK sends, so it has to satisfy both readers.
        public static void LogAdImpression(
            this ITrackingService tracking,
            in AdTrackingInfo info,
            double value,
            bool shown = true,
            AdErrorType errorType = AdErrorType.None,
            string errorCode = null,
            string currency = null)
        {
            if (tracking == null) return;

            var parameters = Identity(info)
                .With(TrackingKeys.Value, value)
                .With(TrackingKeys.IsShow, shown)
                .With(TrackingKeys.ErrorType, errorType.ToParam())
                .With(TrackingKeys.ErrorCode, TrackingValues.OrNull(errorCode));

            if (!string.IsNullOrEmpty(currency)) parameters = parameters.With(TrackingKeys.Currency, currency);

            tracking.Log(TrackingEvents.AdImpression, parameters);
        }

        // Raised by the GAME, when it puts its own "watch an ad?" UI on screen - the ad SDK never sees that moment.
        public static void LogAdOfferShown(
            this ITrackingService tracking, TrackedAdFormat format, string placement)
        {
            if (tracking == null) return;

            tracking.Log(TrackingEvents.AdOfferShown, new TrackingParams()
                .With(TrackingKeys.AdFormat, format.ToParam())
                .With(TrackingKeys.Placement, TrackingValues.OrNull(placement)));
        }

        public static void LogAdClick(this ITrackingService tracking, in AdTrackingInfo info)
        {
            if (tracking == null) return;

            tracking.Log(TrackingEvents.AdClick, Identity(info));
        }

        public static void LogAdComplete(
            this ITrackingService tracking, in AdTrackingInfo info, AdEndType endType, long durationAdMs)
        {
            if (tracking == null) return;

            tracking.Log(TrackingEvents.AdComplete, Identity(info)
                .With(TrackingKeys.EndType, endType.ToParam())
                .With(TrackingKeys.DurationAd, durationAdMs));
        }

        static TrackingParams Identity(in AdTrackingInfo info)
        {
            return new TrackingParams()
                .With(TrackingKeys.AdFormat, info.Format.ToParam())
                .With(TrackingKeys.AdPlatform, TrackingValues.OrNull(info.Platform))
                .With(TrackingKeys.AdNetwork, TrackingValues.OrNull(info.Network))
                .With(TrackingKeys.AdUnitId, TrackingValues.OrNull(info.AdUnitId))
                .With(TrackingKeys.Placement, TrackingValues.OrNull(info.Placement));
        }
    }
}
