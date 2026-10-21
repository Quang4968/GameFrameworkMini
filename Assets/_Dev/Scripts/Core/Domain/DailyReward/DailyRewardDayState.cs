namespace Game.Core.Domain.DailyReward
{
    // How a day card is drawn. Computed on every open, never stored.
    public enum DailyRewardDayState
    {
        Claimed,
        Claimable,
        Locked
    }
}
