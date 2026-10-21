---
name: playable-ads
description: Make an HTML5 playable ad that plays the game's full gameplay with all its assets - keep the game's art map (every art, model, sound, font, colour and string the gameplay scene uses, from the Unity project) current and complete, turn a storyboard (reference ad screenshots for the flow) into an approved scenario.yaml, pull and compress the game's own assets, write the gameplay beats on the shared Pixi engine, build one file per network (AppLovin, Unity Ads, Mintegral, Google Ads), audit size, host hooks and asset provenance, preview with a fake host and hand over a size table plus network test checklist. Use when asked to make, change, rebuild, shrink or check a playable ad.
argument-hint: [artmap|new|scenario|assets|build|audit|export] <playable>
---

# /playable-ads $ARGUMENTS

Skill version 1.5.1 (history in `CHANGELOG.md` next to this file). Long form, `scenario.yaml` format and the
network table: `docs/stack/playable-ads.md` (read its first 20 lines before step 2).

Everything runs from `Playables/` through npm: `npm run <tool> -- <playable>`. Node 20+ must be installed
(`node -v`); on a fresh checkout run `npm install` once in `Playables/`. With no sub-command, run every step
in order and stop at each **Stop**.

**The rules:**
- **Full gameplay, all assets.** A playable reproduces the game's gameplay as the game plays it, start to
  finish: every screen, effect, model, sound and transition of the flow it shows (the win sequence, its 3D
  models and effects included), with the game's own assets and numbers. Nothing the gameplay shows is dropped or
  simplified on the agent's own call; a gap is an open question for the user.
- **The reference gives the flow only.** A reference ad (its link or screenshots) decides
  the beats, timing, what the hand shows and where the CTA sits. Never copy its art, colours, fonts or layout.
- **The project is the only source.** Everything comes from the Unity project through
  `Playables/art-map.yaml`. Audit FIXes a bundled file with no source in `Assets/`, an art map that does not
  cover every asset of the gameplay scene, and art the project marks not releasable (a `README_PROTOTYPE_ONLY`
  / `DO_NOT_SHIP` file in its folder): such a build is for internal review only, and the hand-over says so.

## 0. artmap (once per game; again when the game changes)

`Playables/art-map.yaml` is the game's complete inventory for ads: `scene` (the gameplay scene), `strings`
(localization), `layout`, `colors`, `fonts`, `roles` (each visual/audio/model asset with the numbers the game
uses for it) and `excluded` (scene assets an ad does not show, each with its reason). Build it from the code
and data that draw each thing, never from file names or the reference:

1. `npm run deps -- <scene>` lists every texture, mesh, model, sound and font the gameplay scene reaches
   through prefabs, ScriptableObjects and materials, marked `ok`, `skip` (excluded) or `MISSING`. Work until it
   prints 0 MISSING: each asset becomes a role or an `excluded` line with a reason a reviewer can check
   (e.g. "lives HUD, hidden on the level the ad shows"). Assets loaded by path/address from code are not
   seen: read the views of the flow for those.
2. For each element, read the view / prefab / SO that draws it (`Game/Views`, `Data/*.asset`, prefabs) and
   copy its numbers into the role: sizes in cells, tints, timings, easing, offsets, counts, speeds, sound
   volume. `npm run guid -- <guid>` resolves a reference; `npm run guid -- Assets/<path>` lists its users.
3. Role files can be textures, sounds, fonts, FBX/GLB models, Unity prefabs and Mesh `.asset`s (pull converts
   prefabs and meshes, with their in-prefab transforms and material values, into `.mesh` models plus
   textures). Pivots and 9-slice borders come from the importer settings at pull time.
4. Shaders are ported by hand into `_engine/three` (or game code) from the shader source, keeping its
   parameters as role params (see `toon.ts` for the Toon example).
5. When the Editor is running, capture the real flow (`/playtest --shot`) into `scenario/refs/game-*.png`; the
   preview is compared against these.

## 1. new

`npm run new -- <playable>` (kebab-case, e.g. `tracks-hook-v1`). It copies the game's template
(`Playables/_game/template`: the game kit wired in, every asset the gameplay shows, a working flow from the
last playable) or, for a game without a kit yet or with `--bare`, the engine's `_template`. Ask the user to
drop the storyboard into `Playables/<playable>/scenario/refs/` in play order (`01-hook.png`, ...). If they give
a reference ad link, open it in the browser pane at the mobile preset, play it through and save a screenshot
per beat into `refs/` (flow reference only).

## 2. scenario

1. Read every image in `scenario/refs/` (Read tool shows images) and the brief.
2. Rewrite `scenario/scenario.yaml` from the template's shape: `meta` (orientation, `platforms` the ad is
   bought for, `[android]` / `[ios]` / both, and a store link for each; ask when the brief does not say), `tuning`,
   `texts`, one `beats` entry per screen (`id`, `ref`, `shows`, `waits_for` / `on_idle`), `level` (the
   structured block the game kit reads at boot: grid, clues, path, givens, entry/exit; check it against the
   game's rules and level data), `assets`.
   Every position, size or timing the user may want to nudge (badge height, delays, speeds) is a `tuning`
   entry read by the code, so a "move it up a bit" is a one-line scenario change, not a code edit.
3. `texts`: `'@key'` (or `'@key|arg0'` for `{0}`) for every string the game already has in its localization
   (`art-map.yaml` `strings`); grep the file for the meaning, not the reference's wording. A literal only when
   the game has no such string (audit WARNs on each).
4. `assets.use`: every art-map role the flow shows, from the first frame to the end card, including the
   win/lose sequence the game plays (models, effects, sounds); `assets.fonts`: the font keys. A visual
   the art map lacks goes into the art map first (step 0), never as an outside file.
5. Anything the brief and images do not settle goes into `open_questions`, never a guess. The last beat id is
   always `endcard`.
6. **Stop**: show the beats table and the open questions; wait for the user's answers and approval.

## 3. assets

1. `npm run pull -- <playable>`: copies the roles, fonts and `unity` / `data` entries out of `Assets/`, reads
   pivot and 9-slice per texture, writes `assets/manifest.json` (source of every file). A sprite inside a
   sliced sheet (`Path.png#SpriteName` under `assets.unity`) needs the Editor: `unity command editor_status`
   (ready), then `unity command eval_file --file .claude/skills/playable-ads/scripts/export_sprites.cs`.
2. `npm run assets -- <playable>`; a `FIX ... budget` line means lower `maxSize` / `quality` or `format: jpg`
   for opaque art (in the art-map role, or `assets.overrides` for this playable only), then rerun. Low-contrast
   textures (grass, paper) lose their grain at quality 80: raise `quality` on that role.
   Audio needs `ffmpeg` (`brew install ffmpeg`); without it only `.mp3` passes through, uncompressed.
3. `assets/` is generated and git-ignored: the Unity project stays the only source.

## 4. build

1. Start from the template's `src/game/beats.ts` and change only what the new flow needs. Shared pieces live in
   the game kit `Playables/_game/` (import `@game/...`): the game's board, scenery, HUD and its whole win
   sequence (3D camera, models, effects, badge). A fix or feature any later playable of the game
   needs goes into the kit (and `_game/template` when the starting flow changes), never into one playable.
   One `Beat` per `beats` entry, same ids; files under 300 lines. Engine only:
   - art: `sprite(name)` (game pivot, measured in source pixels), `nineSlice(name, w, h)`, `role(name)`,
     `color(name)`, `font(key)`, `RichText` for strings with `<color=#..>` tags;
   - time: `ctx.tween` (never `setTimeout` / `requestAnimationFrame`: they run while the ad is paused),
     `IdleTimer` for idle hints;
   - tutorial: `TutorialHand` with the game's `frames` and `ripple`;
   - store: `showEndCard` with the game's button, `ctx.ad.cta()` only from a tap;
   - sound: `ctx.audio.play(role, { volume })` with the clip and volume the game's audio settings give that
     action (art-map `sfx_*` roles);
   - 3D, when the game shows models or a tilted camera: `_engine/three` (`ThreeStage` inside the Pixi stage,
     `mirrorToThree`, `loadModel` for FBX, `loadUnityModel` for prefab/Mesh `.mesh`, `toonMaterial`);
   - QA: `qaHook(name, fn)` for points a test aims at (the kit registers `cell`).
2. `npm run typecheck`, then `npm run build -- <playable>` (add `--network unity` to iterate on one).

## 5. audit and preview

1. `npm run audit -- <playable>`: every line `OK` (a `WARN` is reported, a `FIX` blocks upload).
2. `npm run preview -- <playable>` in the background; open `http://localhost:5180/<network>/` in the browser
   pane at the mobile preset. Drive it from `javascript_tool` with the preview's `__qa` API, in design
   coordinates: `await __qa.live()` first (keeps frames coming when the pane is hidden), then
   `__qa.doubleTap(...__qa.at('cell', r, c))`, `__qa.drag(points, ms)`, `__qa.wait(ms)`, and
   `__qa.speed(0.0001)` to freeze for a screenshot (`1` resumes). One script plays the whole flow; screenshot
   every beat and compare the flow with the reference refs and the look with the game. Also: key P toggles
   pause/resume; rotate (custom 812x375) when `orientation` is not `portrait`. The overlay logs `gameReady`,
   `gameEnd`, `mraid.open` / `install` / `ExitApi.exit`.
3. `npm run dev -- <playable>` is for iteration with hot reload; the audit result only counts on a build.
4. If C# changed (e.g. `scripts/export_sprites.cs`), check it in the Editor: `unity command eval_file` on it,
   then `unity command run_tests --mode EditMode --json` (the JSON summary is what the Stop hook records).

## 6. Hand over

- `npm run export -- <playable>`: puts only the files the networks take, `<playable>_<network>.html|zip`, into
  `dist/export/`, `dist/<playable>_all-networks.zip` and the team's delivery folder `<root>/<game>/<pa>/`
  (art-map `export: { root, game }`, scenario `meta.pa`; e.g. `/Users/<you>/Playable/<GameName>/PA4/`). Never
  put reports (size report, audit) in the delivery folder or the zip: they stay in `dist/` (`AUDIT.txt`,
  `size-report.md`), and the verdict with its FIX lines goes in the chat. Ask the user for the PA id when
  `meta.pa` is empty; `--to` sends it elsewhere, `PLAYABLE_EXPORT_ROOT` moves the root on another machine.
- In the chat: per network the file to upload, its size and official test tool, and the audit verdict.
- Where the ad departs from the game's own gameplay and why; anything audit marks not releasable (internal
  review only until the game replaces that art); open questions; what the preview could not check (real
  device, store link).

## Known failures

| Symptom | Cause | Fix |
| --- | --- | --- |
| Upload rejected: "Your playable is missing mraid.open()" | the validator greps the file for the literal call; a local alias (`const m = window.mraid; m.open()`) minifies to `r.open()` | call the global by name (`mraid.open(url)`, see `sdk/networks/mraid.ts`); audit checks the literal text per network |
| Black screen on Unity Ads, fine elsewhere | started before `viewableChange(true)` or `mraid` missing | keep `ad.ready()` before play; if the test app reports no `mraid`, set `"includeMraidJs": true` for that network in `networks.json` |
| CTA does nothing on AppLovin / Unity | empty `meta.store` link for a platform in `meta.platforms` | fill it, or drop the platform if the ad is not bought for it; audit flags it; a device of an unlisted platform falls back to the other link |
| Google validator: "missing exit" | CTA not through `ctx.ad.cta()` | route every store tap through `ctx.ad.cta()` |
| Size over limit after adding art | a full-screen PNG with alpha | `format: jpg` for opaque art, `maxSize` 1024 or less, drop music before SFX |
| Audit: "art map misses N asset(s)" | the gameplay scene uses an asset the art map neither covers nor excludes | `npm run deps -- <scene>`, add roles or `excluded` reasons |
| Audit: "not releasable" | a source sits under a folder the project marks prototype-only | internal review only; the game must replace that art before upload |
| Pull: "not a Mesh .asset" | the prefab's mesh comes from an FBX | add the FBX as a model role instead of the prefab |
| Audit: "not from the Unity project" | a file put into `assets/` by hand | add the art to `Assets/` and the art map, `npm run pull` |
| Art offset or flipped against the game | pivot or flip guessed in code | `sprite()` applies the game pivot; take flips/rotations from the view or SO that draws it |
| Gradient or shade drawn the wrong way | assumed which end of the texture is opaque | read the pixels (`sharp(...).raw()`) before orienting it |
| Text in a fallback font | font key missing from `assets.fonts`, or `font()` key typo | add the key, `npm run pull` |
| Textures blank in a network webview | blob/worker decode blocked | already off in `core/assets.ts`; load textures only through the engine |
| Preview stays an empty background, no error | the browser pane is hidden, so `requestAnimationFrame` never fires | show the pane (Cmd+Shift+B), or tick by hand for a scripted check: `setInterval(() => __PIXI_APP__.ticker.update(performance.now()), 16)` |
| A synthetic drag in the preview does nothing | `computer` drags go in a straight line | `__qa.drag(points)` in the preview |
| The user asks to move or retime something | the value was hard-coded | make it a `tuning` entry with the current value as default, then change the scenario |
| A tool's child script silently does nothing | `new URL(import.meta.url).pathname` keeps `%20` for spaces in the repo path | `fileURLToPath(import.meta.url)` |
| The Stop hook keeps asking for tests that passed | the run printed the human summary, which the hook cannot read | `unity command run_tests --mode EditMode --json` |
| 3D model renders black | its map never loaded (a URL modifier on the shared `DefaultLoadingManager`, or a texture cloned before load) | `loadModel` uses its own manager; pass textures straight into uniforms |
| 3D model faces backwards or lies on its side | front guessed from the mesh shape | take the front from the game's own attachment data (an effect or child placed ahead of the model's centre), then check against the running game |
| Something flashes for one frame as the scene turns 3D | a render presented while objects were shown for the copy | `mirrorToThree` renders off screen; show copy-only pieces only around that call |
| Ogg sound silent on an iPhone | Safari before 17 has no Ogg | `brew install ffmpeg`, `npm run assets` converts it to mp3 |
| Hint or timer fires while the ad is hidden | wall-clock timer in game code | use `ctx.tween.wait` |

## Rules

- Never commit; never edit under `Assets/` (the Unity project is a read-only source of art, strings and levels).
- Layers: `Playables/_engine/` + `_template/` are the framework (every game); `art-map.yaml` + `_game/` (kit and
  template) are this game; `Playables/<playable>/` is one ad (scenario, refs, its flow). A fix goes into the
  lowest layer that every user of it shares, plus a line in `CHANGELOG.md`.
- `Playables/networks.json` is the only place limits and host rules live; when a network changes its spec,
  update the entry, its `source` and `verifiedAt` together.
