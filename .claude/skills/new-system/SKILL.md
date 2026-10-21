---
name: new-system
description: Scaffold a new system the project way - Core interface, use case, PlayerPrefs implementation, SettingsSO, installer extension, LifetimeScope wiring, EditMode test with a fake. Use when adding a service, persisted player data, or a tunable settings asset.
argument-hint: <Name> [--no-persist]
---

# /new-system $ARGUMENTS

The six-step recipe from `docs/stack/adding-a-system.md`, with templates in
`${CLAUDE_SKILL_DIR}/templates/`. `<Name>` is the PascalCase feature name (`DailyReward`),
`<Property>` its main value (`Streak`), `<Verb>` the use case action (`Claim`). `<Game>` and the
scripts root come from the project card in `CLAUDE.md`; every path below is relative to that root.

## 1. Ask once, plan, stop

Read `docs/stack/adding-a-system.md` (`limit: 20`) and the project card. Ask the user in one
`AskUserQuestion`: what state the system holds, which scenes need it, whether numbers are
designer-tunable. Then list the files you will create (from the table below), the scopes you will
touch, the `Register*` variant, and stop for the go-ahead. No file is written before the answer.

| Template | Destination |
| --- | --- |
| `IService.cs.tmpl` | `Scripts/Core/Services/I<Name>Service.cs` |
| `UseCase.cs.tmpl` | `Scripts/Core/UseCases/<Verb><Name>UseCase.cs` |
| `PlayerPrefsService.cs.tmpl` | `Scripts/Game/Services/PlayerPrefs<Name>Service.cs` (skip with `--no-persist`; then a plain `<Name>Service`) |
| `SettingsSO.cs.tmpl` | `Scripts/Game/Data/<Name>SettingsSO.cs` (only if tunable) |
| `Installer.cs.tmpl` | `Scripts/Game/Installers/<Name>Installer.cs` (only if two or more scenes use it) |
| `UseCaseTests.cs.tmpl` | `Scripts/Tests/EditMode/Core/UseCases/<Verb><Name>UseCaseTests.cs` |

## 2. Core

- Interface: properties + `event Action<T>` per changing value + commands. No Unity types, no
  rules; one comment stating the invariant (`TrySpend` returns `false` instead of overdrawing).
- Rules go to `Scripts/Core/GameLogic/<Name>Rules.cs` as pure static functions, or to the use case
  when the whole thing is one action. The service holds state and saves; it decides nothing.
- Time comes through `IClockService`, never `DateTime`.

## 3. Game

- `PlayerPrefs<Name>Service`: `public const string PrefsKey = "<Game>.<Name>"`, state in RAM,
  `PlayerPrefs.Save()` on every change, floor/ceiling clamped inside the service.
- `<Name>SettingsSO`: `[CreateAssetMenu(menuName = "<Game>/<Name> Settings")]`,
  `[SerializeField, Min(0)]` + read-only properties. Create the asset through
  `unity command eval` (`ScriptableObject.CreateInstance` + `AssetDatabase.CreateAsset` under the
  card's `Data/`), never by writing the `.asset` or `.meta` by hand.
- Registration: two or more scenes → `<Name>Installer.Register<Name>(this IContainerBuilder,
  <Name>SettingsSO)` with a `const` default for a null asset; one scene → register inside that
  scope. The scope gets a `[SerializeField] <Name>SettingsSO` with a `[Tooltip]` that states what
  breaks when it is empty, and the code runs with it null. Pick the variant from the decision table;
  add `RegisterBuildCallback` when the service must start at scene load.

## 4. Test

`<Verb><Name>UseCaseTests`: private nested `sealed class Fake<Name>Service : I<Name>Service`,
methods named `Method_Scenario_Expected`, no mocking library. Cover: happy path, refusal path,
non-positive input, null service, event raised once with the new value.

## 5. View

If a view consumes it, continue with `/new-view` (method injection, `OnEnable`/`OnDisable` wiring).

## 6. Verify and document

`/verify`, then `/write-doc <name>`: a new system always gets `docs/<name>.md` and a lookup row.
Report the files created and the scopes changed.
