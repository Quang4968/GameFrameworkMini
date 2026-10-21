namespace Game.Core.Domain
{
    // Difficulty of a level, as the level data declares it.
    // The values ARE the dump's ints, so reading one stays a plain cast with no lookup table.
    public enum E_LevelDifficulty
    {
        Normal = 0,
        Hard = 1,
        VeryHard = 2,
    }
}
