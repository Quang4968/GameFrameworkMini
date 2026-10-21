# UI from a demo

Building, rebuilding or restyling any UI (popup, screen, HUD, UI prefab) goes through
`/ui-from-demo`; long form `docs/stack/ui-from-demo.md`. Each rule below stands for a build the user
had to fix by hand.

- The demo image is the **only visual reference**. Another popup or prefab is a wiring reference at
  most: its button border, text layers, materials and sizes are never carried over unchecked. The
  one standing exception is a popup's frame: the game's first popup takes it from its demo, later
  popups reuse it, and a demo that draws another frame is a question in the spec.
- No number by eye: every position, size, gap and font size is measured on the demo in canvas units
  (`uiref.py`) and written in the spec before it is built.
- Every text style is read from a zoomed crop — face colour, outline, underlay or shadow, Bold,
  size, case — and maps to an existing TMP preset, or the user is asked.
- Every button is read from the demo — body, border ring or none, size, label, icons, states. What
  the demo does not show is deleted, never hidden with alpha 0 or a disabled object.
- A sprite the demo needs and the project lacks is asked for, never substituted.
- The user approves the spec (asset list, layout, text and button tables) before the build, and sees
  the build only after a side-by-side compare with the demo passes row by row; the report carries
  that image.
