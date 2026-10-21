namespace Game.Core.Domain
{
    // One row of the schedule. A DECLARATION only, like FeatureIntroduction: it does not enable the
    // booster — BoosterUnlockRules reads it to answer whether a button is still locked.
    public readonly struct TutorialDefinition
    {
        public E_TutorialId Id { get; }

        // Level index counted from 0, same axis as ILevelProgressService.LevelShow. The player
        // reaches this level with the booster already unlocked.
        public int LevelIndex { get; }

        public BoosterType Booster { get; }

        // Free boosters handed over on claim.
        public int RewardAmount { get; }

        // False for a lesson that only teaches: Booster then means nothing, no button is gated on
        // this level, and there is no gift popup to claim. Defaults to true so a row that says
        // nothing about it still behaves like the five booster lessons.
        public bool UnlocksBooster { get; }

        public TutorialDefinition(E_TutorialId id, int levelIndex, BoosterType booster, int rewardAmount,
            bool unlocksBooster = true)
        {
            Id = id;
            LevelIndex = levelIndex;
            Booster = booster;
            RewardAmount = rewardAmount;
            UnlocksBooster = unlocksBooster;
        }
    }
}
