---
name: new-view
description: Scaffold a Unity view the project way - [Inject] Construct, OnEnable/OnDisable service-event wiring, R3 ReactiveProperty for view-private state tied to destroyCancellationToken, LitMotion with a cancelled MotionHandle, pooled effects. Use for any new MonoBehaviour under Game/Views or Game/Animation.
argument-hint: <Name> [Views/<Folder> | Animation]
---

# /new-view $ARGUMENTS

Templates in `${CLAUDE_SKILL_DIR}/templates/`; patterns explained in
`docs/stack/reactive-and-motion.md`. `<Game>` and the scripts root come from the project card.

## 1. Classify, plan, stop

Read the owning system's doc (`limit: 20`) and decide which of the three kinds this is:

| Kind | Template | Registration | Built by |
| --- | --- | --- | --- |
| Scene view (lives in a scene) | `View.cs.tmpl` | `[SerializeField]` on the scope + `if (x != null) builder.RegisterComponent(x)`; `RegisterComponentInHierarchy<T>` only when it can never be absent | the scene |
| Prefab view holding `[Inject]` | `View.cs.tmpl` | dependencies registered in every scope that spawns it | `resolver.Instantiate` |
| Pooled effect | `PooledEffect.cs.tmpl` | none (no injection); prefab listed in the pool catalog from the card, `TimedDespawner` on the prefab | `IObjectPoolService.Spawn` |

Present: file path (`Scripts/Game/Views/<Folder>/<Name>View.cs` or `Scripts/Game/Animation/<Name>.cs`),
the services it consumes, the scopes to edit, the scene or prefab to wire, and — for a view that
draws UI from a demo — the UI spec from `/ui-from-demo` steps 1–6. Stop for the go-ahead.

## 2. Generate

Fill the template. Rules the template already encodes — keep them when you adapt it:

- no rules in the view: input calls a use case, the view redraws from service state;
- `[Inject] public void Construct(...)`; subscribe in `OnEnable`, unsubscribe in `OnDisable`;
- every serialized reference has a `[Tooltip]` saying what breaks when empty, and the code survives null;
- view-private state is a private `ReactiveProperty<T>` exposed as `ReadOnlyReactiveProperty<T>`,
  subscriptions `.RegisterTo(destroyCancellationToken)`, disposed in `OnDestroy`;
- one `MotionHandle` field per animated property, `TryCancel()` before restart, `AddTo(gameObject)`,
  `MotionScheduler.UpdateIgnoreTimeScale` when the view can sit under a popup;
- no `EveryUpdate` / `Timer` (R3 core only): use `ITickable` or `UniTask.Delay(..., ct)`;
- pooled effect: reset in `OnSpawned`, cancel motions and token sources in `OnDespawned`.

Namespace = `<Game>.` + folder path. Keep the file under 300 lines; a presenter that grows rules
is the sign the rules belong in Core.

## 3. Register and wire

- Add the `[SerializeField]` + registration to every scope that hosts the view. A component used in
  several scenes must not depend on a service only one of them registers.
- Wire it into the scene or prefab through `unity command eval` (`AddComponent`, assign references,
  `EditorSceneManager.MarkSceneDirty` + `SaveOpenScenes`, or `PrefabUtility` after a reimport).
  Never edit `.unity` / `.prefab` YAML by hand. UI is built from the approved spec and compared
  with the demo before it is shown (`/ui-from-demo` steps 7–9).
- A new effect prefab is added to the pool catalog through `eval` (`SerializedObject` on the
  catalog asset), and gets a `TimedDespawner`.

## 4. Verify

`/verify`. Add a bullet to the owning system's doc only if the view introduces a cross-file
invariant (a new event contract, a new registration order).
