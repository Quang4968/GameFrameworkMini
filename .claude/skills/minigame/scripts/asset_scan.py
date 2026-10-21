#!/usr/bin/env python3
"""Phase 0 of /minigame: what the project holds and what the game still needs.

Usage: asset_scan.py [--root <Root>] [--json] [--with Pause,BuyBooster,...]

<Root> defaults to the Root row of the project card in CLAUDE.md.

Exit 1 when gameplay art or a demo of a required screen is missing; the printed table says which
file goes where. --with marks plan screens as required (Phase 1 reruns it with the screens in scope).
The screen list mirrors reference/screens.md; change both together.
"""
import argparse
import json
import os
import re
import sys

REQUIRED = ["Home", "Hud", "Win", "Lose", "OutOfLives", "Settings", "SettingsIngame", "Out"]
PLAN = ["Pause", "BuyBooster", "BoosterUnlock", "NewFeature", "DailyReward", "Profile", "Loading", "Tutorial"]

IMAGE = (".png", ".jpg", ".jpeg", ".psd", ".tga")
MODEL = (".fbx", ".obj", ".blend")
AUDIO = (".wav", ".mp3", ".ogg", ".aif", ".aiff")
DOC = (".md", ".pdf", ".docx", ".txt")


def repo_root():
    root = os.environ.get("CLAUDE_PROJECT_DIR")
    if root:
        return root
    here = os.path.dirname(os.path.abspath(__file__))
    return os.path.abspath(os.path.join(here, "..", "..", "..", ".."))


def card_root(root):
    """The Root row of the project card in CLAUDE.md, e.g. | Root | `<path>/` |."""
    try:
        with open(os.path.join(root, "CLAUDE.md"), encoding="utf-8") as f:
            match = re.search(r"^\|\s*Root\s*\|\s*`([^`]+)`", f.read(), re.M)
    except OSError:
        match = None
    if not match:
        sys.exit("no Root row in the CLAUDE.md project card; pass --root")
    return match.group(1).rstrip("/")


def walk(top, exts):
    found = []
    if not os.path.isdir(top):
        return found
    for base, dirs, files in os.walk(top):
        dirs[:] = [d for d in dirs if not d.startswith(".")]
        for name in files:
            if name.lower().endswith(exts):
                found.append(os.path.join(base, name))
    return found


def rel(path, root):
    return os.path.relpath(path, root)


def find_dir(parent, name):
    """Case-insensitive child folder lookup: demos are often dropped as 'win' or 'WIN'."""
    if not os.path.isdir(parent):
        return None
    for entry in os.listdir(parent):
        if entry.lower() == name.lower() and os.path.isdir(os.path.join(parent, entry)):
            return os.path.join(parent, entry)
    return None


def scan(root, game_root, extra_required):
    textures = os.path.join(game_root, "Textures")
    ui = os.path.join(textures, "UI")
    report = {"root": rel(game_root, root), "missing": [], "screens": {}, "notes": []}

    gameplay_images = [p for p in walk(textures, IMAGE)
                       if not p.startswith(ui + os.sep) and os.sep + "Placeholder" + os.sep not in p]
    models = walk(game_root, MODEL)
    report["gameplay_art"] = {"images": len(gameplay_images), "models": len(models),
                              "sample": [rel(p, root) for p in (gameplay_images + models)[:8]]}
    if not gameplay_images and not models:
        report["missing"].append({"kind": "gameplay art", "where": rel(textures, root) + "/<Game>/",
                                  "example": "pieces, board, background sprites"})

    required = REQUIRED + [s for s in extra_required if s in PLAN]
    for screen in REQUIRED + PLAN:
        folder = find_dir(ui, screen)
        demos = []
        sprites = 0
        if folder:
            for p in walk(folder, IMAGE):
                if "demo" in os.path.basename(p).lower():
                    demos.append(rel(p, root))
                else:
                    sprites += 1
        need = "required" if screen in required else "plan"
        report["screens"][screen] = {"need": need, "demos": demos, "sprites": sprites}
        if need == "required" and not demos:
            report["missing"].append({"kind": f"{screen} demo",
                                      "where": rel(os.path.join(ui, screen), root) + "/",
                                      "example": f"{screen.lower()}_demo.png, the screen as it should look, 1080 wide"})
        if len(demos) > 1:
            report["notes"].append(f"{screen}: {len(demos)} demos, Phase 1 asks which is current")

    audio = walk(game_root, AUDIO)
    music = [p for p in audio if re.search(r"music|bgm", os.path.basename(p), re.I)]
    report["sfx"] = {"clips": len(audio) - len(music), "music": [rel(p, root) for p in music]}
    if not audio:
        report["notes"].append("no audio: cues stay silent and are listed in the report")

    vfx = []
    for p in walk(os.path.join(game_root, "Prefabs"), (".prefab",)):
        try:
            with open(p, encoding="utf-8", errors="ignore") as f:
                if "ParticleSystem:" in f.read():
                    vfx.append(rel(p, root))
        except OSError:
            pass
    report["vfx"] = vfx
    if not vfx:
        report["notes"].append("no particle prefab: the feel stage builds effects on the game's sprites")

    gdd = [rel(p, root) for p in walk(os.path.join(root, "docs"), DOC)
           if re.search(r"gdd|design", os.path.basename(p), re.I)]
    report["gdd"] = gdd
    if not gdd:
        report["notes"].append("no GDD: Phase 1 asks for core loop, win/lose, levels, session length, curve")

    report["fonts"] = len(walk(os.path.join(game_root, "Fonts"), (".mat", ".asset", ".ttf", ".otf")))
    return report


def print_human(report):
    art = report["gameplay_art"]
    print(f"Root {report['root']}")
    print(f"Gameplay art: {art['images']} images, {art['models']} models")
    print("Screens:")
    for name, s in report["screens"].items():
        mark = "ok" if s["demos"] else ("MISSING" if s["need"] == "required" else "-")
        print(f"  {name:<15} {s['need']:<8} {mark:<8} demos {len(s['demos'])}, sprites {s['sprites']}")
    print(f"SFX clips {report['sfx']['clips']}, music {len(report['sfx']['music'])}; "
          f"VFX prefabs {len(report['vfx'])}; GDD {len(report['gdd'])}; font assets {report['fonts']}")
    for note in report["notes"]:
        print(f"note: {note}")
    if report["missing"]:
        print("\nMissing - add these, then run /minigame again:")
        print(f"  {'what':<22} {'folder':<44} example")
        for m in report["missing"]:
            print(f"  {m['kind']:<22} {m['where']:<44} {m['example']}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root")
    parser.add_argument("--json", action="store_true")
    parser.add_argument("--with", dest="extra", default="")
    args = parser.parse_args()

    root = repo_root()
    game_root = os.path.join(root, args.root or card_root(root))
    extra = [s.strip() for s in args.extra.split(",") if s.strip()]
    report = scan(root, game_root, extra)

    if args.json:
        print(json.dumps(report, indent=2))
    else:
        print_human(report)
    sys.exit(1 if report["missing"] else 0)


if __name__ == "__main__":
    main()
