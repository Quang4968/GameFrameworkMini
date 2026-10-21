namespace MoodBoost.Sdk.Tracking
{
    // What the player actually did with the level, shared by level_end and level_exit.
    //
    // Every duration is in MILLISECONDS and excludes time spent watching ads.
    public readonly struct LevelPlayStats
    {
        // Time the level allowed at the start, and after time boosters. Equal when the game cannot extend the clock.
        public readonly long DurationTotalStart;
        public readonly long DurationTotalEnd;

        public readonly long DurationRemain;
        public readonly long DurationPlay;

        public readonly int ItemsTotal;
        public readonly int ItemsCleared;

        // Ordered action aliases, comma separated without spaces. Build it with TrackingValues.Sequence.
        public readonly string ActionSeq;

        public LevelPlayStats(
            long durationTotalStart,
            long durationTotalEnd,
            long durationRemain,
            long durationPlay,
            int itemsTotal,
            int itemsCleared,
            string actionSeq)
        {
            DurationTotalStart = durationTotalStart;
            DurationTotalEnd = durationTotalEnd;
            DurationRemain = durationRemain;
            DurationPlay = durationPlay;
            ItemsTotal = itemsTotal;
            ItemsCleared = itemsCleared;
            ActionSeq = actionSeq;
        }

        // Derives duration_play from the clock, which the plan names as the most accurate way to compute it.
        public LevelPlayStats(
            long durationTotalStart,
            long durationTotalEnd,
            long durationRemain,
            int itemsTotal,
            int itemsCleared,
            string actionSeq)
            : this(
                durationTotalStart,
                durationTotalEnd,
                durationRemain,
                durationTotalEnd - durationRemain,
                itemsTotal,
                itemsCleared,
                actionSeq)
        {
        }
    }
}
