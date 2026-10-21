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
