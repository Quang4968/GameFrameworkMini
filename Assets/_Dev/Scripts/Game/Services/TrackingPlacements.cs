namespace Game.Game.Services
{
    // The plan's placement parameter: WHERE the player is when the resource changes. It is a view-layer
    // concept — a use case does not know whether it was called from the shop or from a gate — so it lives in Game.
    public static class TrackingPlacements
    {
        // Inside a level: using a booster.
        public const string Ingame = "ingame";

        public const string LevelStart = "level_start";
        public const string LevelWin = "level_win";

        // Out-of-lives popup: trade coins for a life, or watch an ad for a life.
        public const string OutOfLives = "out_of_lives";

        public const string BuyBooster = "buy_booster";

        // The tutorial's booster unlock popup.
        public const string BoosterUnlock = "booster_unlock";

        // Reviving a lost level with coins.
        public const string Revive = "revive";

        // The shop reached on its own: the Home tab. Also the placement every shop grant is logged
        // under, since delivery happens long after the popup that opened it is gone.
        public const string Shop = "shop";

        // The "+" on the coin chip in the Home top bar.
        public const string TopBar = "top_bar";

        // The daily login popup on Home.
        public const string DailyReward = "daily_reward";
    }
}
