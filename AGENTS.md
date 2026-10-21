<!-- Generated from CLAUDE.md + .claude/rules/*.md by .claude/hooks/build-agents-md.sh; edit those, not this file. -->

# Game Framework

Rules live in `.claude/rules/` (auto-loaded; long form in `docs/stack/`). A game is built with
`/minigame`: one plan the user approves, then an autonomous run to a TikTok build. Any other task
starts with `/task`, which reads this card and the doc for the system before any code, presents a
plan, and waits for the go-ahead. Every C# change ends with `/verify`. `<Game>` in any rule or stack doc resolves
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
| `/minigame`, its stages, `state.py`, `minigame-continue.sh`, building a game from its assets | `docs/stack/minigame-pipeline.md` |
| `.claude/hooks/`, `.claude/agents/`, `/retro`, the Editor lock | `docs/stack/agent-workflow.md` |
| a new system, layering, DI per scene, pooling, R3/UniTask/LitMotion patterns, Editor CLI, doc format, branches, porting the template, building UI from a demo | `docs/stack/` (`adding-a-system.md`, `architecture.md`, `reactive-and-motion.md`, `unity-editor-cli.md`, `doc-template.md`, `git-workflow.md`, `new-project-checklist.md`, `playable-ads.md`, `ui-from-demo.md`) |

<!-- .claude/rules/architecture.md -->

# Architecture hard rules

Each of these fails silently: no compile error, often no runtime error. The architecture tests catch
most; the rest show up as a dead button or a null field.

- `Core` references nothing but the BCL and `UnityEngine.CoreModule` math (`Vector*`, `Mathf`).
  No `Game`, VContainer, UniTask, LitMotion, R3, SDK, `PlayerPrefs`, `Debug`, `Time`.
- Gameplay rules live in `Core` (`GameLogic/` pure functions, `UseCases/` whole actions). Views
  render state, take input and call use cases; no rules in a `MonoBehaviour`.
- `Core/Services/` holds interfaces only; implementations go in `Game/Services/`. A service announces
  change with `event Action<T>`. An interface that must speak Unity types (`GameObject`,
  `Transform`) lives in `Game/Services/` instead.
- R3 is `Game`-only and never appears in a `Core/Services` signature.
- Every scene has its own `LifetimeScope`, wiped on scene change; there is no global scope. A service
  a later scene needs is registered there again through a shared `<Feature>Installer` extension
  method, never copy-pasted. Cross-scene objects use `static` + `DontDestroyOnLoad`, built lazily.
- `RegisterComponentInHierarchy<T>` resolves at `LifetimeScope.Awake()`: a scene missing that object
  loses its whole container. Optional components: `[SerializeField]` + `if (x != null) Register`.
- A prefab holding `[Inject]` is built with `resolver.Instantiate`; `Object.Instantiate` leaves the
  dependency null with no error.
- A component present in several scenes (`PopupHost`) must not take a service only one scene
  registers; the other scenes' containers fail to build.
- Every short-lived visual spawns through `IObjectPoolService` and is declared in the pool catalog
  named in the project card. Lifetime via `TimedDespawner` on the prefab, animation on the effect
  object. No `Object.Instantiate` per effect, no private pools, no `static PoolBoss.X` outside
  `PoolBossObjectPoolService`.
- `IClockService` is the only reader of `DateTime.UtcNow` (`SystemClockService`).
- Tunable numbers live in a `*SettingsSO` under `Game/Data/`, never in a scene or a view. Every asset
  field on a scope carries a `[Tooltip]` naming what breaks when it is empty, and a null asset
  disables that feature instead of the scene.
- An enum an asset stores by index (the project card lists them) takes new values appended at the
  end only; inserting shifts every later asset entry onto the wrong item.
- Game code uses the SDK facade interfaces only (`IAdService`, `ITrackingService`, `IIapService`,
  `IRemoteConfigService`, `INotificationService`), registered in every scope; never a vendor
  namespace. Reference: `Assets/SDK/README.md`.

<!-- .claude/rules/async-reactive-motion.md -->

# Async, reactive state and motion

Patterns with code: `docs/stack/reactive-and-motion.md`.

- Async is `async UniTask`, or `async UniTaskVoid` started with `.Forget()`. Every async method takes
  a `CancellationToken` and passes it down. No coroutines, no `async void`.
- A `MonoBehaviour` uses `destroyCancellationToken`; a pure C# entry point owns a
  `CancellationTokenSource` cancelled in `Dispose`. Work that must outlive the scene must not listen
  to a scene token (`TransitionSceneLoader`).
- R3 is `Game`-only. Keep `ReactiveProperty<T>` / `Subject<T>` private and expose
  `ReadOnlyReactiveProperty<T>` / `Observable<T>`. Tie every subscription to a lifetime:
  `.RegisterTo(destroyCancellationToken)` in a `MonoBehaviour`, or `.AddTo(ref disposableBag)` with
  the bag disposed in `Dispose`. Bridge a Core service event once with `Observable.FromEvent`.
- The project has R3 core only (NuGet, no `R3.Unity`): `AddTo(this)` does not exist, frame operators
  (`EveryUpdate`, `*Frame`) throw, and time operators (`Timer`, `Interval`, `Delay`, `Debounce`) call
  back off the main thread. Use `ITickable` or `UniTask.Delay(..., cancellationToken: ct)`.
- Motion is LitMotion only: `LMotion.Create(...).BindTo...(target).AddTo(gameObject)`. Keep the
  `MotionHandle` in a field and `TryCancel()` it before starting another. Anything that must move
  while a popup holds `Time.timeScale = 0` uses `MotionScheduler.UpdateIgnoreTimeScale`.
- `ToUniTask` is off (UniTask lives in `Assets/Plugins`): await with
  `UniTask.WaitWhile(() => motion.IsActive(), cancellationToken: ct)` and cancel the motion when
  `ct` fires.
- A despawned pooled object is only inactive: `AddTo(gameObject)` stops nothing. Cancel its motions,
  token sources and subscriptions in `OnDespawned`; reset its state in `OnSpawned`.

<!-- .claude/rules/commits.md -->

# Commits and branches

Branches:

- `develop` is the integration branch, `master` the release branch. Nobody edits or commits on
  either one. Every feature or bug fix lives on its own branch cut from `develop`: `feat/<slug>`
  or `fix/<slug>`, kebab-case English (`feat/daily-reward-popup`).
- `/task` cuts the branch after the plan is approved; `/finish` merges it back with
  `git merge --no-ff` into `develop`, pushes `develop`, and deletes the branch locally and remotely.
- One working tree, one branch at a time: the Unity Editor is bound to this folder, so worktrees are
  out. Never switch branches over a dirty tree that holds someone else's work; ask first.
- Long form: `docs/stack/git-workflow.md`.

Commits:

- English only. Subject `<type>(<scope>): <imperative summary>`, under 72 characters, lowercase
  type (`feat`, `fix`, `refactor`, `chore`, `docs`, `test`). No body unless the change needs
  essential context.
- Describe the product or code change, not the process used to make it.
- Never mention the agent, AI, LLMs, prompts, co-authorship or generated content in messages,
  bodies, trailers, branch names, code comments or documentation unless explicitly requested.
  No `Co-Authored-By` trailers.
- Commit only when asked, on the feature branch. Stage the paths this task changed, one by one.
  An approved `/minigame` plan asks for one commit per stage on `feat/<slug>`; never a push or merge.

Examples: `feat(popup): add pause dialog`, `fix(input): prevent duplicate tap events`,
`chore(deps): update addressables package`.

<!-- .claude/rules/core.md -->

# Core

- Pure C#: no `MonoBehaviour`, `ScriptableObject`, `GameObject`, `Transform`, `PlayerPrefs`,
  `Debug`, `Time`, `Resources`, `Input`; no VContainer, UniTask, LitMotion, R3 or SDK. Only the BCL
  and the math types (`Vector2Int`, `Vector3Int`, `Vector3`, `Mathf`).
- Time comes through an injected `IClockService`; rules never read `DateTime` themselves, so they
  are testable with a fake clock.
- `GameLogic/` holds pure functions; `UseCases/` holds one complete action each. A service
  interface only holds state and announces change with `event Action<T>`; it carries no rules.
- Every Core rule gets an EditMode test in `Scripts/Tests/EditMode/Core/` with fake services.

<!-- .claude/rules/csharp-style.md -->

# C# style

| Kind | Rule | Example |
| --- | --- | --- |
| Private / protected field, incl. `[SerializeField]` | `camelCase`, no underscore | `popupHost`, `[SerializeField] float duration` |
| Enum type | `PascalCase`, no prefix | `PopupId`, `HapticCue` |
| Interface | `I` + `PascalCase` | `IClockService`, `IObjectPoolService` |
| Class, struct, method, property, event | `PascalCase` | `PopupHost`, `TrySpend` |
| Enum member, `const`, `static readonly` | `PascalCase` | `Ease.OutCubic`, `DefaultMaxLives` |
| Local, parameter | `camelCase` | `levelIndex` |
| Async method | `Async` suffix | `LoadSceneAsync` |
| `ScriptableObject` type | `SO` suffix | `PopupCatalogSO` |
| Abstract base | plain name, no `Base` / `Abstract` suffix | `PieceObstacle` |

- Legacy files may still carry retired `I_`, `E_` and `_field` names (listed in
  `ArchitectureBaseline`). New types and new fields follow the table even inside such a file.
  Rename an old symbol only when the task asks, every reference in the same change; a renamed
  `[SerializeField]` needs `[FormerlySerializedAs("_oldName")]` or scenes and prefabs lose the value.
- One public type per file; file name equals the type name.
- Namespace = `<Game>.` + folder path under `Scripts/` (`<Game>.Core.UseCases`); `<Game>` is the
  prefix in the project card.
- Member order: constants, fields, constructor, Unity messages, public methods, private methods.
- Name for the domain (`coin`, `popup`, `level`, `tray`), not the pattern: no `Manager`, `Helper`
  or `Data` suffix on a new type. Naming never overrides layering.

Comments:

- English only in every new or edited comment. Do not mass-translate existing Vietnamese headers;
  replace one only when rewriting that file's code.
- Under 5 lines, 3 or fewer is better, one line is the norm. Comment only what the code cannot say:
  an invariant, a unit, a non-obvious reason, a workaround and why it is needed.
- Never: restatements of the method name, step-by-step narration, section banners, changelogs,
  commented-out code, or a comment written to fill a header.

<!-- .claude/rules/editor-scripts.md -->

# Editor scripts

- `MenuItem` and `CreateAssetMenu` paths start with the menu root in the project card (`<Game>/`).
- A debug window works in two modes: Editor not playing → write PlayerPrefs directly; playing → go
  through the scene's service. Writing PlayerPrefs under a running service gets overwritten, because
  the service keeps state in RAM and saves only on change.
- Editor-only hooks into runtime code are `#if UNITY_EDITOR` static fields on the service
  (`SystemClockService.DebugOffset`), never a second code path.
- After editing level or config data, run the matching validator menu item and read
  `unity command console --level error`.

<!-- .claude/rules/file-layout.md -->

# File layout

**No C# file may exceed 300 lines.** This outranks every other rule: a file that would cross 300
is split by responsibility before it is written, never by line count (no `PartTwo`, no `partial`
halves). Touching a file already over the limit: bring it under in the same change if the work is
in the part that needs splitting; otherwise say it is over and leave it, never grow it.

Where code goes — stop at the first question that is true:

1. Needs `MonoBehaviour`/`GameObject`/`Transform`/Input/LitMotion? → `Scripts/Game/`
2. Only a rule, a computation, or a service interface? → `Scripts/Core/`
3. Editor window, custom inspector, tool? → `Scripts/Editor/`
4. Unit test for Core? → `Scripts/Tests/EditMode/`

| Responsibility | Location (relative to the project root) |
| --- | --- |
| Pure C# domain models | `Scripts/Core/Domain/` |
| Rules and algorithms | `Scripts/Core/GameLogic/` |
| Application actions | `Scripts/Core/UseCases/` |
| Core service interfaces (`event Action<T>` for state changes) | `Scripts/Core/Services/` |
| Unity views and input | `Scripts/Game/Views/<Feature>/` |
| Shared effects and tweens | `Scripts/Game/Animation/` |
| Scene entry points and scene flow | `Scripts/Game/Boot/`, `Scripts/Game/SceneFlow/` |
| Unity implementations of Core services | `Scripts/Game/Services/` |
| ScriptableObject definitions | `Scripts/Game/Data/` |
| VContainer wiring | `Scripts/Game/Installers/` |
| Editor-only tools | `Scripts/Editor/` |
| Core unit tests | `Scripts/Tests/EditMode/Core/` |

- Use the existing folders. Never create a new top-level code folder or a catch-all such as
  `Common`, `Utils`, `Helpers`, `Generated`, or one named after an agent.
- Put a file in the smallest existing feature folder that matches; follow nearby files.
- Unity assets go in their asset directories (`Data/`, `Prefabs/`, `Scenes/`, `Textures/`,
  `Materials/`), never beside C# files.
- Documentation lives in `docs/` at the repo root (outside `Assets/`, so Unity does not import it).

<!-- .claude/rules/tests.md -->

# Tests

- NUnit, EditMode, no scene. Test class `<Subject>Tests`; namespace mirrors the Core folder
  (`<Game>.Core.Tests.<Folder>`); method names `Method_Scenario_Expected`.
- Fakes are private nested `sealed class FakeX : IX` inside the test class; no mocking library.
  Shared builders live in a `*Fixtures.cs` beside the tests.
- A test targets a contract that already exists or that the approved plan names. Never add or change
  a member of a Core interface, domain type, enum or asset field so a test or its fake compiles: the
  test would pin an API nobody designed, and the next change bends the code to fit it. Stop and
  re-plan instead.
- An existing assertion changes only when the plan says that behaviour changes. A red test that pins
  something the docs never agreed on is a question for the user, not a reason to bend the code or
  to rewrite the assertion silently.
- `Tests/EditMode/Architecture/` fails on the silent breaks the compiler cannot see (layering, SDK
  boundaries, coroutines, tween libraries, Pool Boss statics, singletons, `async void`, `Find*`, raw
  `Instantiate`, direct `DateTime`, retired names, files over 300 lines).
- `ArchitectureBaseline` lists debt that predates the tests. Never add an entry to make new code
  pass; fix the code. Paying debt off fails `BaselineHasNoPaidOffDebt` on purpose: lower or remove
  that entry in the same change.
- `ArchitectureConfig` is the only project-specific file and must match the project card in
  `CLAUDE.md`. Long form: `docs/stack/architecture-tests.md`.

<!-- .claude/rules/ui-from-demo.md -->

# UI from a demo

Building, rebuilding or restyling any UI (popup, screen, HUD, UI prefab) goes through
`/ui-from-demo`; long form `docs/stack/ui-from-demo.md`. Each rule below stands for a build the user
had to fix by hand.

- The demo image is the **only visual reference**. Another popup or prefab is a wiring reference at
  most: its button border, text layers, materials and sizes are never carried over unchecked. The
  one standing exception is a popup's frame: the game's first popup takes it from its demo, later
  popups reuse it, and a demo that draws another frame is a question in the spec.
- No number by eye: every position, size, gap and font size is measured on the demo in canvas units
  (`uiref.py`) and written in the spec before it is built.
- Every text style is read from a zoomed crop — face colour, outline, underlay or shadow, Bold,
  size, case — and maps to an existing TMP preset, or the user is asked.
- Every button is read from the demo — body, border ring or none, size, label, icons, states. What
  the demo does not show is deleted, never hidden with alpha 0 or a disabled object.
- A sprite the demo needs and the project lacks is asked for, never substituted.
- The user approves the spec (asset list, layout, text and button tables) before the build, and sees
  the build only after a side-by-side compare with the demo passes row by row; the report carries
  that image. Inside a `/minigame` run the approved build plan approves the spec; the compare still
  gates every screen.

<!-- .claude/rules/unity-editor-cli.md -->

# Unity Editor CLI

The running Editor is driven only through `unity command ...` (`com.unity.pipeline`, experimental;
version per `Packages/manifest.json`). Never use `mcp__unityMCP__*`; everything is reachable through a
dedicated command or `eval`. Long form and `eval` recipes: `docs/stack/unity-editor-cli.md`.

- **Target this project explicitly.** Several Editors may run at once and auto-detection picks any of
  them: a delete meant for this project once landed in another one. `UNITY_PROJECT_PATH` (set per
  machine in `.claude/settings.local.json` `env`) covers it; when it is not set, put
  `--project-path "<repo root>"` after the subcommand (`unity command eval --project-path "..."`), and
  check `projectPath` in `editor_status` before the first mutating call.

- Tests: `unity command run_tests --mode EditMode`. The result is in the response; `test_status`
  reports `no_tests` afterwards.
- C# edits: `recompile` → `recompile_status` (poll until idle) → `console --level error`. **Save
  the open scene first**: the domain reload discards unsaved scene edits.
- Screenshots: `capture_game_view --save_path "Temp/shot.png" --width W --height H`. Without
  `--save_path` the image comes back as base64 and floods the context. The PNG lands under `Assets/`
  whatever path is given: remove it with `AssetDatabase.DeleteAsset` in `eval` (takes the `.meta`
  with it; a shell delete of a `.meta` is blocked).
- `--source camera` (default) omits Screen Space-Overlay UI; `--source screen` includes it, needs
  Play mode and the portrait size from the project card (otherwise the canvas comes out squashed).
- `eval` compiles a method body: no `using`, fully-qualify every type
  (`UnityEditor.EditorApplication.ExecuteMenuItem(...)`).
- `eval` is capped at 5 s on the main thread regardless of `--timeout`: chunk heavy work per call.
  A `Main thread operation timed out` while the Editor is busy is retried after `editor_status`.
- `OpenScene` inside `eval` unloads assets loaded earlier in the same call: load after opening.
- Reimport a prefab (`AssetDatabase.ImportAsset`) before editing it through `eval`; a stale
  AssetDatabase copy overwrites the file on the next domain reload.
- Compile-check `#if UNITY_IOS` / `UNITY_ANDROID` code with `AssemblyBuilder` in `eval`; the Editor
  compiles only the active target.
- Play mode rewrites the TMP dynamic font atlas (`* SDF.asset`): revert it before committing.
- Play-testing spends persisted player state (PlayerPrefs lives, boosters, progress — keys in the
  project card): snapshot before, restore after.
- Dedicated commands beat `eval`: `open_scene --path`, `save_scene`, `editor_play` /
  `editor_stop`, `menu --path "<Game>/Item"`, `eval_file --file` for long scripts.
- Long jobs (`build`, `bake_*`, `audit`) take `--detach`; poll `<name>_status`.
- Output is one long JSON line: filter in the shell before reading a large result
  (`get_scene_hierarchy` has no pagination).

<!-- .claude/rules/workflow.md -->

# Workflow

- Building a game from its assets is `/minigame`: one interactive phase (assets, plan, every input
  and grant), then an autonomous loop. The approved build plan counts as the go-ahead for every stage
  and every skill the loop calls (`/task`, `/new-*`, `/ui-from-demo` spec); nothing stops to ask until
  the run ends (`.claude/skills/minigame/reference/autonomy.md`).
- Any other task starts with `/task`: read the project card in `CLAUDE.md`, then the one doc for the
  system being touched, then plan. **Present the plan and stop for the user's go-ahead before the
  first edit** — every task, even a one-file fix. The user decides the direction, not the agent.
- A design question with several defensible answers is put to the user as options with a
  recommendation before it is planned.
- Read a doc's first 20 lines only (`Read` with `limit: 20`): the summary block holds the cross-file
  invariants no single file states. Read further only when the summary is not enough.
- Source files carry dense header comments: grep to the symbol and read around it, never whole files.
- Game-owned files live under the root named in the project card (template default `Assets/_Dev/`,
  layout `Scripts/{Core,Game,Editor,Tests}`, `Data/`, `Prefabs/`, `Scenes/`...). Rules and skills
  name paths relative to that root; the namespace prefix (`<Game>`) comes from the same card.
- A new service, persisted data or settings SO → `/new-system`; a new `MonoBehaviour` → `/new-view`;
  a new popup → `/new-popup`; UI built or restyled from a demo → `/ui-from-demo`; a new system doc
  → `/write-doc`.
- After every C# change run `/verify` (`unity command run_tests --mode EditMode` underneath); inside
  a `/minigame` run, `/verify --no-review` per stage and the review once at the end. A red
  architecture test is a code bug, never a baseline edit.
- Update the system's doc in the same change that alters its behaviour; no parallel doc set.
- `Assets/SDK/`, `Assets/Plugins/`, `Assets/Packages/`, `Assets/TextMesh Pro/`, vendor folders,
  `.meta` files, `Packages/` and `ProjectSettings/` are read-only; a hook blocks edits with the
  reason. SDK changes happen in the SDK repo and arrive through `/sdk-update`.
- Generated code must be indistinguishable from hand-written code: module namespace, naming,
  formatting, placement. No authorship banners, generated-code markers, prompt transcripts, task
  summaries, backup files, or files and folders named after an agent or model.
- Commit `.meta` files only when Unity generated them for a new or moved asset.
- Parallel agent sessions share one working tree and one Editor: re-read a file before editing it,
  stage only your own paths (never `git add -A`), and check `unity command editor_status` before a
  recompile or Play mode.
- One session drives the Editor at a time: a hook takes the Editor lock on the first `unity command`
  that is not a pure read and refuses other sessions until it is released (`/task` step 7, session
  end, or 15 min unused). Blocked means do the work that needs no Editor, never retry in a loop.
