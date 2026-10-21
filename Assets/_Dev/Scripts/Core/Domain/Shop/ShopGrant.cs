namespace Game.Core.Domain.Shop
{
    // One line of a pack as it ACTUALLY LANDED.
    //
    // Deliberately separate from ShopReward: lives clamp at the cap, so the number handed over can
    // be smaller than the number promised. Tracking has to report this one, or resource_balance
    // stops matching the counter the player is looking at.
    public readonly struct ShopGrant
    {
        public readonly E_ShopRewardKind Kind;

        // Only meaningful when Kind is Booster.
        public readonly BoosterType Booster;

        public readonly int Amount;

        // What the player holds after this line; minutes of window left for UnlimitedLives.
        public readonly int Balance;

        public ShopGrant(E_ShopRewardKind kind, int amount, int balance, BoosterType booster = default)
        {
            Kind = kind;
            Booster = booster;
            Amount = amount;
            Balance = balance;
        }
    }
}
