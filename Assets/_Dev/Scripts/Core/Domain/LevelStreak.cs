namespace Game.Core.Domain
{
    // The player's current run of wins or losses. At most one side is ever non-zero: winning a level ends any losing
    // run and the other way round, so a state with both set describes something that never happened.
    public readonly struct LevelStreak
    {
        public readonly int Win;
        public readonly int Lose;

        public LevelStreak(int win, int lose)
        {
            Win = win < 0 ? 0 : win;
            Lose = lose < 0 ? 0 : lose;
        }

        public static LevelStreak Zero => new LevelStreak(0, 0);
    }
}
