#!/usr/bin/env python3
"""List TextMeshPro material presets as one row each: face, outline and underlay.

  presets.py <folder or .mat> [...]

A demo text is matched against this table: pick its face, outline and shadow colours with
`uiref.py pick` on a zoomed crop, then choose the row whose colours and underlay offset agree.
Offsets are in SDF units: a negative OffsetY drops the underlay below the glyph (a drop shadow).
"""
import glob
import os
import re
import sys

FLOAT = re.compile(r"^\s*- (_\w+): (-?[\d.e-]+)\s*$")
COLOUR = re.compile(r"^\s*- (_\w+): \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+), a: ([\d.e-]+)\}")


def hex_colour(rgba):
    rgb = "".join(f"{round(float(c) * 255):02x}" for c in rgba[:3])
    alpha = round(float(rgba[3]) * 255)
    return f"#{rgb}" + ("" if alpha == 255 else f"@{alpha}")


def describe(path):
    floats, colours, underlay = {}, {}, False
    with open(path, encoding="utf-8") as f:
        for line in f:
            underlay |= "UNDERLAY_ON" in line or "UNDERLAY_INNER" in line
            if m := FLOAT.match(line):
                floats[m[1]] = float(m[2])
            elif m := COLOUR.match(line):
                colours[m[1]] = hex_colour(m.groups()[1:])
    outline = floats.get("_OutlineWidth", 0)
    row = [os.path.splitext(os.path.basename(path))[0], colours.get("_FaceColor", "?"),
           f"{outline:g} {colours.get('_OutlineColor', '?')}" if outline > 0 else "-"]
    if underlay:
        row.append(f"{colours.get('_UnderlayColor', '?')} off {floats.get('_UnderlayOffsetX', 0):g},"
                   f"{floats.get('_UnderlayOffsetY', 0):g} dilate {floats.get('_UnderlayDilate', 0):g}"
                   f" soft {floats.get('_UnderlaySoftness', 0):g}")
    else:
        row.append("-")
    return row


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    paths = []
    for arg in sys.argv[1:]:
        paths += sorted(glob.glob(os.path.join(arg, "*.mat"))) if os.path.isdir(arg) else [arg]
    rows = [["material", "face", "outline", "underlay"]] + [describe(p) for p in paths]
    widths = [max(len(r[i]) for r in rows) for i in range(3)]
    for r in rows:
        print("  ".join(c.ljust(w) for c, w in zip(r, widths)) + "  " + r[3])


if __name__ == "__main__":
    main()
