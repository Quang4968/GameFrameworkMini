# playable-ads changelog

Framework only (engine, tools, workflow). A game's own history lives in its `Playables/CHANGELOG.md`.

## 1.5.1

- Fix: the MRAID adapter calls the global `mraid` by name, so the build carries the literal `mraid.open(`
  that upload validators search for (Unity Ads rejected `r.open(...)`); audit checks `mraid.open(`,
  `mraid.getState(` and `'viewableChange'` as text instead of any `mraid` word.

## 1.5.0

- Per-game kit `Playables/_game/` (`@game`) and `_game/template` that `npm run new` copies (`--bare` for the
  engine template); level read from scenario.yaml `level`; tweakable numbers in `tuning`.
- `npm run export`: network files only, all-networks zip, delivery to `<root>/<game>/<pa>/`; reports stay in
  `dist/`. Preview `__qa` driver and `qaHook`. `meta.platforms` gates which store links audit requires.
- Fixes: child scripts resolved with `fileURLToPath` (spaces in the repo path); `mirrorToThree` renders off
  screen; Editor test runs use `--json`.

## 1.4.0

- Rule: a playable reproduces the full gameplay with all its assets. `npm run deps -- <scene>`, art-map `scene`
  and `excluded`; audit FIXes uncovered assets and art the project marks not releasable.
- Pull converts Unity prefabs and Mesh `.asset`s into `.mesh` models; toon shader colour-map mode.

## 1.3.0

- `_engine/three`: Three.js inside the Pixi stage, `mirrorToThree`, in-memory FBX, stepped toon shader with
  outline and clipping; FBX/GLB model assets; Ogg kept without ffmpeg.

## 1.2.0

- Rule: the reference ad gives the flow; art, fonts, colours and strings come from the Unity project via
  `Playables/art-map.yaml`; `'@key'` texts; pivots and 9-slices from importer settings; asset provenance in
  audit; `npm run guid`; fonts, rich text, nine-slice, hand frames and ripple in the engine.

## 1.1.0

- Engine: `IdleTimer`, double-tap hand, `ctaButton`, tween `onUpdate`; known failures for the hidden preview.

## 1.0.0

- First version: Pixi engine with AppLovin / Unity Ads (MRAID), Mintegral and Google Ads adapters, one inlined
  HTML per network (zip for Google), size and hook audit, preview with a fake host, sprite export from the Editor.
