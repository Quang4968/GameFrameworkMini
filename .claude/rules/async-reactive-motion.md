---
paths:
  - "**/Scripts/Game/**/*.cs"
  - "**/Scripts/Editor/**/*.cs"
---
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
