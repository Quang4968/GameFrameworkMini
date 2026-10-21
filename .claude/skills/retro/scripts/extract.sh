#!/usr/bin/env bash
# Condenses this project's Claude Code transcripts (and its worktrees') from the last N days into
# the signals a retro needs: user prompts, interrupts, AskUserQuestion answers, denied tool calls,
# hook blocks and hook feedback (tests reminder, supervisor questions). One block per session.
#   extract.sh [days=7] [project-dir=$CLAUDE_PROJECT_DIR or cwd]
set -uo pipefail

days=${1:-7}
project=${2:-${CLAUDE_PROJECT_DIR:-$(pwd)}}
slug=$(printf '%s' "$project" | sed 's/[^A-Za-z0-9]/-/g')
base="$HOME/.claude/projects"

dirs=()
for d in "$base/$slug" "$base/$slug"--claude-worktrees-*; do [ -d "$d" ] && dirs+=("$d"); done
[ "${#dirs[@]}" -eq 0 ] && { echo "No transcripts for $project (looked for $base/$slug)." >&2; exit 1; }

find "${dirs[@]}" -maxdepth 1 -name '*.jsonl' -mtime "-$days" -print0 | xargs -0 ls -tr 2>/dev/null |
while IFS= read -r f; do
  jq -r --arg file "$(basename "$f" .jsonl)" '
    def clip($n): (if type == "array" then map(tostring) | join(", ") else tostring end) | gsub("\\s+"; " ") | if length > $n then .[0:$n] + "..." else . end;
    def hhmm: (.timestamp // "") | .[5:16] | sub("T"; " ");
    if .type == "custom-title" then "## \(.customTitle) [\($file[0:8])]"
    elif .type != "user" or .isSidechain == true then empty
    elif (.message.content | type) == "string" then
      (.message.content) as $c
      | if ($c | startswith("Stop hook feedback")) then "\(hhmm) HOOK     \($c | clip(400))"
        elif .isMeta == true then empty
        elif ($c | test("<command-name>")) then
          "\(hhmm) COMMAND  \($c | capture("<command-name>(?<n>[^<]*)</command-name>").n) \((($c | capture("<command-args>(?<a>[\\s\\S]*?)</command-args>")? | .a) // "") | clip(300))"
        else "\(hhmm) PROMPT   \($c | clip(400))" end
    else
      hhmm as $t
      | ( ( .toolUseResult | objects | select(.answers) | .answers | to_entries[]
            | "ANSWER   \(.value | clip(200))  <= \(.key | clip(140))" ),
          ( if .toolDenialKind then
              "DENIED   (\(.toolDenialKind)) \(.message.content[0].content | tostring | clip(220))"
            else
              ( .message.content[]? | objects
                | if .type == "text" and (.text | startswith("[Request interrupted")) then "INTERRUPT"
                  elif .type == "tool_result" and ((.content | tostring) | test("hook error|^Blocked:"))
                    then "BLOCK    \(.content | tostring | clip(220))"
                  else empty end )
            end ) )
      | "\($t) \(.)"
    end' "$f" | awk '/^## / { if ($0 == head) next; head = $0 } NR == 1 && !/^## / { print "## (untitled)" } { print }'
  echo
done
