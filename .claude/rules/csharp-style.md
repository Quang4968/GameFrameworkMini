---
paths:
  - "**/*.cs"
---
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
