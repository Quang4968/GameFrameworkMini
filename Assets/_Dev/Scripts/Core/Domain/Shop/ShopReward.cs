namespace Game.Core.Domain.Shop
{
    // One line of a pack as the catalog DECLARES it - the promise, before any cap is applied.
    public readonly struct ShopReward
    {
        public readonly E_ShopRewardKind Kind;

        // Only read when Kind is Booster.
        public readonly BoosterType Booster;

        // Minutes for UnlimitedLives.
        public readonly int Amount;

        public ShopReward(E_ShopRewardKind kind, int amount, BoosterType booster = default)
        {
            Kind = kind;
            Booster = booster;
            Amount = amount;
        }
    }
}
