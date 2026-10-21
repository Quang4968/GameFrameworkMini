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
- Game code uses the SDK facade interfaces only (`IAdService`, `ITrackingService`, `IHapticsService`,
  `IRemoteConfigService`), registered in every scope; never a vendor namespace (`TTSDK` lives only
  in the SDK's TikTok provider). Reference: `docs/stack/sdk.md`.
