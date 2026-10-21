# build-tiktok changelog

## 1.3.0 - 2026-10-21

### Changed
- The skill and the SDK live in the project: the SDK submodule is gone, its TikTok parts are in `<Root>/Sdk/`,
  and this folder is no longer a link. Mobile-define and `MobileSdkBuildFilter` rows reworded for a project
  that carries no mobile SDK.

## 1.2.0 - 2026-10-08

### Changed
- The skill moves into the MoodBoost SDK (`Assets/SDK/.claude/skills/build-tiktok/`); a game links it as
  `.claude/skills/build-tiktok`.
- Game-specific paths become `<Root>` from the game's project card: `<Root>/link.xml`, and `size_audit.cs`
  takes the root in `gameRoot` on its first line.
- Hand-over checklist no longer names one game's prefab or gameplay (`PopupGameplay`, tracks).

## 1.1.0 - 2026-10-08

### Added
- `scripts/size_audit.cs`: read-only size audit run in preflight (managed stripping, `Assets/_Dev/link.xml`,
  IL2CPP code generation, URP post-process data, default render pipeline, uncompressed textures, long audio).
  SKILL.md gains a **Size audit** table with the figures measured on this project.
- `scripts/watch_build.sh`: background watcher with one verdict (`SUCCESS`, `COMMIT_REFUSED`, `FAILED`,
  `NOT_STARTED`, `TIMEOUT`). It counts the errors already in the console first and skips polls a busy Editor
  does not answer, so an earlier build's failure no longer reads as the current one's.
- `scripts/pack_only.cs` and `--pack-only`: rebuilds `tt-minigame/` from an existing `<OutputDir>/webgl`
  (the TTSDK window's Pack WebGL step) without a new WebGL build.
- Hand-over reports sizes (wasm raw size and function count, `.br` files, package total, Build Report
  categories) and checks the top bar against the TikTok capsule and the load time.
- Known failures: stale `tt-minigame.zip` after a repack, runtime failures from managed stripping
  (fix in `Assets/_Dev/link.xml`), `LEVELPLAY_DEPENDENCIES_INSTALLED` coming back on every domain reload
  (harmless).

### Changed
- `--release` also turns the debug symbols off (`symbolMode`) and sets Code Optimization to Disk Size with LTO.
  That setting is per machine, so `trigger_build.cs` applies it on every build.
- `trigger_build.cs` deletes `<OutputDir>/webgl` and `<OutputDir>/webgl.ttsdk-backup-*` before queuing the
  build, refuses a relative `OutputDir` or one inside the project, and reports what it cleared.
- The `Refusing to continue committing ... different physical directory` row now gives the real cause: TTSDK
  records the inode of `<OutputDir>/webgl` when the build starts and the WebGL build can recreate that folder,
  with or without an old output. The define `LEVELPLAY_DEPENDENCIES_INSTALLED` is unrelated. The fix is
  `pack_only.cs`, not deleting the folder by hand and rebuilding.
- Step 2 notes that the TTSDK window can write its own `enableSDKLog` back. The
  `TTSDK_LOG_DEBUG` log line tells what shipped.
- Release builds take 15-30 min with LTO; no `eval`, recompile or tests while one runs.

## 1.0.0 - 2026-10-08

- First version: preflight (target, TTSDK, appid, framework, defines, ad units, compile, mobile-only
  assemblies, vendor samples, template), `scripts/trigger_build.cs` queuing the TTSDK window's Build,
  polling, known failures (UnityLinker unresolved assembly, stripped `MeshCollider`, output rollback, `eval`
  timeouts, silent SDK features), device test checklist, browser preview with `scripts/serve_br.py`.
