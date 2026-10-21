#!/usr/bin/env bash
# SessionStart. Marks when this session began (the Stop hook only nags about files edited after
# this) and prints three lines of orientation into the context.
set -uo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
. "$here/hook-lib.sh"

HOOK_INPUT=$(cat)
session=$(json_get '.session_id')
root=$(repo_root)
cd "$root" || exit 0

mkdir -p Library/Toolkit/sessions
mark="Library/Toolkit/sessions/${session:-unknown}"
[ -f "$mark" ] || touch "$mark"   # resume keeps the original start time

branch=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "?")
modified=$(git status --porcelain 2>/dev/null | grep -c '^ *[MADRC]' || true)
untracked=$(git status --porcelain 2>/dev/null | grep -c '^??' || true)
last="none"
if [ -f Library/Toolkit/last-tests ]; then
  read -r state stamp _ < Library/Toolkit/last-tests
  age=$(( $(date +%s) - ${stamp:-0} ))
  if [ "$age" -lt 3600 ]; then last="$state $((age / 60))m ago"; else last="$state $((age / 3600))h ago"; fi
fi
echo "[toolkit] branch $branch · $modified modified · $untracked untracked · last EditMode run: $last"

if [ -f Library/Pipeline/.unity-pipeline-port ]; then
  status=$(perl -e 'alarm 5; exec @ARGV' unity command editor_status 2>/dev/null | tail -1 | sed -n 's/.*"status":"\([^"]*\)".*"playMode":"\([^"]*\)".*/\1, play mode \2/p')
  echo "[toolkit] Editor: ${status:-reachable but status unknown} (port $(sed -n 's/.*"port"[[:space:]]*:[[:space:]]*\([0-9]*\).*/\1/p' Library/Pipeline/.unity-pipeline-port | head -1))"
else
  echo "[toolkit] Editor: not running — start it before /verify or any unity command."
fi
"$here/editor-lock.sh" status "$session"
echo "[toolkit] Protected paths are hook-enforced (.claude/hooks/protected-paths.txt). Start work with /task; it plans first and waits for your go-ahead."
exit 0
