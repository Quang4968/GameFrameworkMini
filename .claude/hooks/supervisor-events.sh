#!/usr/bin/env bash
# Stop / SubagentStop. The cheap tier of the supervisor: it spots the moments where an agent tends to
# drift and hands back an open question, never a verdict. Each question is asked once per session.
#   contract   an existing Core contract grew in the same session as a test that uses it
#   assert     assertions that were already in a test got rewritten or removed
#   churn      one file edited CHURN_AT times or more (the direction may have changed unseen)
#   invariant  an edit under a prefix listed in invariant-paths.txt
# Scope is the files this session edited (track-edits.sh), not the whole dirty tree. Exit 2 = feedback.
set -uo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
. "$here/hook-lib.sh"

CHURN_AT=5
MAX_QUESTIONS=3

HOOK_INPUT=$(cat)
case "$(json_get '.stop_hook_active')" in true|True) exit 0 ;; esac
session=$(json_get '.session_id')
root=$(repo_root)
cd "$root" || exit 0

edits="Library/Toolkit/sessions/${session:-unknown}.edits"
asked="Library/Toolkit/sessions/${session:-unknown}.asked"
[ -s "$edits" ] || exit 0
touch "$asked"

questions=()
ask() {   # $1 = key, $2 = question
  grep -qxF -- "$1" "$asked" && return
  [ "${#questions[@]}" -ge "$MAX_QUESTIONS" ] && return
  printf '%s\n' "$1" >> "$asked"
  questions+=("$2")
}
at_head() { git cat-file -e "HEAD:$1" 2>/dev/null; }

files=$(sort -u "$edits")
tests=$(printf '%s\n' "$files" | grep '/Scripts/Tests/.*\.cs$' || true)

while IFS= read -r f; do
  [ -f "$f" ] || continue

  case "$f" in
    */Scripts/Core/Services/*.cs|*/Scripts/Core/Domain/*.cs)
      if [ -n "$tests" ] && at_head "$f" && git diff HEAD -U0 -- "$f" | grep -q '^+[^+]'; then
        name=$(basename "$f" .cs)
        user=$(printf '%s\n' "$tests" | while IFS= read -r t; do grep -lw -- "$name" "$t" 2>/dev/null; done | head -1)
        [ -n "$user" ] && ask "contract:$f" "\`$f\` is an existing Core contract and it changed in the same session as \`$user\`, which uses it. Was each new or changed member named in the approved plan, or was it added so the test or its fake compiles? (.claude/rules/tests.md)"
      fi ;;
    */Scripts/Tests/*.cs)
      if at_head "$f" && git diff HEAD -U0 -- "$f" | grep -q '^-[^-].*Assert\.'; then
        ask "assert:$f" "Assertions that were already in \`$f\` were rewritten or removed. Did the behaviour change on purpose, as the plan says, or was the test adjusted until it passed? If the old assertion pinned something the docs never agreed on, say so rather than choosing silently."
      fi ;;
  esac

  count=$(grep -cxF -- "$f" "$edits")
  if [ "$count" -ge "$CHURN_AT" ]; then
    ask "churn:$f" "\`$f\` has been edited $count times in this session. Does the current approach still match the plan the user approved, or has it become a different one they have not seen?"
  fi

  if [ -f "$here/invariant-paths.txt" ]; then
    while IFS='|' read -r prefix doc question; do
      [ -z "$prefix" ] || [[ "$prefix" == \#* ]] && continue
      [[ "$f" == "$prefix"* ]] && ask "invariant:$prefix" "\`$f\` sits on an invariant described in $doc. $question"
    done < "$here/invariant-paths.txt"
  fi
done <<< "$files"

[ "${#questions[@]}" -eq 0 ] && exit 0
{
  echo "Supervisor, open questions (not a verdict; each is asked once per session):"
  printf -- '- %s\n' "${questions[@]}"
  echo "Answer each in a line or two. If the work is right as it is, say why and finish; if not, fix it or ask the user."
} >&2
exit 2
