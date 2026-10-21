using MoodBoost.Sdk;

namespace Game.Game.Services
{
    // Every remote config parameter the framework reads, with its default and the group it shows
    // under in the SDK's config template. A game keeps its own keys in its own static class; the
    // helpers at the bottom build the per-item keys whose names depend on game data.
    public static class RemoteConfigKeys
    {
        const string Level = "Level";
        const string Onboarding = "Onboarding";
        const string Economy = "Economy";
        const string Ads = "Ads";
        const string Operations = "Operations";

        public static readonly ConfigKey<string> LevelOrder = ConfigKey.Json(
            "level_order", "[]",
            "One-based ids of playable levels, in the order players see them. Empty means every level in order.", Level);

        public static readonly ConfigKey<long> LevelLoopStart = ConfigKey.Long(
            "level_loop_start", 1L, "First displayed level in the loop, counted from 1.", Level);

        public static readonly ConfigKey<long> LevelLoopEnd = ConfigKey.Long(
            "level_loop_end", 50L, "Last displayed level before looping, counted from 1.", Level);

        public static readonly ConfigKey<long> UnlockGiftAmount = ConfigKey.Long(
            "onboard_unlock_gift_amount", 2L, "Free boosters granted when a booster unlocks.", Onboarding);

        public static readonly ConfigKey<double> HintDelaySeconds = ConfigKey.Double(
            "onboard_hint_delay_seconds", 3d, "Seconds of inactivity before the tutorial hand appears.", Onboarding);

        public static readonly ConfigKey<long> MaxLives = ConfigKey.Long(
            "eco_lives_max", 5L, "Maximum number of lives.", Economy);

        public static readonly ConfigKey<long> LifeRefillMinutes = ConfigKey.Long(
            "eco_lives_refill_minutes", 30L, "Minutes required to regenerate one life.", Economy);

        public static readonly ConfigKey<long> LifeRefillCoinCost = ConfigKey.Long(
            "eco_lives_refill_coin_cost", 900L, "Coin price of a full life refill.", Economy);

        public static readonly ConfigKey<long> LivesPerAd = ConfigKey.Long(
            "eco_lives_per_ad", 1L, "Lives granted by one rewarded ad.", Economy);

        public static readonly ConfigKey<long> CoinsPerWin = ConfigKey.Long(
            "eco_coins_per_win", 30L, "Base coins granted for winning a level.", Economy);

        public static readonly ConfigKey<string> DifficultyCoinBonus = ConfigKey.Json(
            "eco_coins_bonus_hard", "{\"hard\":0,\"very_hard\":0}",
            "Additional coins for Hard and Very Hard levels.", Economy);

        public static readonly ConfigKey<long> WinRewardMultiplier = ConfigKey.Long(
            "eco_win_rv_multiplier", 2L, "Coin multiplier after the win rewarded ad.", Economy);

        public static readonly ConfigKey<long> StartingCoins = ConfigKey.Long(
            "eco_starting_coins", 0L, "Coins on the first launch only.", Economy);

        public static readonly ConfigKey<long> ReviveCoinCost = ConfigKey.Long(
            "eco_revive_coin_cost", 900L, "Base coin price of a level revive.", Economy);

        public static readonly ConfigKey<long> ReviveCostStep = ConfigKey.Long(
            "eco_revive_cost_step", 0L, "Additional coin price for each later revive in the same level.", Economy);

        public static readonly ConfigKey<string> BoosterCoinCost = ConfigKey.Json(
            "eco_booster_coin_cost", "{\"others\":250}",
            "Coin price of one booster lot, keyed by lower-case booster name; \"others\" covers the rest.", Economy);

        public static readonly ConfigKey<long> BoosterAmountPerPurchase = ConfigKey.Long(
            "eco_booster_amount_per_purchase", 3L, "Boosters granted by one coin purchase.", Economy);

        public static readonly ConfigKey<bool> SkipInterstitialAfterLoss = ConfigKey.Bool(
            "ads_interstitial_skip_after_loss", false, "Skips an interstitial immediately after a loss.", Ads);

        public static readonly ConfigKey<bool> ExtraLifeRewardedEnabled = ConfigKey.Bool(
            "rv_extra_life_enabled", true, "Shows the rewarded offer that grants lives.", Ads);

        public static readonly ConfigKey<bool> WinRewardedEnabled = ConfigKey.Bool(
            "rv_level_complete_enabled", true, "Shows the rewarded multiplier on the win popup.", Ads);

        public static readonly ConfigKey<bool> ReviveRewardedEnabled = ConfigKey.Bool(
            "rv_revive_enabled", true, "Shows rewarded revive on the lose popup.", Ads);

        public static readonly ConfigKey<string> ConfigVersion = ConfigKey.String(
            "ops_config_version", "default", "Version label attached to analytics events.", Operations);

        // Displayed level that unlocks a lesson: onboard_unlock_level_<lesson>, lower case.
        public static ConfigKey<long> TutorialUnlockLevel(string lesson, long fallback)
            => ConfigKey.Long("onboard_unlock_level_" + lesson.ToLowerInvariant(), fallback,
                "Displayed level that unlocks " + lesson + ".", Onboarding);

        // Displayed level that introduces a feature: onboard_intro_level_<feature>, lower case.
        public static ConfigKey<long> FeatureIntroLevel(string feature, long fallback)
            => ConfigKey.Long("onboard_intro_level_" + feature.ToLowerInvariant(), fallback,
                "Displayed level introducing " + feature + ".", Onboarding);
    }
}
