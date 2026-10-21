# New project checklist

How to stand a new game up on this stack. Defaults for a new project: root `Assets/_Dev/`,
namespace and asmdef prefix `Game`. Nothing under `.claude/` or `docs/stack/` names a game, so
those folders copy verbatim; everything game-specific lives in the project card of `CLAUDE.md`.

## Summary

- Start by cloning the **Game Framework** project: it already holds the copy set below with the
  seams a game plugs into (`ARCHITECTURE.md` of the framework, "Plugging a game in"). Rename the
  prefix `Game` to the new `<Game>` with the sed in step 4, keep the folder `Assets/_Dev/` or move it.
- Copy set (what the framework is made of): `.claude/` (minus `settings.local.json`), `docs/stack/`,
  `<Root>/Scripts/Tests/EditMode/Architecture/`, `.gitignore`, and the generic runtime folders
  (`Scripts/Game/{Boot,SceneFlow,Installers,Services,Views/Popup,Views/UI,Animation}`, the matching
  `Scripts/Core/Services` interfaces and `Scripts/Game/Data` SOs).
  Playable ads come along as `Playables/{_engine,_template,networks.json,package.json,package-lock.json,
  tsconfig.json,.gitignore}` (never a game's `art-map.yaml`, `_game/` kit or `Playables/<playable>/` folders);
  see [playable-ads.md](playable-ads.md).
- The SDK comes along as `Assets/_Dev/Sdk/` (TikTok-only, owned by the project); its per-game config
  lives outside it in `Assets/SDKSetting/` (created by the SDK settings window, never copied between
  games).
- `ArchitectureBaseline` starts empty. The first `/verify` must be green before the first feature.
- `grep -rn <OldGame> .claude docs/stack` must print nothing when you are done.

**Start from**: this file, then `CLAUDE.md` of the source game as the model for the new card.

## Steps

1. **Layout**: create `Assets/_Dev/{Scripts,Data,Prefabs,Scenes,Audio,Fonts,Materials,Mesh,Music,
   Shaders,Textures}/`; `Scripts/{Core,Game,Editor,Tests}` with four asmdefs `Game.Core`,
   `Game.Game`, `Game.Editor`, `Game.Core.Tests` (`rootNamespace` matching). SDK-generated code
   already targets `Assets/_Dev/`.
2. **SDK**: `Assets/_Dev/Sdk/` is copied with the rest of `Assets/_Dev/`. Open **MoodBoost > SDK
   Settings** to create `Assets/SDKSetting/`, and **MoodBoost > SDK Modules** to turn `MB_TIKTOK` on
   (it writes the WebGL define; do not edit Player Settings by hand). Setup steps: `docs/stack/sdk.md`.
   TikTok Mini Game only: switch the target to WebGL, **MoodBoost > TikTok Mini Game > Install
   TTSDK**, enable only `MB_TIKTOK` in SDK Modules, fill the TikTok block (appid, rewarded and
   interstitial ids) in `SdkSettings`. No mobile vendor SDK, MMP or IAP is imported.
3. **Project card**: write the new `CLAUDE.md` from the table below; fill every row before any
   skill runs. Keep the game summary to one paragraph and the lookup table to the docs that exist.
4. **Architecture tests**: edit `ArchitectureConfig.cs` — `ScriptsRoot = "Assets/_Dev/Scripts"`,
   `CoreAssembly = "Game.Core"`, `GameAssembly = "Game.Game"`, `GameAssemblyPrefix = "Game."`,
   `PoolServiceFile`, `ClockServiceFile`; change the `namespace` line of the seven test files to
   `Game.Core.Tests.Architecture` (`sed -i 's/<Old>.Core.Tests/Game.Core.Tests/'`); update the
   Tests asmdef `references`; empty both dictionaries in `ArchitectureBaseline.cs`. Update
   `SdkFacadeAssemblies` / `VendorAssemblyPrefixes` if the SDK's assemblies changed.
5. **Scenes**: `GameScenes` constants + Build Settings list; one `LifetimeScope` per scene, each
   calling the SDK installer and `RegisterObjectPool` with its own `PoolCatalogSO`; Boot runs the
   `IBootStep`s. Boot also calls `R3.ObservableSystem.RegisterUnhandledExceptionHandler(Debug.LogException)`.
6. **Hooks**: `chmod +x .claude/hooks/*.sh .claude/skills/*/scripts/*.sh`; append the project's
   vendor folders to `.claude/hooks/protected-paths.txt`; set `env.UNITY_PROJECT_PATH` to the
   checkout's absolute path in `.claude/settings.local.json` (several Editors may run at once);
   confirm `perl`, `git` and `python3` (`jq` optional; `/retro` needs it) run on `PATH`.
7. **Branches**: create `develop` from the first commit and push it; `master` stays the release
   branch. No hook guards them: the team keeps both clean by convention (`git-workflow.md`). If the
   host refuses those names, change them in `.claude/rules/commits.md`, `/task`, `/minigame` and `/finish`.
8. **Codex**: copy `.codex/` without an MCP server entry (the rules forbid it); run
   `.claude/hooks/build-agents-md.sh` to generate `AGENTS.md`.
9. **Playables** (optional): `npm install` in `Playables/` (Node 20+), `npm run build -- _template && npm run
   audit -- _template`, then write the game's `Playables/art-map.yaml` (`/playable-ads artmap`: `scene` set,
   `npm run deps -- <scene>` at 0 MISSING) and, once a first playable works, its kit `Playables/_game/`.
10. **Verify**: Editor open → `/verify` → 0 failures with an empty baseline;
   `grep -rn <OldGame> .claude docs/stack` empty; `grep -rn '<Game>' CLAUDE.md` empty.

## Blank project card

```markdown
| Key | Value |
| --- | --- |
| Root | `Assets/_Dev/` |
| `<Game>` (namespace / asmdef prefix) | `Game` |
| Scripts | `Assets/_Dev/Scripts/{Core,Game,Editor,Tests}/` |
| Asset roots | `Assets/_Dev/{Data,Prefabs,Scenes,Textures,Materials,Audio,Fonts,Mesh,Music,Shaders}/` |
| Assemblies | `Game.Core`, `Game.Game`, `Game.Editor`, `Game.Core.Tests` |
| PlayerPrefs keys | `"Game.<Name>"` |
| Menu roots | `CreateAssetMenu` and `MenuItem` under `Game/` |
| Scenes | `Splash` → `Boot` → `Home` → `Main` (`Game/SceneFlow/GameScenes.cs`) |
| SDK | `<Root>/Sdk/` (project-owned, TikTok-only); config `Assets/SDKSetting/`; every scope calls `<SdkInstaller>` |
| Pool catalogs | `Assets/_Dev/Data/PoolCatalog.asset`; registered by `ObjectPoolInstaller.RegisterObjectPool` |
| Index-stored enums (append only) | `PopupId`, `AudioCue`, `HapticCue`, `HapticProfile`, ... |
| Reference implementations | `Game/Installers/<Feature>Installer.cs`, `Game/Services/PlayerPrefs<Feature>Service.cs` |
| Architecture config | `Scripts/Tests/EditMode/Architecture/ArchitectureConfig.cs` |
| Game view | portrait, `--width 1080 --height 1920` for `--source screen` captures |
| Playables | `Playables/` (shared `_engine/`, `_template/`, `networks.json`; per game `art-map.yaml` and kit `_game/`; one folder per playable) |
| Canvas reference | 1080×1920, `CanvasScaler` matches width: canvas units = demo px × 1080 / demo width; `uiref.py --ref 1080` |
| UI demos | `Assets/_Dev/Textures/UI/<Feature>/*demo*` |
| TMP presets | `Assets/_Dev/Fonts/<Font>-*.mat`; cap height = font size × `m_CapLine` / `m_PointSize` of the font asset |
| Button sprites | `Assets/_Dev/Textures/UI/<Folder>/<Button>(_border).png`, ... |
| Play-testing consumes | `Game.Lives`, `Game.Coins`, ... |
```

`/ui-from-demo` also needs `python3` with `numpy` and macOS `sips` for its measuring scripts.
