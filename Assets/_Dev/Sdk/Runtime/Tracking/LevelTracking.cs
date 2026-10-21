namespace MoodBoost.Sdk.Tracking
{
    // A service rather than extension methods on ITrackingService: the level is state the SDK owns, and an extension
    // method has nowhere to read IPlayerLevel from.
    public sealed class LevelTracking : ILevelTracking
    {
        readonly ITrackingService tracking;
        readonly IPlayerLevel playerLevel;

        // Read once at level_start and reused until the play ends, so a level that advances before the play's last
        // event cannot split one play across two level numbers.
        int playLevel;
        bool playOpen;

        public LevelTracking(ITrackingService tracking, IPlayerLevel playerLevel)
        {
            this.tracking = tracking;
            this.playerLevel = playerLevel;
        }

        public void LogLevelStart(in LevelTrackingInfo info, long durationTotalStart)
        {
            playLevel = ReadLevel();
            playOpen = true;

            tracking.Log(TrackingEvents.LevelStart, Identity(playLevel, info)
                .With(TrackingKeys.DurationTotalStart, durationTotalStart));
        }

        public void LogLevelEnd(in LevelTrackingInfo info, in LevelPlayStats stats, LevelResult result, string loseBy = null)
        {
            var level = ClosePlay();

            tracking.Log(TrackingEvents.LevelEnd, WithStats(Identity(level, info), stats)
                .With(TrackingKeys.Result, result.ToParam())
                .With(TrackingKeys.LoseBy, TrackingValues.OrNull(loseBy)));
        }

        public void LogLevelExit(in LevelTrackingInfo info, in LevelPlayStats stats, int exitIndex)
        {
            var level = ClosePlay();

            tracking.Log(TrackingEvents.LevelExit, WithStats(
                Identity(level, info).With(TrackingKeys.ExitIndex, exitIndex), stats));
        }

        public void LogLevelReopen(in LevelTrackingInfo info, long durationTotalStart)
        {
            tracking.Log(TrackingEvents.LevelReopen, new TrackingParams()
                .With(TrackingKeys.Level, PlayLevel())
                .With(TrackingKeys.LoopBy, info.LoopBy)
                .With(TrackingKeys.PlayIndex, info.PlayIndex)
                .With(TrackingKeys.LoseIndex, info.LoseIndex)
                .With(TrackingKeys.DurationTotalStart, durationTotalStart)
                .With(TrackingKeys.Mode, TrackingValues.OrNull(info.Mode)));
        }

        int PlayLevel() => playOpen ? playLevel : ReadLevel();

        int ClosePlay()
        {
            var level = PlayLevel();
            playOpen = false;
            return level;
        }

        // 0 when the game never set a provider - PlayerLevel already warns about that once.
        int ReadLevel()
        {
            playerLevel.TryGetCurrent(out var level);
            return level;
        }

        static TrackingParams Identity(int level, in LevelTrackingInfo info)
        {
            return new TrackingParams()
                .With(TrackingKeys.Level, level)
                .With(TrackingKeys.LoopBy, info.LoopBy)
                .With(TrackingKeys.PlayType, info.PlayType.ToParam())
                .With(TrackingKeys.PlayIndex, info.PlayIndex)
                .With(TrackingKeys.LoseIndex, info.LoseIndex)
                .With(TrackingKeys.Mode, TrackingValues.OrNull(info.Mode));
        }

        static TrackingParams WithStats(TrackingParams parameters, in LevelPlayStats stats)
        {
            return parameters
                .With(TrackingKeys.DurationTotalStart, stats.DurationTotalStart)
                .With(TrackingKeys.DurationTotalEnd, stats.DurationTotalEnd)
                .With(TrackingKeys.DurationRemain, stats.DurationRemain)
                .With(TrackingKeys.DurationPlay, stats.DurationPlay)
                .With(TrackingKeys.ItemsTotal, stats.ItemsTotal)
                .With(TrackingKeys.ItemsCleared, stats.ItemsCleared)
                .With(TrackingKeys.ActionSeq, TrackingValues.Truncate(
                    string.IsNullOrEmpty(stats.ActionSeq) ? TrackingValues.NoAction : stats.ActionSeq));
        }
    }
}
