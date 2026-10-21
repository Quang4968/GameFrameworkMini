---
paths:
  - "**/Scripts/Core/**/*.cs"
---
# Core

- Pure C#: no `MonoBehaviour`, `ScriptableObject`, `GameObject`, `Transform`, `PlayerPrefs`,
  `Debug`, `Time`, `Resources`, `Input`; no VContainer, UniTask, LitMotion, R3 or SDK. Only the BCL
  and the math types (`Vector2Int`, `Vector3Int`, `Vector3`, `Mathf`).
- Time comes through an injected `IClockService`; rules never read `DateTime` themselves, so they
  are testable with a fake clock.
- `GameLogic/` holds pure functions; `UseCases/` holds one complete action each. A service
  interface only holds state and announces change with `event Action<T>`; it carries no rules.
- Every Core rule gets an EditMode test in `Scripts/Tests/EditMode/Core/` with fake services.
