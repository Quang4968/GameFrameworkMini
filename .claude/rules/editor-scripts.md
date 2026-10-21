---
paths:
  - "**/Scripts/Editor/**/*.cs"
---
# Editor scripts

- `MenuItem` and `CreateAssetMenu` paths start with the menu root in the project card (`<Game>/`).
- A debug window works in two modes: Editor not playing → write PlayerPrefs directly; playing → go
  through the scene's service. Writing PlayerPrefs under a running service gets overwritten, because
  the service keeps state in RAM and saves only on change.
- Editor-only hooks into runtime code are `#if UNITY_EDITOR` static fields on the service
  (`SystemClockService.DebugOffset`), never a second code path.
- After editing level or config data, run the matching validator menu item and read
  `unity command console --level error`.
