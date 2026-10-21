namespace MoodBoost.Sdk.Tracking
{
    // tutorial_action. One event for a whole lesson: "start" at action_index 0, one name per player action counting
    // up from 1, then "finish". action_cate groups those actions (click, use_booster) and differs per game, so it
    // stays a string; start and finish belong to no group and log it as "null". The caller keeps the count.
    public static class TutorialTrackingExtensions
    {
        public const string Start = "start";
        public const string Finish = "finish";

        public static void LogTutorialStart(this ITrackingService tracking)
            => Log(tracking, null, Start, 0);

        public static void LogTutorialAction(this ITrackingService tracking, string category, string name, int index)
            => Log(tracking, category, name, index);

        public static void LogTutorialFinish(this ITrackingService tracking, int index)
            => Log(tracking, null, Finish, index);

        static void Log(ITrackingService tracking, string category, string name, int index)
        {
            if (tracking == null) return;

            tracking.Log(TrackingEvents.TutorialAction, new TrackingParams()
                .With(TrackingKeys.ActionCate, TrackingValues.OrNull(category))
                .With(TrackingKeys.ActionName, TrackingValues.OrNull(name))
                .With(TrackingKeys.ActionIndex, index));
        }
    }
}
