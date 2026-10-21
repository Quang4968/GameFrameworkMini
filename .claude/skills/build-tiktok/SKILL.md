---
name: build-tiktok
description: Build the TikTok Mini Game (TTSDK, WebGL/Wasm) package end-to-end - preflight the target, defines, appid, ad units and SDK wiring, audit the package size, trigger the TTSDK build tool, watch it, diagnose the known failures (UnityLinker "failed to resolve assembly", file:// load error, cropped canvas, output commit refused), repack without rebuilding, and hand over sizes plus a device test checklist. Use when asked to build, rebuild, test or fix a TikTok / mini game build.
argument-hint: [--release] [--preflight-only] [--pack-only]
---

# /build-tiktok $ARGUMENTS

Skill version 1.3.0 (history in `CHANGELOG.md` next to this file).

The skill lives in the project (`.claude/skills/build-tiktok/`), next to the TikTok-only SDK in
`<Root>/Sdk/`; edit both here. `<Root>` is the game root from the project card in the game's `CLAUDE.md` (template default
`Assets/_Dev`); `scripts/size_audit.cs` takes it on its first line.

Default is a test build (Development + TTSDK log + external debug symbols). `--release` turns all three off and
links with Disk Size + LTO. `--preflight-only` stops after step 1 (size audit included). `--pack-only` skips the
build and repacks `tt-minigame/` from the existing `<OutputDir>/webgl` (see `COMMIT_REFUSED` in step 3). Drive the Editor only through `unity command` (rules in the game's `.claude/rules/unity-editor-cli.md` when it has one).
`eval` output is one JSON line: read the `result` field. `Assets/Plugins/`, `Assets/SDKSetting/`
and `ProjectSettings/` are protected: change them only through the Editor or `eval`, and only after the user
says yes.

## 1. Preflight (read-only, report every miss before building)

| Check | How | Fix |
| --- | --- | --- |
| Editor ready | `unity command editor_status` | wait, or report Editor not running |
| Target is WebGL | `eval`: `EditorUserBuildSettings.activeBuildTarget` | `switch_build_target --target WebGL --confirm true` (long reimport, ask first) |
| TTSDK imported | `Assets/Plugins/com.tiktok.minigame/` exists, `ttsdk_tools` assembly loaded | user imports the TTSDK package |
| AppId set | `StarkBuilderSettings._appId` (see `scripts/trigger_build.cs`) | user pastes it in **TikTokGame/Build Minigame** |
| Framework Wasm, publish type WebGL | `framework == Wasm`, `_publishType` is `AndroidWebGLWithIOS` or `AndroidWebGLOnly` | set the field, `Save()` |
| `MB_TIKTOK` on WebGL | `PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL)` | **MoodBoost > SDK Modules**, tick TikTok Mini Game |
| No stray define reaches WebGL code | WebGL defines hold `MB_TIKTOK` and at most `MB_ADS_FAKE` (test build) / `MB_SDK_LOG`; no leftover mobile `MB_*` from an older project | remove it in Player Settings |
| TikTok ad units | `Assets/SDKSetting/SdkSettings.asset` block `tikTok:` has `rewarded` / `interstitial` ids | ids come from the TikTok developer console under the same appid; empty = that format off |
| Compile clean | `recompile` -> `recompile_status` -> `console --level error` | fix before building |
| Game code names no mobile-only assembly | grep the game scripts for `Solo.MOST_IN_ONE`, `Unity.Services.LevelPlay`, `Firebase`, `Tenjin`, `AdjustSdk`, `UnityEngine.Purchasing` | route through the SDK facade (`IHapticsService`, `IAdService`, `ITrackingService`...), see Known failures |
| No vendor sample in `Assembly-CSharp` | `eval`: `CompilationPipeline.GetAssemblies(PlayerWithoutTestAssemblies)` -> `Assembly-CSharp.sourceFiles` naming a mobile SDK | delete the vendor `Samples/` folder (vendor path: ask first) |
| WebGL template | `PlayerSettings.WebGL.template == "PROJECT:Portrait"` | set it; only matters for browser previews |
| Package size | `unity command eval_file --file .claude/skills/build-tiktok/scripts/size_audit.cs`: every `FIX` line | see **Size audit** below; required before `--release` |

### Size audit

Load time on TikTok is download plus wasm compile, so the wasm dominates. `size_audit.cs` is read-only and
checks, in order of payoff (figures measured on the Game Framework, Oct 2026):

| Check | Why | Release value |
| --- | --- | --- |
| Managed stripping | Minimal keeps every assembly whole (wasm 75.8 MB raw, 194k functions) | **Medium** (with OptimizeSize + LTO: 29.1 MB, 101k functions) |
| `<Root>/link.xml` | VContainer builds the game's and `MoodBoost.Sdk.*` types by reflection, which the linker cannot see | present; add any assembly that fails at runtime |
| IL2CPP code generation | OptimizeSpeed inlines generics | **OptimizeSize** |
| Code optimization | per machine, not stored in the project | `trigger_build.cs` sets **DiskSizeLTO** for a release |
| Mipmapped NPOT sprites | WebGL cannot compress them, so they ship as RGBA32 (a 1080x1920 background: 10.5 MB, 0.9 MB as ASTC) | mipmaps **off** on sprites |
| URP `postProcessData` | drags FilmGrain/SMAA textures and UberPost in although no camera renders post-processing | cleared on every renderer |
| Default render pipeline | an HDR desktop asset adds shader variants | `Mobile_RPAsset`; the `PC` quality level excludes WebGL |
| Long audio clips | WebGL re-encodes to AAC and has no per-platform audio override | mono, quality 0.4 (shared with mobile) |

A `FIX` that touches project settings, a vendor folder or shared import settings needs the user's yes. Leave
`TextMesh Pro/Examples & Extras` (~2.5 MB through its `Resources/`) to the user: vendor path.

Also print the output folder (`StarkBuilderSettings.OutputDir`) and warn if the working tree has uncommitted
changes the build would ship.

## 2. Trigger the build

1. Save the open scene (`save_scene`): the build domain-reloads and drops unsaved scene edits.
2. Edit the two flags at the top of `scripts/trigger_build.cs` (`--release`: both `false`), then
   `unity command eval_file --file .claude/skills/build-tiktok/scripts/trigger_build.cs`.
   It saves `StarkBuilderSettings` and queues the TTSDK window's own Build on `delayCall`, the same path as
   clicking **Build** in **TikTokGame/Build Minigame** (WebGL build + Wasm conversion + packaging). Before
   queuing it deletes `<OutputDir>/webgl` and `<OutputDir>/webgl.ttsdk-backup-*` (see Known failures), and
   refuses an `OutputDir` that is relative or inside the project. With both flags `false` it also turns the
   debug symbols off and sets Code Optimization to Disk Size with LTO (a per-machine setting, not stored in the project).
   The TTSDK window can write its own `enableSDKLog` back over the script's value: after the build, the log line
   `[TTSDK] Scripting Define TTSDK_LOG_DEBUG = <bool>` says what shipped. A release with `True` there needs the
   SDK log unticked in **TikTokGame/Build Minigame** and a rebuild.
3. **`delayCall` only fires when the Editor ticks.** A backgrounded, idle Editor never starts the build: if
   nothing moves within ~20 s, ask the user to click the Unity window once (and not to press Build too, or it
   builds twice).

## 3. Watch it

Start the watcher in the background right after queuing, from the project root:
`.claude/skills/build-tiktok/scripts/watch_build.sh <OutputDir> 45` (`run_in_background`). It counts the errors
already in the console first (an earlier build's failure stays there and must not read as this one's), skips
polls the busy Editor does not answer, and ends with one verdict:

| Verdict | Next |
| --- | --- |
| `SUCCESS` | step 5 |
| `COMMIT_REFUSED` | the WebGL build is usually fine: confirm `[Builder] build successfully!` in `Editor.log`, then run `scripts/pack_only.cs` through `eval_file` and the watcher again (it reports `SUCCESS` once `tt-minigame/` is rewritten) |
| `FAILED <line>` | step 4; the linker entry holds the whole UnityLinker command line, strip `--allowed-assembly` and `/Applications` lines before reading it |
| `NOT_STARTED` | the Editor is idle in the background: ask the user to click the Unity window once (not Build) and rerun the watcher |

A test build takes 5-10 min, a release with LTO 15-30 min. Never `eval`, recompile or run tests while it runs:
the Editor queues them behind the build and every call times out.

## 4. Known failures

| Symptom | Cause | Fix |
| --- | --- | --- |
| `Fatal error in Unity CIL Linker ... Failed to resolve assembly: 'X'` | a kept assembly references X, which is not in the WebGL build (a mobile vendor SDK imported again, or a package without WebGL support). The first stack frame names the method (`file.cs(line)`). | game code: move the call behind the SDK facade (`IHapticsService`, `IAdService`, `ITrackingService`) and drop X from the asmdef. `Assembly-CSharp`: usually a vendor sample script; delete the sample folder. SDK code: fix it in `<Root>/Sdk/` |
| `Can't add component because class 'MeshCollider' doesn't exist!` at runtime | engine code stripping removed Physics while a scene/prefab still has the component | remove the component, or keep the module in a `link.xml` |
| `[TTSDK] Refusing to continue committing/rolling back build output ... resolves to a different physical directory` | TTSDK records the `(volume, inode)` of `<OutputDir>/webgl` when the build starts, and the WebGL build can delete and recreate that folder mid-build; the commit check then fails and `tt-minigame/` keeps the previous package. It happens with or without an old `webgl/` and is unrelated to `LEVELPLAY_DEPENDENCIES_INSTALLED` (the LevelPlay editor re-adds that define on every domain reload) | if `[Builder] build successfully!` and `[TTSDK] ===== WASM Metrics` were logged, the WebGL build is good: run `unity command eval_file --file .claude/skills/build-tiktok/scripts/pack_only.cs` (the window's Pack WebGL step), wait for `[PackOnly] result=` in the console, and check `tt-minigame/` timestamps. `trigger_build.cs` already clears `webgl/` and its backups before building |
| Main thread operation timed out on `eval` | Editor busy (importing, compiling, building) | `editor_status`, wait, retry; never stack evals during a build |
| `tt-minigame.zip` older than `tt-minigame/` | `pack_only.cs` repacks the folder but does not zip it | upload the `tt-minigame/` folder through the TikTok developer IDE, never the stale zip |
| White screen, `MissingMethodException`, an `[Inject]` field null, or a dead button on device only | managed stripping removed a type something reaches by reflection | add its assembly to `<Root>/link.xml` (`preserve="all"`) and rebuild; list only assemblies already in the build (the `ManagedStripped` folder of the WebGL Bee artifacts), or the entry pulls a whole unused assembly in |
| `LEVELPLAY_DEPENDENCIES_INSTALLED` back in the WebGL defines | the LevelPlay editor adds it to the selected target on every domain reload | harmless: no runtime code reads it and the LevelPlay assemblies are filtered out of WebGL; do not fight it |
| Haptics / ads / tracking silent on device | missing `MB_TIKTOK`, empty `tikTok:` ad units, or `TT.InitSDK` timed out (15 s) | check step 1; the SDK logs `TikTok` / `TikTok/Tracking` warnings |

## 5. Hand over

Report: success or the exact error, build duration, `<OutputDir>/tt-minigame` path, appid, dev/release, and
anything preflight warned about. Then the sizes, next to the previous build's when known:

| Line | Source |
| --- | --- |
| wasm raw size and function count | `[TTSDK] File size:` and `Functions total:` in `Editor.log` |
| `wasmcode/*.br`, `data-package/*.br` | file sizes under `tt-minigame/` |
| package total | `du -sh <OutputDir>/tt-minigame`; a release holds no `__TTMG_TEMP__/webgl.symbols.json` |
| user assets by category | the last `Build Report` block in `Editor.log` (Textures, Sounds, Shaders...) |

Then the test checklist:

- **Run**: open `<OutputDir>/tt-minigame` in the TikTok developer IDE, then the QR preview on a phone. The IDE
  simulator neither vibrates nor shows real ads.
- **Top bar**: nothing under the TikTok `... X` capsule (top right). Push the HUD down with a WebGL-only offset
  in the game's layout, never by changing the mobile layout.
- **Load time**: seconds from tap to the Home screen on a mid-range phone; compare release with test.
- **Haptics**: tap buttons and the game's haptic actions; vibration is `TT.VibrateShort` only (no strength levels).
- **Ads**: rewarded from any ad button at once. Interstitials only from level `ads_interstitial_first_level`
  (remote config default in `SdkSettings`, no Firebase on WebGL) and after the 30 s / 45 s cooldowns; lower the
  level temporarily to test, then restore it.
- **Tracking**: events go out through `TT.ReportEvent` and must be declared on the TikTok console; an
  undeclared one logs `TikTok rejected <event> - is it declared on the console?` once. No ad revenue, user
  properties or crashes on TikTok. Info logs need `MB_SDK_LOG` on WebGL.

## 6. Browser preview (optional)

A plain WebGL build (`unity command build --target WebGL --outputPath <dir> --confirm true`, poll
`build_status`) is a quick smoke test of the same code, without the TikTok container: no ads, haptics or
tracking. Never open its `index.html` through `file://`; serve it:
`python3 .claude/skills/build-tiktok/scripts/serve_br.py <dir> 8765` (run in background), open
`http://127.0.0.1:8765/index.html` in the browser pane, check `read_console_messages` for errors. The
`Portrait` template keeps the canvas 9:16 and fitted to the window; a cropped, zoomed canvas means the Default
template with the 1080x1920 web resolution.

## Rules

- Never commit from this skill. If asked afterwards, stage paths one by one (`ProjectSettings/`, `Assets/SDKSetting/`,
  `Assets/Plugins/com.tiktok.minigame/`, `Assets/Editor/StarkBuilderSetting.asset`,
  `<Root>/link.xml`, `game.json`, `project.config.json`) per the game's commit rules (`.claude/rules/commits.md`).
- Build output lives outside the repo. The scripts only ever delete `<OutputDir>/webgl` and
  `<OutputDir>/webgl.ttsdk-backup-*`, and refuse an `OutputDir` inside the project.
- Play mode rewrites the TMP atlas (`* SDF.asset`): revert it before committing.
- Remove build output written under the repo (`AssetDatabase.DeleteAsset` for anything under `Assets/`).
