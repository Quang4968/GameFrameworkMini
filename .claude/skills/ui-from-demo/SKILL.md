---
name: ui-from-demo
description: Build or restyle UI from its demo image - measure the demo in canvas units, read every text style and button, write a UI spec the user approves, build only from the spec, then compare side by side with a Play-mode capture row by row. Use whenever a popup, screen, HUD or UI prefab is built, rebuilt or visually adjusted.
argument-hint: <feature or prefab>
---

# /ui-from-demo $ARGUMENTS

The demo is the only picture of what to build. This flow exists because the cheap path fails: sizes
typed by eye, a text style or button carried over from a sibling popup, a missing sprite replaced
silently, and a first build shown without being compared. Long form, table templates and traps:
`docs/stack/ui-from-demo.md` (`limit: 20` first). Tools in `${CLAUDE_SKILL_DIR}/scripts/`:
`uiref.py` (grid, scan, bbox, pick, side) and `presets.py` (TMP material table). Every coordinate is a
canvas unit: pass `--ref` = the card's `Canvas reference` width.

## 1. Demo

- Find it with the card's `UI demos` pattern in the feature's texture folder; `uiref.py info` it.
  Two candidates, or a demo that disagrees with the sprites delivered → ask which is current. No
  demo → ask; another screen of the game is never the reference.
- `uiref.py grid <demo>` and `Read` the result: name every element, top to bottom.

## 2. Assets

For each element, the sprite that draws it (feature folder first, then the card's shared UI folders):
native size and 9-slice border (`spriteBorder` in its importer, read-only). A sprite the demo needs
and the project lacks is listed and asked for — never dropped, borrowed from a sibling or faked by
scaling a look-alike.

## 3. Layout table

Measure, never estimate: `scan --row` / `--col` through each band of elements for exact edges and
gaps, `bbox --crop` per item, `grid --crop --zoom 3 --step 10` where the eye must decide. One row per
element: parent, x/y/w/h, anchor and pivot, gap to its neighbour, repeat pattern (rows × columns,
spacing), native size or stretched, draw order, and where the number lives — the prefab, or the
SettingsSO a layout script reads. One number, one place.

## 4. Text table

Every distinct text style gets a zoomed crop (`grid --crop --zoom 4 --step 10`), then `pick` or
`scan` across one glyph for face, outline and shadow colours. One row per style: sample content, cap
height → font size, face colour, outline (none / colour / thickness), underlay or shadow (none /
colour / direction), Bold, case, alignment, auto-size, and the preset from
`presets.py <the card's TMP presets folder>` whose colours and offset agree — read the table, not the
preset's name. A face text over an offset copy is two layers, written as two rows. No preset agrees →
ask before creating one; never a per-text material override.

## 5. Button table

Each button is read from a zoomed crop of the demo: body sprite, **border ring around the body or
none**, size, corner shape, label (a text-table row), icons inside (lock, currency) with offsets,
drop shadow, and every state the demo shows (free / priced / locked / claimed). Match the body to
the card's `Button sprites`; a button in another popup is not a reference.

## 6. Spec, stop

Present the asset list, the three tables and the open questions with the plan — or as a gate of
their own when the plan is already approved — and **stop for the user's go-ahead**. A new demo
mid-task restarts at step 1.

## 7. Build

- Only numbers from the spec; one the spec lacks is measured and added to the spec first.
- Nodes copied from another prefab are scaffolding: every property is set from the spec, and what
  the demo does not show (a border ring, a text layer, an icon) is **deleted** — never alpha 0,
  never disabled.
- Prefab and scene edits follow `.claude/rules/unity-editor-cli.md` (reimport before and after
  saving a prefab; save the open scene before a recompile).

## 8. Compare

1. Bracket Play mode as `.claude/skills/playtest/SKILL.md` steps 1 and 3 do: snapshot and restore
   the card's play-testing keys, revert the TMP atlas.
2. Put the UI in the state the demo shows (a debug window or public method through `eval`); close
   popups that opened on the way, they show through the dim. Then `capture_game_view --source
   screen` at the card's portrait size, `--save_path "Temp/<label>.png"` (it lands at
   `Assets/Temp/<label>.png`). Keep every command naming that path free of `rm`, `git checkout` and
   redirects: the path guard blocks the whole command.
3. `uiref.py side <demo> Assets/Temp/<label>.png --diff` for the whole screen, then
   `side --crop <region> --zoom 3` for each region of the spec: every card, text style and button.
   An element that moved gets `--shift=<dx>,<dy>` so its style is compared against itself.
4. Walk every spec row and mark ✓ or ✗ with numbers: `scan` the same row or column in both images.
   Position and size within ±4 canvas units; sprite, text style, presence and states exact. Live
   data the demo does not show (other rewards, prices) is listed as content, not marked ✗.
5. Fix every ✗ and capture again. After three rounds, stop and report the rows still ✗ rather than
   looping.

## 9. Report

- Attach the full `side` image and the ✓/✗ checklist to the report, with any question from step 2
  or 6 still open.
- The owning system's doc gets a short UI section: the demo path, the preset per text role, the
  sprite per button. The next change starts from it.
- Delete the captures (`eval` `UnityEditor.AssetDatabase.DeleteAsset("Assets/Temp/<label>.png")`).
  `uiref.py` writes to `Temp/uiref/`, carved out of the path guard: Unity clears it when the Editor
  quits, and a single image goes with `rm Temp/uiref/<file>.png`.
