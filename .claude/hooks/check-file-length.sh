#!/usr/bin/env bash
# PostToolUse feedback. A .cs file that just grew past 300 lines (or past its legacy ceiling in
# ArchitectureBaseline.FileLines) gets flagged back to the model. Exit 2 = feedback, the edit stays:
# a split legitimately passes through an over-limit state, and FilesStayUnderLineLimit is the real gate.
set -uo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
. "$here/hook-lib.sh"

HOOK_INPUT=$(cat)
file=$(json_get '.tool_input.file_path')
[ -z "$file" ] && exit 0
[[ "$file" == *.cs ]] || exit 0
[ -f "$file" ] || exit 0

limit=300
lines=$(awk 'END { print NR }' "$file")
[ "$lines" -le "$limit" ] && exit 0

root=$(repo_root)
rel=${file#"$root"/}
# Path as the baseline spells it: relative to the Scripts root, forward slashes.
key=${rel#*/Scripts/}
baseline=$(find "$root/Assets" -path '*/Tests/EditMode/Architecture/ArchitectureBaseline.cs' -print -quit 2>/dev/null)
ceiling=$limit
if [ -n "$baseline" ]; then
  found=$(awk -v p="$key" '/FileLines[[:space:]]*=/ { f = 1 } f && index($0, "[\"" p "\"]") { gsub(/[^0-9]/, "", $NF); print $NF; exit }' "$baseline")
  [ -n "$found" ] && ceiling=$found
fi

[ "$lines" -le "$ceiling" ] && exit 0

{
  echo "$rel is $lines lines; the ceiling is $ceiling (FilesStayUnderLineLimit will fail)."
  echo "Split it by responsibility now (see .claude/rules/file-layout.md), not by line count."
  [ "$ceiling" != "$limit" ] && echo "This file is legacy debt listed in ArchitectureBaseline; it may shrink, never grow."
} >&2
exit 2
