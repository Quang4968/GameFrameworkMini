#!/usr/bin/env python3
"""Run state of /minigame, kept in Library/Toolkit/minigame/state.json (gitignored).

The Stop hook minigame-continue.sh reads the same file: while status is "running" and a stage is
open, it hands the turn back. Commands:

  init <slug>            new run in "planning"; refuses to overwrite a running one
  start                  planning -> running (after the user approved the plan)
  next                   prints "<id> <reference file>" of the stage to work on, or "done"
  done <id> [note]       closes a stage
  fail <id> <reason>     counts a failed attempt; the third one blocks the stage and moves on
  pause <reason>         hands the turn back to the user on purpose (Editor unreachable)
  resume                 paused -> running
  status                 one line per stage
"""
import json
import os
import sys
import time

STAGES = [
    ("core", "stage-core.md"),
    ("views", "stage-views.md"),
    ("balance", "stage-balance.md"),
    ("ui", "stage-ui.md"),
    ("feel", "stage-feel.md"),
    ("tutorial", "stage-tutorial.md"),
    ("sdk", "stage-sdk.md"),
    ("monet", "stage-monet.md"),
    ("smoke", "stage-smoke.md"),
    ("build", "stage-build.md"),
    ("report", "stage-report.md"),
]
MAX_ATTEMPTS = 3


def repo_root():
    root = os.environ.get("CLAUDE_PROJECT_DIR")
    if root:
        return root
    here = os.path.dirname(os.path.abspath(__file__))
    return os.path.abspath(os.path.join(here, "..", "..", "..", ".."))


def state_path():
    return os.path.join(repo_root(), "Library", "Toolkit", "minigame", "state.json")


def load():
    path = state_path()
    if not os.path.exists(path):
        return None
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def save(state):
    path = state_path()
    os.makedirs(os.path.dirname(path), exist_ok=True)
    state["updated"] = int(time.time())
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(state, f, indent=2)
    os.replace(tmp, path)


def fail_exit(message):
    print(message, file=sys.stderr)
    sys.exit(1)


def require(state, *statuses):
    if state is None:
        fail_exit("no run: /minigame <slug> starts one")
    if statuses and state["status"] not in statuses:
        fail_exit(f"run is {state['status']}, expected {' or '.join(statuses)}")


def stage(state, stage_id):
    for s in state["stages"]:
        if s["id"] == stage_id:
            return s
    fail_exit(f"unknown stage {stage_id}; stages: {', '.join(s['id'] for s in state['stages'])}")


def open_stage(state):
    for s in state["stages"]:
        if s["status"] in ("pending", "in_progress"):
            return s
    return None


def finish_if_closed(state):
    if open_stage(state) is None:
        state["status"] = "done"


def cmd_init(args):
    if not args:
        fail_exit("usage: state.py init <slug>")
    current = load()
    if current and current["status"] in ("running", "paused"):
        fail_exit(f"run '{current['slug']}' is {current['status']}; finish or pause it first")
    save({
        "slug": args[0],
        "status": "planning",
        "started": int(time.time()),
        "continues": 0,
        "stages": [{"id": i, "reference": r, "status": "pending", "attempts": 0, "note": ""} for i, r in STAGES],
    })
    print(f"run {args[0]} created (planning)")


def cmd_start(_):
    state = load()
    require(state, "planning")
    state["status"] = "running"
    save(state)
    print("running")


def cmd_next(_):
    state = load()
    require(state, "running", "done")
    if state["status"] == "done":
        print("done")
        return
    s = open_stage(state)
    if s is None:
        finish_if_closed(state)
        save(state)
        print("done")
        return
    if s["status"] == "pending":
        s["status"] = "in_progress"
        save(state)
    print(f"{s['id']} reference/{s['reference']} attempt {s['attempts'] + 1}/{MAX_ATTEMPTS}")


def cmd_done(args):
    if not args:
        fail_exit("usage: state.py done <id> [note]")
    state = load()
    require(state, "running", "paused")
    s = stage(state, args[0])
    s["status"] = "done"
    s["note"] = " ".join(args[1:])
    finish_if_closed(state)
    save(state)
    print(f"{s['id']} done; run {state['status']}")


def cmd_fail(args):
    if len(args) < 2:
        fail_exit("usage: state.py fail <id> <reason>")
    state = load()
    require(state, "running", "paused")
    s = stage(state, args[0])
    s["attempts"] += 1
    s["note"] = " ".join(args[1:])
    if s["attempts"] >= MAX_ATTEMPTS:
        s["status"] = "blocked"
        finish_if_closed(state)
        print(f"{s['id']} blocked after {s['attempts']} attempts; moving on")
    else:
        s["status"] = "in_progress"
        print(f"{s['id']} attempt {s['attempts']}/{MAX_ATTEMPTS} failed; retry")
    save(state)


def cmd_pause(args):
    state = load()
    require(state, "running")
    state["status"] = "paused"
    state["pause_reason"] = " ".join(args) or "paused"
    save(state)
    print(f"paused: {state['pause_reason']}")


def cmd_resume(_):
    state = load()
    require(state, "paused", "running")
    state["status"] = "running"
    state.pop("pause_reason", None)
    state["continues"] = 0
    save(state)
    print("running")


def cmd_status(_):
    state = load()
    if state is None:
        print("no run")
        return
    print(f"{state['slug']}: {state['status']}" + (f" ({state['pause_reason']})" if state.get("pause_reason") else ""))
    for s in state["stages"]:
        tries = f" {s['attempts']} failed" if s["attempts"] else ""
        note = f" - {s['note']}" if s["note"] else ""
        print(f"  {s['id']:<9} {s['status']}{tries}{note}")


COMMANDS = {
    "init": cmd_init, "start": cmd_start, "next": cmd_next, "done": cmd_done, "fail": cmd_fail,
    "pause": cmd_pause, "resume": cmd_resume, "status": cmd_status,
}

if __name__ == "__main__":
    if len(sys.argv) < 2 or sys.argv[1] not in COMMANDS:
        fail_exit(__doc__)
    COMMANDS[sys.argv[1]](sys.argv[2:])
