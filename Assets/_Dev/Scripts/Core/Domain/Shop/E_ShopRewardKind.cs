namespace Game.Core.Domain.Shop
{
    // What one line of a shop pack hands over.
    //
    // ShopCatalog stores this by NUMBER, so new values are APPENDED AT THE END only - inserting in
    // the middle shifts every reward already declared in the asset onto a different kind, and a
    // coin pack quietly starts handing out boosters.
    public enum E_ShopRewardKind
    {
        Coins = 0,

        // Clamped at MaxLives like every other life grant, so a pack bought on a full bar pays out
        // less than it promises. Price accordingly.
        Lives = 1,

        Booster = 2,

        // 3 was RemoveAds (an IAP flag, gone with IAP). Never reuse it: stored rewards keep the number.

        // Amount is MINUTES of unlimited lives, stacked onto a window already running. A quantity,
        // not a flag, so it never makes a pack buy-once.
        UnlimitedLives = 4,
    }
}
