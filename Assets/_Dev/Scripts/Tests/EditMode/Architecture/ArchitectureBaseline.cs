using System.Collections.Generic;

namespace Game.Core.Tests.Architecture
{
    // Violations that predate the architecture tests. Each number is a ceiling: a file may lose hits but never gain
    // one, and a paid-off entry must be lowered or removed (BaselineHasNoPaidOffDebt). New code never gets an entry.
    public static class ArchitectureBaseline
    {
        public static readonly Dictionary<string, Dictionary<string, int>> Violations =
            new Dictionary<string, Dictionary<string, int>>
            {
                ["coroutine"] = new Dictionary<string, int>
                {
                    ["Game/Splash/SplashBootstrap.cs"] = 4,
                },
                ["find-object"] = new Dictionary<string, int>
                {
                    ["Game/Views/Diagnostics/DebugConsole.cs"] = 2,
                    ["Game/Views/Diagnostics/FpsOverlay.cs"] = 1,
                    ["Game/Views/Gameplay/GameplayHudPresenter.cs"] = 1,
                },
                ["raw-instantiate"] = new Dictionary<string, int>
                {
                    ["Game/SceneFlow/SceneTransitionProvider.cs"] = 1,
                    ["Game/Services/CoinFlightService.cs"] = 2,
                },
                ["system-clock"] = new Dictionary<string, int>
                {
                    ["Game/Services/PlayerPrefsActiveDayService.cs"] = 1,
                },
                ["interface-prefix"] = new Dictionary<string, int>
                {
                    ["Core/Services/I_ActiveDayService.cs"] = 1,
                    ["Core/Services/I_InterstitialAdService.cs"] = 1,
                    ["Core/Services/I_LevelPlayTimeService.cs"] = 1,
                    ["Core/Services/I_LevelSessionService.cs"] = 1,
                    ["Core/Services/I_LevelStreakService.cs"] = 1,
                    ["Core/Services/I_ProfileService.cs"] = 1,
                    ["Core/Services/I_TutorialStateService.cs"] = 1,
                    ["Game/Services/I_CoinFlightService.cs"] = 1,
                    ["Game/Services/I_CoinFlightTarget.cs"] = 1,
                },
                ["enum-prefix"] = new Dictionary<string, int>
                {
                    ["Core/Domain/E_LevelDifficulty.cs"] = 1,
                    ["Core/Domain/E_LevelPlayType.cs"] = 1,
                    ["Core/Domain/E_TutorialId.cs"] = 1,
                    ["Core/Domain/E_TutorialStage.cs"] = 1,
                    ["Core/Domain/Shop/E_ShopRewardKind.cs"] = 1,
                    ["Game/Views/Tutorial/E_HandApproach.cs"] = 1,
                    ["Game/Views/Tutorial/E_TutorialAction.cs"] = 1,
                },
                ["underscore-field"] = new Dictionary<string, int>
                {
                    ["Core/GameLogic/LevelPlayStopwatch.cs"] = 4,
                    ["Game/Animation/BoosterIconFloat.cs"] = 6,
                    ["Game/Animation/FlyingCoin.cs"] = 1,
                    ["Game/Data/AvatarCatalogSO.cs"] = 1,
                    ["Game/Data/CoinFlightSettingsSO.cs"] = 18,
                    ["Game/Services/GameRemoteConfig.cs"] = 1,
                    ["Game/Services/LevelPlayTimeService.cs"] = 2,
                    ["Game/Services/PlayerPrefsProfileService.cs"] = 1,
                    ["Game/Services/WinRewardSettings.cs"] = 4,
                    ["Game/Views/Diagnostics/DebugConsole.cs"] = 15,
                    ["Game/Views/Diagnostics/FpsOverlay.cs"] = 6,
                    ["Game/Views/Gameplay/LevelPlayTimePresenter.cs"] = 9,
                    ["Game/Views/Gameplay/LevelPopupPause.cs"] = 3,
                },
            };

        // Line count of each file already over ArchitectureConfig.MaxFileLines; it may shrink but not grow.
        public static readonly Dictionary<string, int> FileLines = new Dictionary<string, int>
        {
            ["Editor/SOManagerWindow.cs"] = 688,
            ["Game/Animation/ButtonPressEffect.cs"] = 324,
        };

        public static int Allowed(string ruleId, string path) =>
            Violations.TryGetValue(ruleId, out var files) && files.TryGetValue(path, out var count) ? count : 0;
    }
}
