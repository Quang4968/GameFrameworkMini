# Playable ads

HTML5 playable ads built next to the Unity project in `Playables/`, looking like the game and playing the flow
of a storyboard, for AppLovin, Unity Ads, Mintegral and Google Ads. Read before touching `Playables/` or
`/playable-ads`.

## Summary

- **Full gameplay, all assets**: a playable plays the game's flow as the game does, every effect, model and
  sound included; `npm run deps -- <scene>` + art-map `excluded` prove nothing the scene uses is left out
  silently (audit FIXes gaps, and art the project marks prototype-only / do-not-ship).
- **The game's look, the reference's flow**: a reference ad gives beats and timing only. Every image, font,
  colour and string comes from the Unity project through **`Playables/art-map.yaml`** (role -> texture, plus the
  tints, sizes and fonts the game's prefabs/SOs set) and the game's localization (`'@key'` texts). `audit.mjs`
  FIXes any bundled file without a source in `Assets/` (`assets/manifest.json`, written by `npm run pull`).
- **Not Unity**: a playable is TypeScript + PixiJS v8 bundled by Vite; Unity WebGL is 8-30MB against a 5MB
  limit. The project is a read-only source; `assets/` in a playable is generated and git-ignored.
- **Source pixels**: `sprite()` / `nineSlice()` apply the Unity pivot and 9-slice border read from the texture
  importer settings and undo any downscale (`density`), so code measures as the game does (e.g. a rail sprite's source width = one cell).
- **One file per network, everything inlined** (base64): `tools/build.mjs` writes `dist/<network>/index.html`
  (Google: plus a zip). A second file, a CDN script or a `fetch` of an asset gets the creative rejected.
- **`Playables/networks.json` is the only place** limits, CTA calls and host rules live; `audit.mjs` and
  `build.mjs` read it. A spec change updates the entry, its `source` and `verifiedAt` together.
- **Budget**: target `targetBytes` (3MB), hard `maxBytes` (5MB). Engine + Pixi ~700KB; assets weigh +33% once
  base64'd, so `assets/opt` stays under ~1.7MB (`npm run assets` prints the line).
- **The network is fixed at compile time** (`__NETWORK__`); game code talks only to `AdNetwork`
  (`_engine/sdk/host.ts`). Calling `mraid`, `ExitApi` or `window.install` directly breaks the other networks.
- **Nothing plays before `ad.ready()`** and nothing is audible or timed before `ctx.firstTouch`; both are wired
  in `_engine/core/app.ts`. The store opens only from a tap through `ctx.ad.cta()`.
- **Game time only**: `ctx.tween.to/wait/loop` stop with the ad's pause; `setTimeout` and
  `requestAnimationFrame` keep running while the ad is hidden.
- **Beats mirror the storyboard**: `scenario.yaml` `beats[].id` = `Beat.id` in `src/game/beats.ts`; the last
  is `endcard`, which `tuning.maxPlaySec` after the first touch forces.
- **Layers**: `_engine/`, `_template/`, `networks.json` are framework (every game); `art-map.yaml` and the game
  kit `_game/` (shared modules + `template/` that `npm run new` copies) are per game; `Playables/<playable>/`
  is one ad (scenario, refs, its flow). The level and every tweakable number live in scenario.yaml.

**Start from**: `Playables/art-map.yaml`, `Playables/_engine/core/assets.ts`, `Playables/networks.json`.

## Flow

```
prefabs / SOs / views ──(once per game)──▶ art-map.yaml (roles, colours, fonts, strings, layout)
reference ad refs (flow) ──/playable-ads scenario──▶ scenario.yaml ──user approves──▶ src/game/*.ts
                              │ texts '@key'   assets.use / fonts / data
Assets/... ──npm run pull──▶ assets/raw + manifest.json (source, pivot, 9-slice)
           ──npm run assets──▶ assets/opt (webp/jpg, fonts, mono mp3) + density
src + _engine + assets/opt + runtimeConfig (scenario + art map + strings + manifest) ──build──▶ dist/<network>/
dist ──npm run audit──▶ OK/WARN/FIX (size, hooks, provenance)   dist ──npm run preview──▶ fake host
```

## Layout

| Path | Owns |
| --- | --- |
| `Playables/art-map.yaml` | per game: `export` (`root`, `game`: delivery folder `<root>/<game>/<meta.pa>/`), `scene` (gameplay scene), `excluded` (scene assets an ad does not show, with reasons), `strings` (localization file), `layout` (px per unit, board offsets), `colors`, `fonts` (key -> ttf + family), `roles` (name -> `file` + compression + the game's params for it) |
| `Playables/networks.json` | per network: `format`, `maxBytes`, `maxFiles`, `cta`, `requires` (hooks audit greps for), `rules`, `test` tool, `source` |
| `_engine/core/` | `app.ts` boot + `PlayableContext`, `assets.ts` (bundle glob, `sprite`, `nineSlice`, `role`, `color`, `font`, font loading), `rich-text.ts` (TMP `<color>` tags), `tween.ts`, `audio.ts`, `hand.ts` (frames + ripple), `idle.ts`, `end-card.ts` |
| `_engine/three/` | optional 3D: `stage.ts` (Three.js rendered into a Pixi sprite, camera matching the Pixi root, `loadModel` for FBX, `loadTexture`), `unity-model.ts` (prefab/Mesh `.mesh` models in Unity space, `toonFromMaterial`), `mirror.ts` (Pixi subtree -> quads and billboards on z = 0), `toon.ts` (stepped toon + colour map + outline hull, clipping) |
| `_engine/sdk/` | `adapter.ts` picks the network; `networks/mraid.ts` (AppLovin, Unity), `mintegral.ts`, `google.ts`, `preview.ts` |
| `_engine/scenario/beats.ts` | runs beats in order, cuts to `endcard` on `maxPlaySec` |
| `_engine/tools/` | `new`, `guid` (Unity guid <-> path, who uses a texture), `deps` (every asset a scene reaches, art-map coverage), `export` (network files only, zipped and delivered to `<root>/<game>/<pa>/`; reports stay in `dist/`), `lib/unity.mjs` (Unity YAML: guids, Mesh assets, prefab parts, materials), `pull-assets`, `optimize-assets`, `dev`, `build`, `audit`, `preview` (all `npm run <name> -- ...`); `lib/art.mjs` reads the art map, strings, importer settings, manifest |
| `_template/` | the engine's bare "tap to win" playable (`npm run new -- x --bare`, or a game without a kit) |
| `_game/` | per game: the kit (`@game/...`: the game's level model, layout, board, scenery, HUD and win sequence) and `template/` (scenario with every asset + a working flow) that `npm run new` copies |
| `.claude/skills/playable-ads/` | the workflow, known failures, `scripts/export_sprites.cs` |

## scenario.yaml

| Block | Read by | Holds |
| --- | --- | --- |
| `meta` | runtime + build | `name`, `pa` (ad id, the delivery sub-folder), `platforms` (`android` / `ios`: store links required only for these), `orientation` (`portrait`/`landscape`/`both`), `design` resolution, `store.ios/android` (`background` defaults to art-map `colors.background`) |
| `tuning` | runtime | `maxPlaySec`, `idleHintSec`, and every position/timing a reviewer may nudge (`perfectY`, ...); code keeps the game default when absent |
| `texts` | runtime | every visible string: `'@key'` / `'@key\|arg0'` from the game's localization, a literal only when the game has none |
| `beats` | author | `id`, `ref` screenshot (flow), `shows`, `waits_for`, `on_idle` |
| `level` | runtime (game kit) | structured level the kit validates at boot (`useLevel`): `size`, `rows`, `cols`, `path` (travel order), `givens`, `entry`, `exit` |
| `assets` | pull + assets tools | `use` (art-map roles), `fonts` (art-map keys), `unity` (one-off `name: path[#Sprite]`), `data`, `overrides` |
| `open_questions` | reviewer | what the images leave open; empty before build |

## Networks

| Network | Deliverable | CTA | Host lifecycle |
| --- | --- | --- | --- |
| AppLovin | single HTML ≤5MB | `mraid.open(store)` | MRAID 2.0: wait `ready`; timer/audio after first touch |
| Unity Ads | single HTML ≤5MB | `mraid.open(store)` | MRAID 3.0: start after `viewableChange(true)`; no XHR |
| Mintegral | single HTML ≤5MB | `window.install()` | `gameReady()` → host `gameStart()`; `gameEnd()` on end card (names from third-party docs: confirm in Mintegral's test tool) |
| Google Ads | zip ≤5MB, ≤512 files | `ExitApi.exit()` | `exitapi.js` + `ad.orientation` meta in head; no `localStorage` |

Adding a network: an entry in `networks.json`, a file in `_engine/sdk/networks/`, a branch in `adapter.ts`,
its hooks in `HOOKS` of `audit.mjs`, a mock in `preview.mjs`, a row here.

## Traps

- An imported `.css` file is emitted, not inlined, and the build drops it with a WARN: style in code.
- Pixi decodes images in workers started from blob URLs, which ad webviews block; `core/assets.ts` turns
  workers off. Load textures only through `texture(name)`.
- The preview in a hidden browser pane renders nothing: `requestAnimationFrame` is paused, not the game.
- Opaque full-screen art as PNG/WebP-with-alpha is the usual budget breaker: `format: jpg` on the role.
- Low-contrast art (grass tiles, paper) loses its grain at WebP quality 80: raise `quality` on that role.
- A role's numbers describe the game's own drawing (cells, source pixels), not the ad's screen: convert
  through `layout.pxPerUnit` and the cell size, in one layout module of the game kit.
- 3D is opt-in (~600KB): only a playable importing `_engine/three` pays for it. It renders on the Pixi
  ticker, so it pauses with the ad; Pixi UI layers above it because the 3D frame is a Pixi sprite.
- An FBX keeps the game's axes: read the prefab's child rotation and confirm the front from the geometry.
- A `.mesh` model stays in Unity's left-handed space: place it under a parent scaled (1, 1, -1) and build its
  transform with Unity's rules (`Quaternion.Euler` is Euler order 'YXZ' in Three).
- The preview's `__qa` driver works in design coordinates and ticks by hand when the pane is hidden; game
  code registers aim points with `qaHook` (`_engine/core/qa.ts`).
- `npm run dev` uses the `preview` network: a CTA there proves nothing about the real hosts; audit a build.
