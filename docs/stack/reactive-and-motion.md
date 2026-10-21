# Async, reactive state and motion

Canonical UniTask / R3 / LitMotion / pooled-object patterns for `Game` code. The rules are in
`.claude/rules/async-reactive-motion.md`; this file is the code. `<Game>` and the paths come from
the project card in `CLAUDE.md`. The running example is a fictional `DailyReward` feature.

## Summary

- UniTask for every async path; `CancellationToken` in every signature; `.Forget()` only on
  `UniTaskVoid`. A `MonoBehaviour` uses `destroyCancellationToken`, an entry point owns a CTS.
- R3 is **core only** (NuGet `R3`, no `R3.Unity`): no `AddTo(Component)`, frame operators throw,
  `Timer`/`Interval`/`Delay`/`Debounce` call back **off the main thread**. Tick with `ITickable` or
  `UniTask.Delay`.
- R3 swallows exceptions thrown inside `Subscribe` unless Boot registers
  `ObservableSystem.RegisterUnhandledExceptionHandler(UnityEngine.Debug.LogException)`.
- One bridge per Core event: `Observable.FromEvent` → `ToReadOnlyReactiveProperty(initial)`.
  Private `ReactiveProperty<T>` / `Subject<T>`, public `ReadOnlyReactiveProperty<T>` / `Observable<T>`.
- LitMotion only. One `MotionHandle` field per animated property, `TryCancel()` before restart,
  `AddTo(gameObject)`, `MotionScheduler.UpdateIgnoreTimeScale` when a popup can freeze time.
  `ToUniTask` is off: await with `UniTask.WaitWhile(() => handle.IsActive(), cancellationToken: ct)`.
- Pooled objects reset in `OnSpawned` and cancel everything in `OnDespawned`; `destroyCancellationToken`
  and `AddTo(gameObject)` do nothing on despawn because the object is only deactivated.

**Start from**: `Game/SceneFlow/TransitionSceneLoader.cs`, `Game/Animation/PopInEffect.cs`,
`Game/Animation/FlyingCoin.cs`.

## What the project ships

| Library | How it is installed | Consequence |
| --- | --- | --- |
| UniTask | vendored in `Assets/Plugins/UniTask` | `LITMOTION_SUPPORT_UNITASK` is off → no `ToUniTask()` on a `MotionHandle` |
| R3 1.x | NuGetForUnity, `Assets/Packages/R3.*` | core package only: no `R3.Unity` extensions, no `AddTo(this)`, no `EveryUpdate` |
| LitMotion | UPM git | `LitMotion` + `LitMotion.Extensions` (`BindTo*`) |
| VContainer | UPM git | `IStartable` / `ITickable` / `IDisposable` entry points |

Boot must call once, before any subscription exists:

```csharp
R3.ObservableSystem.RegisterUnhandledExceptionHandler(UnityEngine.Debug.LogException);
```

## (a) Ticking from a pure C# entry point

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;
using <Game>.Core.Services;

public sealed class DailyRewardTicker : IStartable, IDisposable
{
    readonly IDailyRewardService daily;
    readonly CancellationTokenSource cts = new();   // scope disposal cancels it; not a scene token

    public DailyRewardTicker(IDailyRewardService daily) => this.daily = daily;

    public void Start() => TickAsync(cts.Token).Forget();

    async UniTaskVoid TickAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Not Observable.Timer/Interval: R3 core's TimeProvider calls back off the main thread.
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: ct);
            daily.RefreshAvailability();
        }
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
    }
}
```

## (b) Bridging a Core `event Action<T>` once

```csharp
using R3;
using TMPro;
using UnityEngine;
using VContainer;
using <Game>.Core.Services;

public sealed class DailyStreakView : MonoBehaviour
{
    [Tooltip("Label showing the streak. Empty: nothing is drawn, the scene still runs.")]
    [SerializeField] TMP_Text label;

    ReadOnlyReactiveProperty<int> streak;   // the only R3 object that knows the event

    [Inject]
    public void Construct(IDailyRewardService daily)
    {
        // ToReadOnlyReactiveProperty subscribes immediately and keeps the last value,
        // so a late subscriber gets the current streak instead of nothing.
        streak = Observable
            .FromEvent<int>(h => daily.StreakChanged += h, h => daily.StreakChanged -= h)
            .ToReadOnlyReactiveProperty(daily.Streak);
        streak.RegisterTo(destroyCancellationToken);   // unhooks the C# event on destroy
    }

    void Start()
    {
        if (streak == null) { Debug.LogError($"[{nameof(DailyStreakView)}] not injected.", this); return; }
        streak.Subscribe(value => { if (label != null) label.text = value.ToString(); })
              .RegisterTo(destroyCancellationToken);
    }
}
```

## (c) View-owned state

```csharp
using R3;
using UnityEngine;

public sealed class DailyRewardPanel : MonoBehaviour
{
    readonly ReactiveProperty<bool> canClaim = new(false);
    readonly Subject<Unit> claimClicked = new();

    public ReadOnlyReactiveProperty<bool> CanClaim => canClaim;   // never expose the setter
    public Observable<Unit> ClaimClicked => claimClicked;

    public void SetCanClaim(bool value) => canClaim.Value = value;   // notifies only on change

    public void OnClaimButton() => claimClicked.OnNext(Unit.Default);   // wired from the Button

    void OnDestroy()
    {
        canClaim.Dispose();      // completes subscribers so a presenter never holds a dead view
        claimClicked.Dispose();
    }
}
```

## (d) Presenter subscriptions in a `DisposableBag`

```csharp
using System;
using R3;
using VContainer.Unity;
using <Game>.Core.UseCases;

public sealed class DailyRewardPresenter : IStartable, IDisposable
{
    readonly DailyRewardPanel panel;
    readonly ClaimDailyRewardUseCase claim;
    DisposableBag disposables;

    public DailyRewardPresenter(DailyRewardPanel panel, ClaimDailyRewardUseCase claim)
    {
        this.panel = panel;
        this.claim = claim;
    }

    public void Start()
    {
        panel.ClaimClicked.Subscribe(_ => claim.Execute()).AddTo(ref disposables);
    }

    public void Dispose() => disposables.Dispose();   // scope death unsubscribes everything
}
```

## (e) LitMotion handle discipline and awaiting

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public sealed class DailyRewardPanelAnimator : MonoBehaviour
{
    [SerializeField, Min(0.01f)] float showDuration = 0.25f;

    MotionHandle show;   // one field per animated property

    public async UniTask ShowAsync(CancellationToken ct)
    {
        show.TryCancel();   // never stack a second motion on the same target
        show = LMotion.Create(0f, 1f, showDuration)
            .WithEase(Ease.OutBack)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)   // popups set timeScale = 0
            .BindToLocalScaleXYZ(transform)
            .AddTo(gameObject);   // stops on Destroy; does nothing on pool despawn

        try
        {
            await UniTask.WaitWhile(() => show.IsActive(), cancellationToken: ct);
        }
        catch (System.OperationCanceledException)
        {
            show.TryCancel();   // the token dying must also stop the tween
            throw;
        }
    }

    void OnDestroy() => show.TryCancel();
}
```

## (f) Pooled effect: reset on spawn, cancel on despawn

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

// Lifetime comes from TimedDespawner on the same prefab; this component never despawns itself.
public sealed class ClaimBurstVfx : MonoBehaviour
{
    [SerializeField] ParticleSystem particles;
    [SerializeField, Min(0.01f)] float punchDuration = 0.2f;

    MotionHandle punch;
    CancellationTokenSource spawnLifetime;   // destroyCancellationToken never fires on despawn

    // Pool Boss broadcasts both; private and parameterless on purpose.
    void OnSpawned()
    {
        spawnLifetime = new CancellationTokenSource();
        transform.localScale = Vector3.zero;   // reset everything the previous life touched
        if (particles != null) particles.Play(true);
        punch = LMotion.Create(0f, 1f, punchDuration)
            .WithEase(Ease.OutBack)
            .BindToLocalScaleXYZ(transform)
            .AddTo(gameObject);
        FadeAsync(spawnLifetime.Token).Forget();
    }

    void OnDespawned()
    {
        punch.TryCancel();
        spawnLifetime?.Cancel();
        spawnLifetime?.Dispose();
        spawnLifetime = null;
        if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void OnDestroy() => OnDespawned();   // scene unload while spawned

    async UniTaskVoid FadeAsync(CancellationToken ct)
    {
        await UniTask.Delay(300, cancellationToken: ct);
        // fade-out work here
    }
}
```

## Checklist

- [ ] No `async void`, no coroutine, no `Task`; every async method has a `CancellationToken`
- [ ] Cross-scene work does not listen to a scene token
- [ ] R3 objects are private; public surface is read-only; every subscription is tied to a lifetime
- [ ] No R3 frame/time operators; no R3 in `Core`
- [ ] Every `MotionHandle` is a field, cancelled before restart and on destroy/despawn
- [ ] Motion that must run under a popup uses `UpdateIgnoreTimeScale`
- [ ] Pooled object resets in `OnSpawned`, cancels in `OnDespawned`, prefab carries `TimedDespawner`
