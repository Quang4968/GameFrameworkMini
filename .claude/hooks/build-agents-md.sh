#!/usr/bin/env bash
# Generates AGENTS.md (read by Codex, which cannot load .claude/rules/) from CLAUDE.md plus every rules
# file with its frontmatter stripped. Runs by hand or as a PostToolUse hook after those files change.
set -uo pipefail

root=${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel 2>/dev/null || pwd)}
cd "$root" || exit 0

# As a hook: only act when the edited file is one of the sources.
if [ ! -t 0 ]; then
  input=$(cat)
  if [ -n "$input" ]; then
    file=$(printf '%s' "$input" | sed -n 's/.*"file_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)
    rel=${file#"$root"/}
    case "$rel" in
      CLAUDE.md|.claude/rules/*.md) ;;
      *) exit 0 ;;
    esac
  fi
fi

[ -f CLAUDE.md ] || exit 0

{
  echo '<!-- Generated from CLAUDE.md + .claude/rules/*.md by .claude/hooks/build-agents-md.sh; edit those, not this file. -->'
  echo
  cat CLAUDE.md
  for f in .claude/rules/*.md; do
    [ -f "$f" ] || continue
    echo
    echo "<!-- $f -->"
    echo
    # Drop a leading YAML frontmatter block (--- ... ---); Codex cannot scope rules by path anyway.
    awk 'NR==1 && $0=="---" {skip=1; next} skip && $0=="---" {skip=0; next} !skip' "$f"
  done
} > AGENTS.md
