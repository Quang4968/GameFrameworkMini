#!/usr/bin/env bash
# Shared helpers for the hooks. Source it; it defines json_get and repo_root.
# json_get '<jq path>' reads one string/bool field from the JSON held in $HOOK_INPUT.

json_get() {
  if command -v jq >/dev/null 2>&1; then
    printf '%s' "$HOOK_INPUT" | jq -r "$1 // empty" 2>/dev/null
  else
    printf '%s' "$HOOK_INPUT" | python3 -c '
import json, sys
try:
    node = json.load(sys.stdin)
except Exception:
    print(""); sys.exit(0)
for key in sys.argv[1].strip(".").split("."):
    node = node.get(key, "") if isinstance(node, dict) else ""
if isinstance(node, bool): print("true" if node else "false")
elif isinstance(node, str): print(node)
else: print("")' "$1" 2>/dev/null
  fi
}

repo_root() {
  printf '%s' "${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel 2>/dev/null || pwd)}"
}

# The checkout the Unity Editor is bound to: the main worktree, so a session started in a linked
# worktree shares state (the Editor lock) with the base checkout.
editor_root() {
  local root common
  root=$(repo_root)
  common=$(git -C "$root" rev-parse --path-format=absolute --git-common-dir 2>/dev/null) || { printf '%s' "$root"; return; }
  dirname "$common"
}
