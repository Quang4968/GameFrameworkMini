namespace MoodBoost.Sdk.Tracking
{
    // The four level events of the tracking plan.
    //
    // The level number is not a parameter: it comes from IPlayerLevel, the same source the level-gated ad rules read,
    // so tracking and ads can never disagree about which level the player is on.
    public interface ILevelTracking
    {
        void LogLevelStart(in LevelTrackingInfo info, long durationTotalStart);

        void LogLevelEnd(in LevelTrackingInfo info, in LevelPlayStats stats, LevelResult result, string loseBy = null);

        void LogLevelExit(in LevelTrackingInfo info, in LevelPlayStats stats, int exitIndex);

        // The plan leaves play_type off level_reopen: the play it reopens already reported how it was entered.
        void LogLevelReopen(in LevelTrackingInfo info, long durationTotalStart);
    }
}
