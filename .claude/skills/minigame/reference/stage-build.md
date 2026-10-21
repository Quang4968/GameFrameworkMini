# Stage `build` - size fixes and the TikTok package

Plan section: Build (package budget, output folder).

## Size fixes (granted in Phase 1)

Run `.claude/skills/build-tiktok/scripts/size_audit.cs` through `eval_file` and apply every `FIX`:
IL2CPP code generation OptimizeSize; `<Root>/link.xml` keeping `<Game>.*`, `MoodBoost.Sdk.*` and
VContainer types before stripping goes to Medium; renderer post-process data cleared; default render
pipeline `Mobile_RPAsset` with desktop quality levels excluded from WebGL; sprite mipmaps off; long
audio mono 0.4; WebGL template `PROJECT:Portrait`. Rerun the audit until it prints no `FIX`.

## Build

`/build-tiktok` (test build first). Its known-failures table covers linker, load and commit
errors; a failure it does not cover is retried once after a clean of the output folder, then the
stage fails with the log line. A release build only when the plan asks for it.

## Done when

`tt-minigame/` exists in the output folder, the wasm and total sizes are inside the plan's budget
(or the overrun is logged), and the device test checklist from `/build-tiktok` is copied into the
report.
