# UI from a demo

How a UI is built from its demo image. The steps are `/ui-from-demo`; the hard rules
`.claude/rules/ui-from-demo.md`.

## Summary

- **The demo is the only visual source** (a popup's frame is the one the game's first popup took from its demo). A copied
  sibling brings its button ring, text layers and materials; alpha 0 leaves a hidden raycast target.
- **Demo px to canvas units** = px × reference width / demo width (card: `Canvas reference`).
- **Numbers come from `scan` and `bbox`**: `Read` downscales images, eyeballed sizes are off by tens.
- **Text style lives in shared TMP presets**: match `presets.py` colours and offsets, never names.
- **A button is body + optional border ring + label + icons**; what the demo lacks is deleted.
- **A missing sprite is a question**, never a scaled look-alike or a dropped slot.
- **One layout number, one place**: the prefab, or the SettingsSO a layout script reads.
- **Nothing is shown before the compare**: `side` + zoomed pairs, each spec row within ±4 units.
- The owning system's doc keeps the demo path, the preset per text role and the sprite per button.

**Start from**: `.claude/skills/ui-from-demo/SKILL.md`, `.claude/skills/ui-from-demo/scripts/uiref.py`.

## Tools

`python3 .claude/skills/ui-from-demo/scripts/uiref.py <command> ... [--ref <width>] [--out <png>]`
needs only python3, numpy and macOS `sips`. It resamples every image to `--ref` px wide first, so all
input and output is in canvas units. Images it writes go to `Temp/uiref/` in the project and their
path is printed; `Read` that path. `Temp/` is gitignored and cleared when the Editor quits;
`Temp/uiref/` is the one folder in it the path guard lets shell commands write and delete
(`!Temp/uiref/` in `.claude/hooks/protected-paths.txt`).

| Command | Answers | Notes |
| --- | --- | --- |
| `info <img>` | size, scale to canvas | a 1078-wide demo scales ×1.0019 |
| `grid <img> [--crop x,y,w,h --zoom 4 --step 10]` | where things are, by eye | full image at step 50 to name elements; zoomed crop to judge a text or a corner |
| `scan <img> --row y` / `--col x` | exact edges, gaps, widths | `--start/--end` narrow the line; `--tol` merges gradients; `--min 1` keeps 1-unit runs |
| `bbox <img> --crop x,y,w,h [--bg #rrggbb]` | box of an item, occupied column/row spans | the background defaults to the crop's top-left pixel; spans split items in a row |
| `pick <img> x,y ...` | colours | face, outline, shadow, button fill |
| `side <demo> <capture> [--crop --zoom] [--shift=dx,dy] [--diff]` | the compare | demo left, capture middle, per-pixel difference ×3 right; `--shift` moves the capture's crop onto an element that moved (write it with `=` when negative) |

`presets.py <folder>` prints one row per TMP material: face colour, outline width and colour,
underlay colour, offset, dilate, softness.

### Reading a scan

A run of 1–3 units between two colours is anti-aliasing: the edge sits in its middle. A card reads
as outer colour → border band → inner fill → border band → outer colour; the border band's width is
the 9-slice border to reproduce. A gradient surface splits into several runs of close colours; raise
`--tol` until it is one.

### Reading a text

`scan --col` through a vertical stroke of a capital (`--min 1`), then `scan --row` through it:

```
y 760-762 (3) #569606   outline above
y 764-793 (30) #ffffff  face: cap height 30
y 795-796 (2) #569606   outline below
y 797-798 (2) #3c6805   darker band below only → drop shadow (underlay or a second text layer)
```

- Cap height → font size: `fontSize = cap height × m_PointSize / m_CapLine` (the font asset's face
  info; the card's `TMP presets` row gives the ratio). Auto-size texts are measured at their longest
  content.
- Outline present when a band of another colour wraps the face on **all** sides; its width in units
  against the cap height says thin or thick. A band on one side only is a shadow, and its side gives
  the offset direction.
- Bold: compare stroke width to cap height with a text known to be regular in the same font.
- Match face, outline and shadow colours to a `presets.py` row, not to a preset's name: a `-Shadow`
  preset may carry no underlay at all. No row within a few units per channel → ask. A darker copy
  of the whole glyph behind the face is the two-layer trick (face text + shadow text offset down):
  two rows in the text table, each with its own preset.

## Spec tables

Written in the plan; the user approves them before the build.

| Element | Parent | x, y, w, h | Anchor / pivot | Gap to next | Pattern | Native / stretched | Order | Lives in |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |

| Text role | Sample | Cap → size | Face | Outline | Underlay / shadow | Bold | Case | Align | Preset |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |

| Button | Body sprite | Border ring | Size | Label (text role) | Icons + offsets | Shadow | States |
| --- | --- | --- | --- | --- | --- | --- | --- |

Plus the asset list (element → sprite, native size, 9-slice border) and the open questions.

## Buttons

- Body: the pill or panel sprite; its colour comes from the sprite, not from an `Image` tint, unless
  the demo shows a tint of an existing sprite.
- Border ring: a separate sprite around the body in many existing buttons. Zoom the demo's edge:
  one fill with a highlight, or a distinct darker ring of constant width. No ring in the demo → the
  node does not exist.
- Label and icons are measured relative to the button's box (`bbox` inside the button crop): a lock
  or a price sits at a fixed offset from the right edge, not centred with the text.
- States the demo draws (free, priced, locked, claimed) each get a row; a state the demo does not
  draw is a question, not an invention.

## Compare

1. Capture in Play mode at the card's portrait size with `--source screen` (overlay UI is missing from
   camera captures), in the state the demo shows. A popup that auto-opened on the way shows through
   the dim; close it first.
2. `side <demo> <capture> --diff`: the diff panel lights up every offset; a whole element shifted
   shows as a double outline. Live data the demo does not show (other rewards, prices, timers) lights
   it up too, so the diff says *where* to look and `scan` says *by how much*.
3. One `side --crop <region> --zoom 3` per card, text role and button; the zoom is where outline,
   underlay and ring differences show. When `scan` found the element moved, `--shift=dx,dy` lines
   the capture's crop up on it, so its style is judged apart from its position.
4. Each spec row: ✓ or ✗ with the numbers from `scan` on the same line of both images. Size and
   position ±4 units; sprite, preset, presence and state exact.
5. A round fixes every ✗ before the next capture. After three rounds the rows still ✗ go to the user
   with the image, not into a fourth round.

## Traps

- **Stale prefab copy**: an edit saved without a reimport before and after is overwritten on the next
  domain reload or Play; a fix that "reverts itself" is this (`unity-editor-cli.md`).
- **Demo replaced mid-task**: the spec is re-measured from step 1; patching the old spec towards the
  new demo mixes both.
- **Fixing size without re-checking neighbours**: a bigger icon pushes its label out of the card; the
  compare re-walks every row of the region, not only the one changed.
- **Two demos in one folder** (`*_demo`, `*_demo2`, `* copy`): ask which one is current.
- **Path guard**: `Temp/` is protected, so a shell command that names `Assets/Temp/` (the capture)
  and also runs `rm`, `git checkout` or a redirect is blocked whole; keep `uiref.py` calls on a
  capture in a command of their own. Only `Temp/uiref/` is carved out, and never through `..`.
