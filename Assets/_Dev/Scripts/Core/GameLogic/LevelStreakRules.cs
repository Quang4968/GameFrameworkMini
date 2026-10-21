using Game.Core.Domain;

namespace Game.Core.GameLogic
{
    // Pure transitions of the win/lose streak. No clock, no disk - the whole point is that the revive path below can
    // be tested without a scene.
    public static class LevelStreakRules
    {
        public static LevelStreak OnWin(LevelStreak current) => new LevelStreak(current.Win + 1, 0);

        public static LevelStreak OnLose(LevelStreak current) => new LevelStreak(0, current.Lose + 1);
    }
}
