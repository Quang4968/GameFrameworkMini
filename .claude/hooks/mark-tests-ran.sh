#!/usr/bin/env bash
# PostToolUse on Bash. When the command was an EditMode test run, record the outcome in
# Library/Toolkit/last-tests (gitignored) so the Stop hook knows whether C# edits were verified.
set -uo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
. "$here/hook-lib.sh"

HOOK_INPUT=$(cat)
cmd=$(json_get '.tool_input.command')
printf '%s' "$cmd" | grep -q 'unity command run_tests' || exit 0

root=$(repo_root)
mkdir -p "$root/Library/Toolkit"

# The pipeline answers with {"Summary":{"Total":N,"Passed":N,"Failed":N,...}}; look for it in the response.
response=$(printf '%s' "$HOOK_INPUT" | tr -d '\n')
state=ran
# Quotes may be escaped, single or double depending on how the output was echoed; only the digit matters.
if printf '%s' "$response" | grep -Eq 'Failed[^0-9]{1,6}[1-9]'; then
  state=failed
elif printf '%s' "$response" | grep -Eq 'Failed[^0-9]{1,6}0'; then
  state=passed
fi

printf '%s %s %s\n' "$state" "$(date +%s)" "$(git -C "$root" rev-parse --short HEAD 2>/dev/null || echo none)" > "$root/Library/Toolkit/last-tests"
exit 0
