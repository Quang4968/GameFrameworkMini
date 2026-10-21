# Game Framework

Rules live in `.claude/rules/` (auto-loaded; long form in `docs/stack/`). Every task starts with
`/task`, which reads this card and the doc for the system before any code, presents a plan, and waits
for the go-ahead. Every C# change ends with `/verify`. `<Game>` in any rule or stack doc resolves
through the project card below.

**This project**: the base every new TikTok Mini Game is cloned from. It holds everything a casual level game
shares - Splash → Boot → Home → MainGame flow, per-scene DI, popups, audio/haptics, lives, coins,
shop, boosters, tutorial engine, feature intros, tracking hooks, pooling, editor tools - and no
gameplay and no visuals: every popup, screen and HUD is built per game from its demo
(`docs/stack/popup-behaviours.md`). The seam to the game is `GameplayBridge` (`Game/Services/`): the game reports level
outcomes, revives and input pauses through it, and the framework's popups, economy and tracking
listen on the interfaces behind it. Porting steps: `docs/stack/new-project-checklist.md`.

## Project card

| Key | Value |
| --- | --- |
| Root | `Assets/_Dev/` |
| `<Game>` (namespace / asmdef prefix) | `Game` |
| Scripts | `Assets/_Dev/Scripts/{Core,Game,Editor,Tests}/` |
| Asset roots | `Assets/_Dev/{Data,Prefabs,Scenes,Textures,Materials,Audio,AudioNew,Fonts,Shaders}/` |
| Assemblies | `Game.Core`, `Game.Game`, `Game.Editor`, `Game.Core.Tests` |
| PlayerPrefs keys | `"Game.<Name>"` |
| Menu roots | `CreateAssetMenu` and `MenuItem` under `Game/` |
| Scenes | `Splash` → `Boot` → `HomeScene` → `MainGame` (`Game/SceneFlow/GameScenes.cs`) |
| SDK | **Not in this repo.** Added per game as a git submodule at `Assets/SDK/` (MoodBoost facade assemblies `MoodBoost.Sdk.{Core,Ads,Tracking,Iap,Notifications,Unity}`); the framework does not compile until it is there. Config `Assets/SDKSetting/` is created per game by the SDK settings window; every scope calls `SdkSystemInstaller.RegisterSdkSystem`. **TikTok Mini Game only**: WebGL target, the one define `MB_TIKTOK` (TTSDK in `Assets/Plugins/com.tiktok.minigame`); no mobile vendor SDK, IAP, notifications or consent popup, and every other target's defines stay empty |
| Pool catalogs | `Assets/_Dev/Data/PoolCatalog.asset` (MainGame), `HomePoolCatalog.asset` (Home); registered by `ObjectPoolInstaller.RegisterObjectPool` |
| Index-stored enums (append only) | `PopupId`, `AudioCue`, `HapticCue`, `HapticProfile`, `BoosterType`, `GameFeature`, `E_TutorialId` |
| Reference implementations | `Game/Installers/LifeSystemInstaller.cs`, `Game/Services/PlayerPrefsCoinService.cs` |
| Architecture config | `Scripts/Tests/EditMode/Architecture/ArchitectureConfig.cs` |
| Game view | portrait, `--width 1080 --height 1920` for `--source screen` captures |
| Playables | `Playables/` (shared `_engine/`, `_template/`, `networks.json`; a game adds its `art-map.yaml` and kit `_game/` with its `template/`; one folder per playable ad, built with `/playable-ads`) |
| Canvas reference | 1080×1920, `CanvasScaler` matches width (Boot, HomeScene, MainGame): canvas units = demo px × 1080 / demo width; `uiref.py --ref 1080` |
| UI demos | `Assets/_Dev/Textures/UI/<Feature>/*demo*` (`.png` / `.jpg`) |
| TMP presets | `Assets/_Dev/Fonts/LilitaOne-Regular-Outline-*.mat` (one font); outline and underlay only through a preset, a text sets size, Bold, case, alignment; cap height = font size × 64/90 |
| Button sprites | come with the game's art under `Textures/UI/`; body + `_border` ring pairs where the demo draws a ring |
| Play-testing consumes | `Game.Lives`, `Game.LivesRefillRemainingTicks`, `Game.LivesClockAnchorUtcTicks`, `Game.UnlimitedLifeEndUtcTicks`, `Game.DailyReward*`, `Game.Booster.*`, `Game.CurrentLevel`, `Game.Coins`, `Game.Language` |

## Hard rules of the base

- Gameplay never lands in this project. A file that names a board, a piece or a level format
  belongs to the game that clones it.
- `BoosterType`, `GameFeature`, `HapticProfile` are empty here and `E_TutorialId` holds only
  `HowToPlay`; `AudioCue` and `PopupId` hold the values the framework plays or opens itself. A game
  appends after them and never reorders.
- The MainGame scene ends a level only through `GameplayBridge.ReportWon/ReportLost`; a game
  registers its views in a `MainGameLifetimeScope` subclass (`RegisterGameplay`).
- A popup freezes the level (`Time.timeScale = 0`); anything that must keep moving over a popup runs
  on unscaled time.

## Which doc to read

Read the doc before the code (protocol in `.claude/rules/workflow.md`). First matching row wins:

| Touching | Read |
| --- | --- |
| anything named `*DailyReward*`, `Game/Views/DailyReward/` | `docs/daily-reward.md` |
| unlimited lives: `UnlimitedLifeRules`, `ILifeService.UnlimitedTimeLeft/AddUnlimited`, `LeavingLevelCostsLifeUseCase` | `docs/unlimited-lives.md` |
| playable ads: `Playables/`, `/playable-ads`, `networks.json`, `art-map.yaml` | `docs/stack/playable-ads.md` |
| localization: `*Locali*` (`ILocalizationService`, `LocalizationTable`, `LocalizationCsv`, `LocalizationProvider`, `LocalizationSettingsSO`, `LocalizedText`, `Localize(...)`), `LanguageEntry`, `LanguageFontProfileSO`, `FontSlot`, `FontStyleMaterial`, `FontMaterialCloneRules`, `FontProfile*`, `LocalizedTextFitter`, `LocalizationDebugPanel`, `Data/Localization/`, `tools/localization/`, localization keys / sheet / language fonts | `docs/localization.md` |
| a popup, screen or HUD view: Win, Lose, Out of lives, Buy booster, Shop, Settings, Profile, Home, `HomeBootstrap.RequestPlay` | `docs/stack/popup-behaviours.md` |
| scene flow, which installer each scene calls, the GameplayBridge seam | `ARCHITECTURE.md` |
| `.claude/hooks/`, `.claude/agents/`, `/lead`, `/design`, `/retro`, the Editor lock, supervisor questions | `docs/stack/agent-workflow.md` |
| a new system, layering, DI per scene, pooling, R3/UniTask/LitMotion patterns, Editor CLI, doc format, branches, porting the template, building UI from a demo | `docs/stack/` (`adding-a-system.md`, `architecture.md`, `reactive-and-motion.md`, `unity-editor-cli.md`, `doc-template.md`, `git-workflow.md`, `new-project-checklist.md`, `playable-ads.md`, `ui-from-demo.md`) |
