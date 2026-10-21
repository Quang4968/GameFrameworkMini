#!/usr/bin/env python3
"""Measure a UI demo image in canvas units and set it beside an in-game capture.

Every coordinate this tool reads or prints is a canvas unit: an image is resampled to --ref px wide
(the canvas reference width from the project card) before anything else. Needs python3, numpy and
macOS sips; any format sips opens (png, jpg) goes through a BMP round-trip. Images it writes go to
Temp/uiref/ in the project (gitignored, cleared when the Editor quits) unless --out names a file.

  info IMG                                   size and scale to canvas units
  grid IMG [--crop X,Y,W,H] [--zoom N]       labelled coordinate grid, to read positions by eye
  scan IMG --row Y | --col X                 colour runs along one line: edges, gaps, widths
  bbox IMG --crop X,Y,W,H                    box of what differs from the background + spans
  pick IMG X,Y [X,Y ...]                     colour at points: text face, outline, underlay, fill
  side DEMO CAPTURE [--crop] [--shift] [--diff]   demo | capture on one grid (| difference)
"""
import argparse
import os
import struct
import subprocess
import sys
import tempfile

try:
    import numpy as np
except ImportError:
    sys.exit("uiref: numpy is missing; install it with `python3 -m pip install numpy`")

OUT_DIR = os.path.join(os.path.abspath(__file__).split(os.sep + ".claude" + os.sep)[0], "Temp", "uiref")
GRID_COLOUR = np.array([255, 0, 255], np.int16)
DIGITS = dict(zip("0123456789-", (  # 3x5 bitmap digits, row by row
    "111101101101111 010110010010111 111001111100111 111001111001111 101101111001001 111100111001111 "
    "111100111101111 111001001001001 111101111101111 111101111001111 000000111000000").split()))
LABEL = 3  # label pixel size: a digit is 3x5 cells of LABEL px
MARGIN_LEFT, MARGIN_TOP = 4 * 4 * LABEL + 8, 5 * LABEL + 8  # room for four digits / one row
MAX_RUNS = 80


def sips(src, fmt, dst):
    result = subprocess.run(["sips", "-s", "format", fmt, src, "--out", dst], capture_output=True, text=True)
    if result.returncode != 0 or not os.path.exists(dst):
        sys.exit(f"uiref: sips could not convert {src}: {result.stderr.strip()}")


def read_image(path, ref):
    """RGB uint8 array resampled to `ref` px wide, so one pixel is one canvas unit."""
    with tempfile.TemporaryDirectory() as tmp:
        bmp = os.path.join(tmp, "in.bmp")
        sips(path, "bmp", bmp)
        with open(bmp, "rb") as f:
            data = f.read()
    offset = struct.unpack_from("<I", data, 10)[0]
    width, height = struct.unpack_from("<ii", data, 18)
    depth = struct.unpack_from("<H", data, 28)[0] // 8
    if depth not in (3, 4):
        sys.exit(f"uiref: unexpected {depth * 8}-bit BMP from sips")
    stride = (width * depth + 3) & ~3
    rows = np.frombuffer(data, np.uint8, stride * abs(height), offset).reshape(abs(height), stride)
    pixels = rows[:, : width * depth].reshape(abs(height), width, depth)[:, :, 2::-1]
    pixels = pixels[::-1] if height > 0 else pixels  # a positive height stores rows bottom-up
    return resample(np.ascontiguousarray(pixels), ref / width), width, abs(height)


def resample(img, scale):
    if abs(scale - 1) < 1e-6:
        return img
    h, w = img.shape[:2]
    ys = np.minimum(((np.arange(round(h * scale)) + 0.5) / scale).astype(int), h - 1)
    xs = np.minimum(((np.arange(round(w * scale)) + 0.5) / scale).astype(int), w - 1)
    return img[ys][:, xs]


def write_image(img, path):
    img = np.clip(img, 0, 255).astype(np.uint8)
    h, w = img.shape[:2]
    stride = (w * 3 + 3) & ~3
    rows = np.zeros((h, stride), np.uint8)
    rows[:, : w * 3] = img[::-1, :, ::-1].reshape(h, w * 3)
    header = struct.pack("<2sIHHI", b"BM", 54 + rows.size, 0, 0, 54)
    header += struct.pack("<IiiHHIIiiII", 40, w, h, 1, 24, 0, rows.size, 2835, 2835, 0, 0)
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        bmp = os.path.join(tmp, "out.bmp")
        with open(bmp, "wb") as f:
            f.write(header + rows.tobytes())
        sips(bmp, "png", path)
    print(path)


def out_path(args, command, image):
    if args.out:
        return args.out
    stem = os.path.splitext(os.path.basename(image))[0].replace(" ", "_")
    suffix = "-" + args.crop.replace(",", "_") if getattr(args, "crop", None) else ""
    return os.path.join(OUT_DIR, f"{command}-{stem}{suffix}.png")


def parse_ints(text, count, what):
    try:
        values = [int(round(float(v))) for v in text.split(",")]
    except ValueError:
        values = []
    if len(values) != count:
        sys.exit(f"uiref: {what} must be {count} comma-separated numbers, got '{text}'")
    return values


def parse_colour(text):
    return np.array([int(text.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4)], np.int16)


def hex_colour(colour):
    return "#" + "".join(f"{int(c):02x}" for c in colour)


def crop(img, box):
    x, y, w, h = box
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(img.shape[1], x + w), min(img.shape[0], y + h)
    if x1 <= x0 or y1 <= y0:
        sys.exit(f"uiref: crop {box} is outside the {img.shape[1]}x{img.shape[0]} image")
    return img[y0:y1, x0:x1], (x0, y0)


def draw_label(img, corner, text):
    x, y = corner
    for i, ch in enumerate(text):
        for k, bit in enumerate(DIGITS[ch]):
            if bit == "1":
                r, c = divmod(k, 3)
                top, left = y + r * LABEL, x + (i * 4 + c) * LABEL
                img[top:top + LABEL, left:left + LABEL] = 0


def label_width(text):
    return (len(text) * 4 - 1) * LABEL


def with_grid(img, origin, zoom, step):
    """Zoom `img`, whose top-left sits at canvas `origin`, and frame it with a labelled grid."""
    big = np.repeat(np.repeat(img, zoom, 0), zoom, 1).astype(np.int16)
    out = np.full((big.shape[0] + MARGIN_TOP, big.shape[1] + MARGIN_LEFT, 3), 255, np.int16)
    out[MARGIN_TOP:, MARGIN_LEFT:] = big
    for vertical in (True, False):  # lines at x values labelled on top, then at y values on the left
        first, length = (origin[0], img.shape[1]) if vertical else (origin[1], img.shape[0])
        margin, last = (MARGIN_LEFT if vertical else MARGIN_TOP), -1
        for v in range(-(-first // step) * step, first + length, step):
            p, text = margin + (v - first) * zoom, str(v)
            if vertical:
                out[MARGIN_TOP:, p] = (out[MARGIN_TOP:, p] + GRID_COLOUR) // 2
                at, extent = max(MARGIN_LEFT, p - label_width(text) // 2), label_width(text)
            else:
                out[p, MARGIN_LEFT:] = (out[p, MARGIN_LEFT:] + GRID_COLOUR) // 2
                at, extent = max(MARGIN_TOP, p - 5 * LABEL // 2), 5 * LABEL
            if at > last and at + extent <= out.shape[1 if vertical else 0]:
                draw_label(out, (at, 4) if vertical else (MARGIN_LEFT - 4 - label_width(text), at), text)
                last = at + extent + 2 * LABEL
    return out


def spans(flags, offset, gap):
    """Runs of True in `flags` as 'a-b' canvas ranges, bridging holes shorter than `gap`."""
    result = []
    for i in np.nonzero(flags)[0]:
        if result and i - result[-1][1] <= gap:
            result[-1][1] = i
        else:
            result.append([i, i])
    return ", ".join(f"{offset + a}-{offset + b} ({b - a + 1})" for a, b in result)


def cmd_info(args):
    img, width, height = read_image(args.image, args.ref)
    print(f"{args.image}: {width}x{height} px, x{args.ref / width:.4f} -> canvas {img.shape[1]}x{img.shape[0]}")


def cmd_grid(args):
    img, _, _ = read_image(args.image, args.ref)
    part, origin = crop(img, parse_ints(args.crop, 4, "--crop")) if args.crop else (img, (0, 0))
    write_image(with_grid(part, origin, args.zoom, args.step), out_path(args, "grid", args.image))


def cmd_scan(args):
    img, _, _ = read_image(args.image, args.ref)
    axis, name = ("x", "row y") if args.row is not None else ("y", "column x")
    index, size = (args.row, img.shape[0]) if axis == "x" else (args.col, img.shape[1])
    if (args.row is None) == (args.col is None) or not 0 <= index < size:
        sys.exit(f"uiref scan: give one of --row Y (0-{img.shape[0] - 1}) or --col X (0-{img.shape[1] - 1})")
    line = img[index] if axis == "x" else img[:, index]
    lo, hi = max(0, args.start), min(len(line), args.end or len(line))
    line = line[lo:hi].astype(np.int16)
    runs, start = [], 0
    for i in range(1, len(line) + 1):
        if i < len(line) and np.abs(line[i] - line[start]).max() <= args.tol:
            continue
        colour = np.median(line[start:i], axis=0)
        if runs and (i - start < args.min or np.abs(colour - runs[-1][2]).max() <= args.tol):
            runs[-1][1] = i - 1  # an anti-aliased edge or a near-identical run joins the previous one
        else:
            runs.append([start, i - 1, colour])
        start = i
    print(f"scan {name}={index}, {axis} {lo}-{hi - 1}, tol {args.tol}, min run {args.min}")
    for a, b, _ in runs[:MAX_RUNS]:
        colour = np.median(line[a:b + 1], axis=0)
        print(f"  {axis} {lo + a:>4}-{lo + b:<4} ({b - a + 1:>4})  {hex_colour(colour)}")
    if len(runs) > MAX_RUNS:
        print(f"  ... {len(runs) - MAX_RUNS} more runs; raise --tol or --min, or narrow --start/--end")


def cmd_bbox(args):
    img, _, _ = read_image(args.image, args.ref)
    part, (x0, y0) = crop(img, parse_ints(args.crop, 4, "--crop"))
    background = parse_colour(args.bg) if args.bg else part[0, 0].astype(np.int16)
    mask = np.abs(part.astype(np.int16) - background).max(axis=2) > args.tol
    if not mask.any():
        print(f"nothing in the crop differs from {hex_colour(background)} by more than {args.tol}")
        return
    ys, xs = np.nonzero(mask.any(1))[0], np.nonzero(mask.any(0))[0]
    print(f"background {hex_colour(background)}, tol {args.tol}")
    print(f"  box   x={x0 + xs[0]} y={y0 + ys[0]} w={xs[-1] - xs[0] + 1} h={ys[-1] - ys[0] + 1}")
    print(f"  cols  {spans(mask.any(0), x0, args.gap)}")
    print(f"  rows  {spans(mask.any(1), y0, args.gap)}")


def cmd_pick(args):
    img, _, _ = read_image(args.image, args.ref)
    for point in args.points:
        x, y = parse_ints(point, 2, "a point")
        inside = 0 <= x < img.shape[1] and 0 <= y < img.shape[0]
        print(f"  {x},{y}  {hex_colour(img[y, x]) if inside else 'outside the image'}")


def cmd_side(args):
    demo, _, _ = read_image(args.demo, args.ref)
    capture, _, _ = read_image(args.capture, args.ref)
    whole = (0, 0, args.ref, max(demo.shape[0], capture.shape[0]))
    box = parse_ints(args.crop, 4, "--crop") if args.crop else whole
    dx, dy = parse_ints(args.shift, 2, "--shift") if args.shift else (0, 0)
    parts = [crop(demo, box), crop(capture, (box[0] + dx, box[1] + dy, box[2], box[3]))]
    panels = [with_grid(part, origin, args.zoom, args.step) for part, origin in parts]
    if args.diff:
        h, w = np.min([p.shape[:2] for p, _ in parts], axis=0)
        a, b = (p[:h, :w].astype(np.int16) for p, _ in parts)
        delta = np.minimum(np.abs(a - b).max(axis=2) * 3, 255)
        panels.append(with_grid(np.repeat(delta[:, :, None], 3, 2), parts[0][1], args.zoom, args.step))
    height = max(p.shape[0] for p in panels)
    gap = np.full((height, 16, 3), 128, np.int16)
    padded = [np.pad(p, ((0, height - p.shape[0]), (0, 0), (0, 0)), constant_values=255) for p in panels]
    row = [piece for panel in padded for piece in (panel, gap)][:-1]
    write_image(np.concatenate(row, axis=1), out_path(args, "side", args.capture))


def main():
    common = argparse.ArgumentParser(add_help=False)
    common.add_argument("--ref", type=int, default=1080, help="canvas reference width (project card)")
    common.add_argument("--out", help="output png (default: Temp/uiref/ in the project)")
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)

    def command(name, run, *positional):
        p = sub.add_parser(name, parents=[common])
        for arg in positional:
            p.add_argument(arg)
        p.set_defaults(run=run)
        return p

    def framing(p):
        p.add_argument("--zoom", type=int, default=1)
        p.add_argument("--step", type=int, default=50, help="grid spacing in canvas units")

    command("info", cmd_info, "image")
    p = command("grid", cmd_grid, "image")
    p.add_argument("--crop", help="X,Y,W,H in canvas units")
    framing(p)
    p = command("scan", cmd_scan, "image")
    p.add_argument("--row", type=int)
    p.add_argument("--col", type=int)
    p.add_argument("--start", type=int, default=0, help="first canvas unit along the line")
    p.add_argument("--end", type=int, help="last canvas unit along the line (exclusive)")
    p.add_argument("--tol", type=int, default=24, help="max channel difference inside one run")
    p.add_argument("--min", type=int, default=3, help="runs shorter than this join the previous one")
    p = command("bbox", cmd_bbox, "image")
    p.add_argument("--crop", required=True, help="X,Y,W,H in canvas units")
    p.add_argument("--bg", help="background #rrggbb (default: the crop's top-left pixel)")
    p.add_argument("--tol", type=int, default=24)
    p.add_argument("--gap", type=int, default=4, help="holes up to this wide stay inside one span")
    p = command("pick", cmd_pick, "image")
    p.add_argument("points", nargs="+", help="X,Y in canvas units")
    p = command("side", cmd_side, "demo", "capture")
    p.add_argument("--crop", help="X,Y,W,H in canvas units, applied to both")
    p.add_argument("--shift", help="DX,DY added to the capture's crop (write --shift=-16,76 when negative)")
    p.add_argument("--diff", action="store_true", help="add a third panel: per-pixel difference x3")
    framing(p)

    args = parser.parse_args()
    args.run(args)


if __name__ == "__main__":
    main()
