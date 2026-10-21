namespace MoodBoost.Sdk.Tracking
{
    // The startup funnel: how long the app took to become playable, and what each SDK did on the way.
    //
    // Every event carries since_launch because these are queued before tracking is live and replayed afterwards, so
    // the timestamp Firebase stamps on them is not the moment they happened. Read the funnel by since_launch.
    //
    // SdkInitReport is taken straight from MoodBoost.Sdk rather than mirrored into a tracking type: unlike the ads
    // layer, Core is already a reference of this assembly.
    public static class StartupTrackingExtensions
    {
        public static void LogAppLaunch(this ITrackingService tracking, long engineStartupMs)
        {
            if (tracking == null) return;

            // The engine was already up for engineStartupMs before the first line of managed code ran, so this is the
            // part of the launch the funnel cannot see from the inside.
            tracking.Log(TrackingEvents.AppLaunch, new TrackingParams()
                .With(TrackingKeys.SinceLaunch, engineStartupMs));
        }

        public static void LogSplashLoaded(this ITrackingService tracking, long sinceLaunchMs, bool wasBackgrounded)
        {
            if (tracking == null) return;

            tracking.Log(TrackingEvents.SplashLoaded, new TrackingParams()
                .With(TrackingKeys.SinceLaunch, sinceLaunchMs)
                .With(TrackingKeys.WasBackgrounded, wasBackgrounded));
        }

        public static void LogSdkInit(
            this ITrackingService tracking, in SdkInitReport report, long sinceLaunchMs, bool wasBackgrounded)
        {
            if (tracking == null || string.IsNullOrEmpty(report.Id)) return;
            if (report.Result == SdkInitResult.Skipped) return;
            if (report.IsModule && (report.Id == SdkInitIds.Tracking || report.Id == SdkInitIds.UserProperties)) return;
            if (!report.IsModule && report.Id == SdkInitIds.LogTracking) return;
            if (!report.IsModule && report.Id == SdkInitIds.LogNotifications) return;

            tracking.Log(TrackingEvents.InitPrefix + report.Id, new TrackingParams()
                .With(TrackingKeys.Sdk, report.Id)
                .With(TrackingKeys.InitScope, TrackingValues.InitScope(report.IsModule))
                .With(TrackingKeys.DurationInit, report.ElapsedMs)
                .With(TrackingKeys.Result, report.Result.ToParam())
                .With(TrackingKeys.Error, TrackingValues.OrNull(TrackingValues.Truncate(report.Error)))
                .With(TrackingKeys.SinceLaunch, sinceLaunchMs)
                .With(TrackingKeys.WasBackgrounded, wasBackgrounded));
        }

        // sdkReadyMs is when the last module finished; sinceLaunchMs is when the player could actually see Home. The
        // later of the two is when this event fires, and wait_by says which one it was.
        //
        // result=timeout means Boot stopped waiting and went to Home anyway, so sdkReadyMs is when the wait was cut
        // short rather than when the SDK finished - the SDK may still have been coming up.
        public static void LogLoadingDone(
            this ITrackingService tracking, long sinceLaunchMs, long sdkReadyMs, bool waitedOnSdk, bool sdkTimedOut,
            bool wasBackgrounded)
        {
            if (tracking == null) return;

            tracking.Log(TrackingEvents.LoadingDone, new TrackingParams()
                .With(TrackingKeys.SinceLaunch, sinceLaunchMs)
                .With(TrackingKeys.DurationInit, sdkReadyMs)
                .With(TrackingKeys.WaitBy, waitedOnSdk ? TrackingValues.WaitBySdk : TrackingValues.WaitByUi)
                .With(TrackingKeys.Result, sdkTimedOut ? TrackingValues.Timeout : TrackingValues.Success)
                .With(TrackingKeys.WasBackgrounded, wasBackgrounded));
        }
    }
}
