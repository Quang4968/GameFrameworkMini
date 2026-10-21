namespace MoodBoost.Sdk.Ads
{
    // The outcome of one full-screen ad display call.
    //
    // The most important boundary in the whole system: only Completed may grant a reward. Every other value means
    // "nothing happened" in the sense the player experienced, so the call site needs one comparison rather than a
    // branch per error kind.
    public enum AdResult
    {
        // Watched to the end and confirmed by the ad network. Rewarded: grant the reward. Interstitial: it was shown.
        Completed = 0,

        // The player closed it partway. No reward.
        Skipped = 1,

        // No ad had finished loading at the time of the call.
        NotReady = 2,

        // A game rule blocked it (cooldown not elapsed, minimum level not reached, a rewarded ad just watched).
        Blocked = 3,

        // The remove-ads pack was bought, or the ads module is off via define symbol.
        Disabled = 4,

        // The ad network reported an error while showing.
        Failed = 5,
    }
}
