#!/usr/bin/env bash
# PreToolUse guard. Refuses edits under the paths listed in protected-paths.txt (minus the folders its
# '!' lines carve out), edits to .meta files, and blanket staging. Exit 2 blocks the tool call; stderr
# goes back to the model as the reason.
set -uo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
# shellcheck source=hook-lib.sh
. "$here/hook-lib.sh"

HOOK_INPUT=$(cat)
tool=$(json_get '.tool_name')
file=$(json_get '.tool_input.file_path')
[ -z "$file" ] && file=$(json_get '.tool_input.notebook_path')
cmd=$(json_get '.tool_input.command')

root=$(repo_root)
table="$here/protected-paths.txt"
unlock="$root/Library/Toolkit/unlock"

block() {
  printf 'Blocked: %s\n%s\n' "$1" "$2" >&2
  exit 2
}

unlocked() {   # $1 = repo-relative path
  [ -f "$unlock" ] || return 1
  while IFS= read -r allow; do
    [ -n "$allow" ] && [[ "$1" == "$allow"* ]] && return 0
  done < "$unlock"
  return 1
}

carved() {   # $1 = repo-relative path; true when a '!' line of the table carves it out
  local prefix reason
  [ -f "$table" ] || return 1
  [[ "$1" == *..* ]] && return 1
  while IFS='|' read -r prefix reason; do
    [[ "$prefix" == '!'* ]] && [[ "$1" == "${prefix#!}"* ]] && return 0
  done < "$table"
  return 1
}

strip_carved() {   # $1 = command text; prints it without the folders '!' lines carve out
  local text=$1 rest out prefix reason base
  [ -f "$table" ] || { printf '%s' "$text"; return; }
  while IFS='|' read -r prefix reason; do
    [[ "$prefix" == '!'* ]] || continue
    base=${prefix#!}
    base=${base%/}
    [ -n "$base" ] || continue          # a bare '!' line carves nothing
    [[ "$text" == *..* ]] && continue   # a carved folder is never a way out of its parent
    out=
    rest=$text
    while [[ "$rest" == *"$base"* ]]; do
      out+=${rest%%"$base"*}
      rest=${rest#*"$base"}
      case "${rest:0:1}" in
        ''|/|' '|'"'|"'"|';'|'&'|'|'|')') ;;
        *) out+=$base ;;   # a longer name that only starts like the carved folder
      esac
    done
    text=$out$rest
  done < "$table"
  printf '%s' "$text"
}

check_path() {   # $1 = repo-relative path
  local rel=$1 prefix reason
  unlocked "$rel" && return 0
  [[ "$rel" == *.meta ]] && block "$rel" "Unity owns .meta files. Create, move or rename assets with AssetDatabase in 'unity command eval' so the Editor writes the meta."
  carved "$rel" && return 0
  [ -f "$table" ] || return 0
  while IFS='|' read -r prefix reason; do
    [ -z "$prefix" ] && continue
    [[ "$prefix" == \#* || "$prefix" == '!'* ]] && continue
    if [[ "$rel" == "$prefix"* ]]; then
      block "$rel" "$reason To override once, the user adds the prefix to Library/Toolkit/unlock."
    fi
  done < "$table"
}

case "$tool" in
  Edit|Write|MultiEdit|NotebookEdit)
    [ -z "$file" ] && exit 0
    rel=${file#"$root"/}
    check_path "$rel"
    ;;
  Bash)
    [ -z "$cmd" ] && exit 0
    # Blanket staging hides other sessions' work in a shared tree.
    if printf '%s' "$cmd" | grep -Eq 'git[[:space:]]+add[[:space:]]+(-A|--all|\.)([[:space:]]|$)|git[[:space:]]+commit[[:space:]]+[^|;&]*(-a|--all)([[:space:]]|$)|git[[:space:]]+commit[[:space:]]+-am[[:space:]]'; then
      block "git add -A / git commit -a" "Stage only the paths this task changed: git add <path> ..."
    fi
    # Only mutating shell commands are inspected; reads (grep, cat, ls) stay free.
    # A redirect is '>' after whitespace, a descriptor digit or '&'; '<slug>' in prose is not one.
    if printf '%s' "$cmd" | grep -Eq '(^|[;&|][[:space:]]*|[[:space:]])(rm|mv|cp|touch|tee|truncate|ln|sed[[:space:]]+-i|perl[[:space:]]+-p?i)([[:space:]]|$)|(^|[[:space:]0-9&])>>?[[:space:]]*"?[^&[:space:]]|git[[:space:]]+(checkout|restore|clean|rm|mv)[[:space:]]'; then
      scan=$(strip_carved "$cmd")
      while IFS='|' read -r prefix reason; do
        [ -z "$prefix" ] && continue
        [[ "$prefix" == \#* || "$prefix" == '!'* ]] && continue
        if printf '%s' "$scan" | grep -Fq -- "$prefix"; then
          unlocked "$prefix" && continue
          block "$prefix (named in a mutating shell command)" "$reason"
        fi
      done < "$table"
      if printf '%s' "$cmd" | grep -Eq '\.meta(["'"'"'[:space:]]|$)'; then
        block ".meta (named in a mutating shell command)" "Unity owns .meta files; use AssetDatabase.MoveAsset / DeleteAsset in 'unity command eval'."
      fi
    fi
    ;;
esac
exit 0
