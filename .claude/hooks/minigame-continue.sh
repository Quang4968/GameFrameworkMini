#!/usr/bin/env bash
# Stop hook. Keeps an approved /minigame run going: while Library/Toolkit/minigame/state.json says
# "running" and a stage is open, the turn is handed back with the next step instead of ending.
# Two brakes: 30 stops on one stage count as a failed attempt (state.py moves on after three), and
# 400 stops in a run pause it for the user. Without a running state it does nothing.
set -uo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
. "$here/hook-lib.sh"

HOOK_INPUT=$(cat)
root=$(repo_root)
state="$root/Library/Toolkit/minigame/state.json"
[ -f "$state" ] || exit 0

STATE_FILE="$state" python3 - <<'PY'
import json, os, sys, time

path = os.environ["STATE_FILE"]
try:
    with open(path, encoding="utf-8") as f:
        state = json.load(f)
except Exception:
    sys.exit(0)

if state.get("status") != "running":
    sys.exit(0)

stage = next((s for s in state["stages"] if s["status"] in ("pending", "in_progress")), None)
if stage is None:
    sys.exit(0)

PER_STAGE, PER_RUN = 30, 400
state["continues"] = state.get("continues", 0) + 1
if state.get("last_stage") != stage["id"]:
    state["last_stage"], state["stage_stops"] = stage["id"], 0
state["stage_stops"] = state.get("stage_stops", 0) + 1

note = ""
if state["continues"] > PER_RUN:
    state["status"] = "paused"
    state["pause_reason"] = f"{PER_RUN} stops without finishing; check the last stage and /minigame resume"
    note = "paused"
elif state["stage_stops"] > PER_STAGE:
    stage["attempts"] += 1
    stage["note"] = f"no progress after {PER_STAGE} turns"
    stage["status"] = "blocked" if stage["attempts"] >= 3 else "in_progress"
    state["stage_stops"] = 0
    note = f"{stage['id']} counted as a failed attempt ({stage['attempts']}/3)"

state["updated"] = int(time.time())
tmp = path + ".tmp"
with open(tmp, "w", encoding="utf-8") as f:
    json.dump(state, f, indent=2)
os.replace(tmp, path)

if state["status"] != "running":
    print(f"[minigame] run paused: {state['pause_reason']}", file=sys.stderr)
    sys.exit(0)

reason = (
    f"/minigame '{state['slug']}' is running; stage '{stage['id']}' is open "
    f"(attempt {stage['attempts'] + 1}/3{'; ' + note if note else ''}). Do not stop to ask: "
    "finish it per .claude/skills/minigame/reference/autonomy.md, then run "
    "`python3 .claude/skills/minigame/scripts/state.py done|fail <id> ...` and `state.py next`. "
    "Only `state.py pause <reason>` (Editor unreachable) ends the run early."
)
print(json.dumps({"decision": "block", "reason": reason}))
PY
exit 0
