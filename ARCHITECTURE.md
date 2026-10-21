# Game Framework - structure of the base

The shared part of the stack (two layers, DI per scene, scene transitions, pooling, where a new
file goes) is in [docs/stack/architecture.md](docs/stack/architecture.md); the recipe for a new
system in [docs/stack/adding-a-system.md](docs/stack/adding-a-system.md). This file holds only what
the base itself wires: the scenes, the installer each scene calls, and the seam a game plugs into.

## Summary

- Four scenes in order `Splash` → `Boot` → `HomeScene` → `MainGame`; Boot **skips Home** for the
  onboarding levels (`OnboardingRules.SkipsHome`).
- The base has no level data. `ILevelCatalog` (count + difficulty) is all the framework asks for;
  `LevelCatalogSO` is the stand-in asset, and every scene must point at the **same** one or the
  Play button shows a different number than MainGame loads.
- The game talks to the framework through **`GameplayBridge`** (`Game/Services/`), registered in
  `MainGameLifetimeScope`: `ReportWon/ReportLost/ReportResumed`, `ItemsRemainingProvider` (tracking),
  `CanReviveProvider` + `ReviveHandler` (Lose popup), `PauseHandler` (tutorial). Presenters listen on
  `ILevelOutcomeSource`, `ILevelReviveHandler`, `IGameplayPauseService` and never on a view.
- A game subclasses `MainGameLifetimeScope`, overrides `RegisterGameplay`, and swaps that component
  onto the scene's scope object. `MainGameBootstrap` is the placeholder entry point.
- Pools register through `ObjectPoolInstaller.RegisterObjectPool` with `PoolCatalog.asset`
  (MainGame) and `HomePoolCatalog.asset` (Home).

**Start from**: `Game/SceneFlow/GameScenes.cs`, `Game/Boot/BootstrapRunner.cs`,
`Game/Installers/MainGameLifetimeScope.cs`, `Game/Services/GameplayBridge.cs`.

## Four scenes

```
Splash      no LifetimeScope, no DI. Shows the logo, then moves on.          (SplashBootstrap)
  v
Boot        BootLifetimeScope: runs every IBootStep in order, then Home.     (BootstrapRunner)
  v
HomeScene   HomeLifetimeScope: Play button, tab bar, life/coin counters.     (HomeBootstrap)
  v
MainGame    MainGameLifetimeScope: shared systems + GameplayBridge + HUD.    (MainGameBootstrap)
```

## Shared installers

| Extension (`Game/Installers/`) | Boot | Home | MainGame |
|---|---|---|---|
| `RegisterSdkSystem` (`SdkSystemInstaller`) | x | x | x |
| `RegisterLevelProgress` (`LevelProgressInstaller`) | x | x | x |
| `RegisterLocalization` (`LocalizationInstaller`) | x | x | x (Splash builds the same service through `LocalizationProvider.GetOrCreate`) |
| `RegisterLifeSystem` (`LifeSystemInstaller`) | | x | x |
| `RegisterBoosterSystem` (`BoosterSystemInstaller`) | | x | x |
| `RegisterUserProperties` (`UserPropertyInstaller`) | | x | x |
| `RegisterObjectPool`, `RegisterCoinFlight` (`ObjectPoolInstaller`) | | x | x |
| `RegisterTutorialSystem` (`TutorialSystemInstaller`) | | | x |
| `RegisterPopupSystem` (`PopupSystemInstaller`) | | | x (Home registers its popups by hand) |

## Plugging a game in

1. Append the game's values to `BoosterType`, `GameFeature`, `HapticProfile`, `E_TutorialId`,
   `AudioCue`, `PopupId`; fill the matching `*Settings.asset` rows.
2. Implement `ILevelCatalog` on the game's level repository and register it in place of
   `LevelCatalogSO` in the three scopes (`RegisterLevelProgress(catalog)`).
3. Subclass `MainGameLifetimeScope`; in `RegisterGameplay` register the board views and a bootstrap
   that loads `ILevelProgressService.LevelLoad` and wires `GameplayBridge`.
4. Call `bridge.ReportWon()` / `ReportLost()` from the game's outcome watcher; `ReportResumed()` is
   the framework's, fired by `LevelReviveController` after `ReviveHandler` succeeds.
5. Add the game's remote-config keys in its own static class and read them through
   `GameRemoteConfig.Int/Bool/Float`.

## Folders: what the base keeps

| Folder | Content |
|---|---|
| `Core/Domain/` | `E_LevelDifficulty`, `LevelStreak`, `FeatureIntroduction`, `TutorialDefinition`, `Shop/` |
| `Core/GameLogic/` | `LifeRegenRules`, `LevelLoopRules`, `OnboardingRules`, `CoinFormat`, `TutorialSchedule`, `FlatJson` |
| `Core/UseCases/` | lives, coins, boosters, reward grants (daily reward), tutorial rewards |
| `Core/Services/` | every service interface, plus the seam interfaces `ILevelOutcomeSource`, `ILevelReviveHandler`, `IGameplayPauseService`, `ILevelCatalog` |
| `Game/Views/Popup/`, `Views/UI/`, `Views/Tutorial/`, `Views/Transition/`, `Views/Diagnostics/` | popup system and gates, shared UI components, tutorial engine, loading screen, debug overlays (no visuals: built per game from its demo) |
| `Game/Views/Gameplay/` | level-lifecycle presenters only (`LevelPopupPause`, `LevelTrackingPresenter`, `WinCoinRewardPresenter`...) |
| `Game/Services/` | PlayerPrefs services, audio, haptics, pool, remote config, `GameplayBridge` |
| `Game/Data/` | every `*SettingsSO`, catalogs, `LevelCatalogSO` |
| `Editor/` | SO manager, scene switcher, time-scale toolbar, debug windows |
