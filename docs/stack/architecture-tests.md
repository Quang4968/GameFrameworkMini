# Architecture tests

`Scripts/Tests/EditMode/Architecture/` is the executable half of `.claude/rules/`: it fails on the
breaks the compiler cannot see. Run with `unity command run_tests --mode EditMode`. Only
`ArchitectureConfig.cs` is project-specific; the rest ports unchanged.

## Summary

- Source rules are regexes over every `.cs` under `ArchitectureConfig.ScriptsRoot`, with comments
  and strings stripped first. Assembly rules read the compiled assemblies, so auto-referenced
  packages (R3, UniTask, VContainer) are caught even though no asmdef lists them.
- `ArchitectureBaseline` is a ceiling per (rule, file), plus a line-count ceiling per over-limit
  file. A file may lose hits, never gain one. New code never gets an entry.
- `BaselineHasNoPaidOffDebt` fails when a ceiling is higher than reality: paying debt off means
  lowering or deleting the entry in the same change (the ratchet only turns one way).
- `ArchitectureConfig` must agree with the project card in `CLAUDE.md` (root, prefix, assemblies).

**Start from**: `ArchitectureConfig.cs`, `SourceRules.cs`, `ArchitectureBaseline.cs`.

## Source rules (`SourceRulesTests.NoNewViolations`)

| Id | Catches | Scope | Write instead |
| --- | --- | --- | --- |
| `coroutine` | `IEnumerator`, `StartCoroutine(`, `yield return` | Core, Game | `async UniTask` + `CancellationToken` |
| `tween-library` | `DG.Tweening`, `LeanTween`, `iTween` | Core, Game | `LMotion.Create(...).BindTo...(...).AddTo(gameObject)` |
| `poolboss-static` | `PoolBoss.` | Core, Game (exempt: `PoolServiceFile`) | inject `IObjectPoolService` |
| `singleton` | `static T Instance` | Core, Game | register in a `LifetimeScope`, inject |
| `async-void` | `async void` | Core, Game | `async UniTaskVoid` + `.Forget()`, or return `UniTask` |
| `find-object` | `Find*ObjectByType`, `FindObjectOfType`, `GameObject.Find(` | Core, Game | inject, or a `[SerializeField]` reference |
| `raw-instantiate` | `Instantiate<(`, `Object.Instantiate`, `GameObject.Instantiate` | Core, Game | pool via `IObjectPoolService`; `resolver.Instantiate` for `[Inject]` prefabs |
| `system-clock` | `DateTime[Offset].UtcNow/Now/Today` | Core, Game (exempt: `ClockServiceFile`) | inject `IClockService` |
| `core-unity-api` | `MonoBehaviour.`/`GameObject.`/`PlayerPrefs.`/`Debug.`/`Time.`..., `: MonoBehaviour`, `using VContainer/LitMotion/Cysharp/R3/DarkTonic/MoodBoost/UnityEngine.` | Core only | move to Game behind a `Core/Services` interface |
| `vendor-sdk-namespace` | `using Firebase`, `Unity.Services.LevelPlay`, `UnityEngine.Purchasing`, `Tenjin`, `AdjustSdk`, `GoogleMobileAds`, `TTSDK`, provider namespaces `MoodBoost.Sdk.{Ads.Mediation,Ads.Providers,Tracking.Providers,Haptics.Providers}` | Core, Game | the SDK facade interfaces |
| `interface-prefix` | `interface I_Foo` | all | `IFoo` |
| `enum-prefix` | `enum E_Foo` | all | `Foo` |
| `underscore-field` | a field declared `_foo` | all | `foo` (+ `[FormerlySerializedAs("_foo")]` when serialized) |

`FilesStayUnderLineLimit`: every file `<= MaxFileLines` (300) or its `FileLines` ceiling.

## Assembly rules (`AssemblyDependencyTests`)

| Test | Guarantees |
| --- | --- |
| `CoreReferencesOnlyBclAndUnityMath` | `CoreAssembly` references only the BCL and `CoreAllowedAssemblies` |
| `GameUsesSdkOnlyThroughFacade` | `GameAssembly` references no vendor assembly and no non-facade `MoodBoost.Sdk.*` |
| `SdkNeverReferencesGame` | no `SdkAssemblyPrefix` assembly references a `GameAssemblyPrefix` one |
| `SdkModulesStayFreeOfDiAndGameLibraries` | SDK assemblies outside `SdkDiAssemblies` never reference `SdkForbiddenAssemblyPrefixes` |
| `FacadeAssembliesNeverNameVendors` | facade assemblies and Core never reference `VendorAssemblyPrefixes` |

## `ArchitectureConfig` fields

| Field | Meaning | Where the value comes from |
| --- | --- | --- |
| `ScriptsRoot` | folder scanned for `.cs`, relative to the project root | project card *Scripts* |
| `MaxFileLines` | 300 | fixed |
| `CoreAssembly`, `GameAssembly`, `GameAssemblyPrefix` | `<Game>.Core`, `<Game>.Game`, `<Game>.` | project card *Assemblies* |
| `SdkAssemblyPrefix`, `SdkFacadeAssemblies`, `SdkDiAssemblies`, `SdkForbiddenAssemblyPrefixes` | the SDK surface game code may see | `docs/stack/sdk.md`; update when an SDK assembly is added or removed |
| `VendorAssemblyPrefixes` | third-party SDKs only a provider may name | add a vendor when its provider is added |
| `PoolServiceFile`, `ClockServiceFile` | the two exempt files | fixed unless renamed |
| `RuntimeFolders`, `CoreFolders`, `AllFolders` | rule scopes | fixed |

## Baseline mechanics

- `Violations[ruleId][path] = ceiling`; `FileLines[path] = ceiling`. Paths are relative to
  `ScriptsRoot` with forward slashes.
- A failing `NoNewViolations` names the file, the hit count, the ceiling and the line numbers. Fix
  the code. The one legitimate reason to add an entry: code written on a parallel branch before the
  tests existed and merged afterwards — say so in a comment next to the entry.
- A failing `BaselineHasNoPaidOffDebt` names the entry to lower or delete; do it in the same change.
- A new project starts with both dictionaries empty.

## Adding a rule

Append a `new SourceRule(id, regex, fix, scope[, exemptFiles])` to `SourceRules.All`. The `fix` text
is what the failure prints, so it names the replacement, not the sin. Run the suite: every existing
hit becomes a baseline entry in the same commit, or the code is fixed.
