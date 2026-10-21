# Unity Editor CLI

The running Editor is driven only through `unity command ...` (`com.unity.pipeline`, experimental;
version per `Packages/manifest.json`). Never use `mcp__unityMCP__*`; everything is reachable through a
dedicated command or `eval`. Run from the project root or pass `--project-path`. Long form and
`eval` recipes: `docs/stack/unity-editor-cli.md`.

- Tests: `unity command run_tests --mode EditMode`. The result is in the response; `test_status`
  reports `no_tests` afterwards.
- C# edits: `recompile` → `recompile_status` (poll until idle) → `console --level error`. **Save
  the open scene first**: the domain reload discards unsaved scene edits.
- Screenshots: `capture_game_view --save_path "Temp/shot.png" --width W --height H`. Without
  `--save_path` the image comes back as base64 and floods the context. The PNG lands under `Assets/`
  whatever path is given: remove it with `AssetDatabase.DeleteAsset` in `eval` (takes the `.meta`
  with it; a shell delete of a `.meta` is blocked).
- `--source camera` (default) omits Screen Space-Overlay UI; `--source screen` includes it, needs
  Play mode and the portrait size from the project card (otherwise the canvas comes out squashed).
- `eval` compiles a method body: no `using`, fully-qualify every type
  (`UnityEditor.EditorApplication.ExecuteMenuItem(...)`).
- `eval` is capped at 5 s on the main thread regardless of `--timeout`: chunk heavy work per call.
  A `Main thread operation timed out` while the Editor is busy is retried after `editor_status`.
- `OpenScene` inside `eval` unloads assets loaded earlier in the same call: load after opening.
- Reimport a prefab (`AssetDatabase.ImportAsset`) before editing it through `eval`; a stale
  AssetDatabase copy overwrites the file on the next domain reload.
- Compile-check `#if UNITY_IOS` / `UNITY_ANDROID` code with `AssemblyBuilder` in `eval`; the Editor
  compiles only the active target.
- Play mode rewrites the TMP dynamic font atlas (`* SDF.asset`): revert it before committing.
- Play-testing spends persisted player state (PlayerPrefs lives, boosters, progress — keys in the
  project card): snapshot before, restore after.
- Dedicated commands beat `eval`: `open_scene --path`, `save_scene`, `editor_play` /
  `editor_stop`, `menu --path "<Game>/Item"`, `eval_file --file` for long scripts.
- Long jobs (`build`, `bake_*`, `audit`) take `--detach`; poll `<name>_status`.
- Output is one long JSON line: filter in the shell before reading a large result
  (`get_scene_hierarchy` has no pagination).
