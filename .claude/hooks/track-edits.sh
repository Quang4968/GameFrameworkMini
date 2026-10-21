#!/usr/bin/env bash
# PostToolUse on Edit/Write/MultiEdit. Appends the edited path (repo-relative) to
# Library/Toolkit/sessions/<session>.edits, one line per edit. supervisor-events.sh reads it to know
# which files this session touched (not other sessions sharing the tree) and how often.
set -uo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
. "$here/hook-lib.sh"

HOOK_INPUT=$(cat)
session=$(json_get '.session_id')
file=$(json_get '.tool_input.file_path')
[ -z "$session" ] || [ -z "$file" ] && exit 0

root=$(repo_root)
mkdir -p "$root/Library/Toolkit/sessions"
printf '%s\n' "${file#"$root"/}" >> "$root/Library/Toolkit/sessions/$session.edits"
exit 0
