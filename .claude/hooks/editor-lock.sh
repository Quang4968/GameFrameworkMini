#!/usr/bin/env bash
# One session drives the Unity Editor at a time. A `unity command` that is not a pure read takes
# Library/Toolkit/editor-lock in the checkout the Editor is bound to (linked worktrees share it).
# Subagents carry their parent's session id, so a lead and its peers hold one lock. The lock frees
# itself after STALE_AFTER seconds unused and on SessionEnd.
#
#   PreToolUse (Bash)   take or refresh the lock; exit 2 blocks with the holder as the reason
#   SessionEnd          drop the lock this session holds
#   editor-lock.sh status [session]   one line for humans and session-start.sh
#   editor-lock.sh release [--force]  print the state; the PreToolUse pass already dropped the
#                                     caller's own lock. --force (user, from a terminal) breaks any lock.
set -uo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
. "$here/hook-lib.sh"

STALE_AFTER=900
lock="$(editor_root)/Library/Toolkit/editor-lock"
now=$(date +%s)

holder="" stamp=0 where=""
[ -f "$lock" ] && read -r holder stamp where < "$lock"
age=$(( now - ${stamp:-0} ))

describe() {
  if [ -z "$holder" ]; then echo "free"; return; fi
  local who="${holder:0:8}"
  [ -n "${1:-}" ] && [ "$holder" = "$1" ] && who="this session"
  echo "held by $who, last used $(( age / 60 ))m ago${where:+ from $where}"
}

write_lock() {   # $1 = session; overwrite (refresh or takeover)
  mkdir -p "$(dirname "$lock")"
  printf '%s %s %s\n' "$1" "$now" "$(repo_root)" > "$lock.$$" && mv -f "$lock.$$" "$lock"
}

take_free() {    # $1 = session; atomic, fails if another session got there first
  mkdir -p "$(dirname "$lock")"
  ( set -o noclobber; printf '%s %s %s\n' "$1" "$now" "$(repo_root)" > "$lock" ) 2>/dev/null
}

# ---- CLI ----
case "${1:-}" in
  status)  echo "[toolkit] Editor lock: $(describe "${2:-}")"; exit 0 ;;
  release)
    if [ "${2:-}" = "--force" ]; then rm -f "$lock"; holder=""; fi
    echo "[toolkit] Editor lock: $(describe)"; exit 0 ;;
esac

# ---- hook ----
HOOK_INPUT=$(cat)
session=$(json_get '.session_id')
[ -z "$session" ] && exit 0

if [ "$(json_get '.hook_event_name')" = "SessionEnd" ]; then
  [ "$holder" = "$session" ] && rm -f "$lock"
  exit 0
fi

cmd=$(json_get '.tool_input.command')
[ -z "$cmd" ] && exit 0

# Prints the first argument after each match of $1 in command position (not inside a grep pattern
# or a quoted string), one per line; an empty line for a match without arguments.
invoked() {
  printf '%s' "$cmd" | perl -0ne 'BEGIN { $p = shift @ARGV } while (/(?:^|[;&|(\n`]|\$\()\s*(?:\w+=\S*\s+)*(?:timeout\s+\S+\s+)?$p(?:\s+([^\s;&|)]+(?:\s+--force)?))?/g) { print(($1 // "") . "\n") }' "$1"
}

release=$(invoked '(?:\S*/)?editor-lock\.sh')
if [ -n "$release" ]; then
  if printf '%s' "$release" | grep -q -- 'release[[:space:]]\+--force'; then
    printf 'Blocked: breaking the Editor lock\nOnly the user breaks another session'"'"'s lock, from a terminal. Tell them who holds it (editor-lock.sh status).\n' >&2
    exit 2
  fi
  printf '%s' "$release" | grep -q '^release' && [ "$holder" = "$session" ] && rm -f "$lock"
  exit 0
fi

subs=$(invoked 'unity\s+command')
[ -z "$subs" ] && exit 0

# Pure reads never take the lock. Anything else, including an option before the subcommand, does.
needs_lock=0
while IFS= read -r sub; do
  case "$sub" in
    ''|editor_status|console|*_status|get_*|find_*|list_*) ;;
    *) needs_lock=1 ;;
  esac
done <<< "$subs"
[ "$needs_lock" -eq 0 ] && exit 0

if [ "$holder" = "$session" ] || { [ -n "$holder" ] && [ "$age" -ge "$STALE_AFTER" ]; }; then
  write_lock "$session"
  exit 0
fi
if [ -z "$holder" ]; then
  [ -f "$lock" ] && { write_lock "$session"; exit 0; }   # empty or unreadable lock: nobody holds it
  take_free "$session" && exit 0
  read -r holder stamp where < "$lock"; age=$(( now - ${stamp:-0} ))
fi

{
  echo "Blocked: the Unity Editor is $(describe)."
  echo "One session drives the Editor at a time; a recompile, test run or Play here would interleave with theirs."
  echo "Do the parts that need no Editor, or stop and tell the user. Do not retry in a loop: the lock frees"
  echo "itself after $(( STALE_AFTER / 60 ))m unused, and only the user breaks it (.claude/hooks/editor-lock.sh release --force)."
} >&2
exit 2
