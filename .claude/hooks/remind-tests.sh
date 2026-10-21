#!/usr/bin/env bash
# Stop hook. When C# files changed in this session after the last passed EditMode run, hand the turn
# back to the model once with a reminder. It never runs anything itself.
set -uo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
. "$here/hook-lib.sh"

HOOK_INPUT=$(cat)

# Second Stop of the same turn: the reminder was already delivered; let the turn end (loop breaker).
case "$(json_get '.stop_hook_active')" in true|True) exit 0 ;; esac

session=$(json_get '.session_id')
root=$(repo_root)
cd "$root" || exit 0

marker="Library/Toolkit/last-tests"
session_mark="Library/Toolkit/sessions/${session:-unknown}"

# C# files dirty relative to HEAD: modified, added, renamed, untracked.
changed=$(git status --porcelain --untracked-files=all -- '*.cs' 2>/dev/null | sed -E 's/^.{3}//; s/^.* -> //; s/^"//; s/"$//')
[ -z "$changed" ] && exit 0

stale=""
while IFS= read -r f; do
  [ -n "$f" ] && [ -f "$f" ] || continue
  # Only files touched during this session count; a dirty tree inherited from elsewhere is not our nag.
  if [ -f "$session_mark" ] && [ ! "$f" -nt "$session_mark" ]; then continue; fi
  if [ -f "$marker" ]; then
    read -r state _ < "$marker"
    if [ "$state" = "passed" ] && [ ! "$f" -nt "$marker" ]; then continue; fi
  fi
  stale="$stale$f"$'\n'
done <<< "$changed"

stale=$(printf '%s' "$stale" | sed '/^$/d')
[ -z "$stale" ] && exit 0

count=$(printf '%s\n' "$stale" | grep -c .)
{
  echo "EditMode tests have not passed since $count C# file(s) changed in this session:"
  printf '%s\n' "$stale" | head -12 | sed 's/^/  /'
  [ "$count" -gt 12 ] && echo "  ..."
  echo "Run: unity command recompile; unity command recompile_status; unity command console --level error; unity command run_tests --mode EditMode"
  echo "If the Editor is not running, or you are stopping mid-task on purpose, say so in one line and stop."
} >&2
exit 2
