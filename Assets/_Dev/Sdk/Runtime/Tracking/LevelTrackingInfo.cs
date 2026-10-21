namespace MoodBoost.Sdk.Tracking
{
    // The parameters every level event repeats, except the level number, which LevelTracking reads from IPlayerLevel.
    // Built once when the level starts and handed to level_start, level_end, level_exit and level_reopen unchanged, so
    // the four events cannot disagree about which play they describe.
    public readonly struct LevelTrackingInfo
    {
        // The level whose content is being played, when a high level replays an older level's content. 0 means the
        // level is playing its own content.
        public readonly int LoopBy;

        public readonly PlayType PlayType;

        // How many times the player has played this level, counting this one. Starts at 1.
        public readonly int PlayIndex;

        // How many times the player has lost this level BEFORE this play. 0 on the first play.
        public readonly int LoseIndex;

        public readonly string Mode;

        public LevelTrackingInfo(
            PlayType playType,
            int playIndex,
            int loseIndex,
            int loopBy = 0,
            string mode = null)
        {
            PlayType = playType;
            PlayIndex = playIndex;
            LoseIndex = loseIndex;
            LoopBy = loopBy;
            Mode = mode;
        }
    }
}
