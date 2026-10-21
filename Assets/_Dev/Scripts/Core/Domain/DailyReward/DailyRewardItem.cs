namespace Game.Core.Domain.DailyReward
{
    // One line of a day's reward. The id is the game's own vocabulary ("coins", "shuffle"): only the
    // IDailyRewardGranter knows what it pays out, which keeps the cycle rules game-agnostic.
    public readonly struct DailyRewardItem
    {
        public readonly string Id;

        // Unit depends on the id (minutes for unlimited lives, a count for everything else).
        public readonly int Amount;

        public DailyRewardItem(string id, int amount)
        {
            Id = id;
            Amount = amount;
        }
    }
}
