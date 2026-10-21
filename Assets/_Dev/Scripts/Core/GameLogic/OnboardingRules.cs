namespace Game.Core.GameLogic
{
    // The first ten levels are one closed run: the app opens straight into gameplay, there is no
    // way out to Home, and nothing is charged for playing. Claiming the win on the last one is the
    // single hand-off to Home, and from level 11 the normal Home -> Play loop takes over.
    //
    // Every question below is asked about LevelShow (counted from 0), never about LevelLoad — the
    // run is about what the player has seen, not about which file was loaded.
    public static class OnboardingRules
    {
        // Last onboarding level as the player reads it (the first level is 1).
        public const int LastLevelNumber = 10;

        const int LastLevelIndex = LastLevelNumber - 1;

        public static bool IsOnboarding(int levelShow) => levelShow <= LastLevelIndex;

        // Boot hands off to gameplay instead of Home: Home has nothing to offer during the run.
        public static bool SkipsHome(int levelShow) => IsOnboarding(levelShow);

        // False hides every Home button and turns "leave the level" into "restart the level".
        public static bool AllowsHome(int levelShow) => !IsOnboarding(levelShow);

        // The very first level is bare: no level number, no settings gear, nothing above the board
        // but the level itself. One level only — by level 2 the player has seen a whole round.
        public static bool ShowsTopHud(int levelShow) => levelShow > 0;

        // No rewarded ad is offered to save a lost run: the first ten levels are the player's
        // first look at the game, and the coin button alone already teaches what a revive is.
        public static bool OffersRewardedRevive(int levelShow) => !IsOnboarding(levelShow);

        // Nothing is spent on entry, so nothing is refunded on a win and losing costs nothing.
        public static bool ChargesLife(int levelShow) => !IsOnboarding(levelShow);

        // The one win that ends the run, and it happens exactly once because LevelShow never
        // repeats a level before the end-game loop.
        public static bool ReturnsHomeAfterWin(int levelShow) => levelShow == LastLevelIndex;
    }
}
