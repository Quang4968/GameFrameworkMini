---
name: retro
description: Weekly retro over this project's Claude Code transcripts - where the user corrected course, answered "adjust", interrupted, or a hook blocked or questioned the agent - grouped into failure modes, each with a proposed change to a rule, skill, hook, doc or memory. Proposes only; changes go through /task. User-invoked, weekly or on demand.
argument-hint: [days=7]
disable-model-invocation: true
---

# /retro $ARGUMENTS

The loop that makes the instructions a few percent better each week. It reads, groups and
proposes; it edits nothing.

## 1. Extract

Run `"${CLAUDE_SKILL_DIR}/scripts/extract.sh" <days>` (the argument, default 7) with its output
redirected to a file in your scratch directory. It covers this checkout and its `.claude/worktrees/*` sessions. Each session is a `## <title>`
block of timestamped lines: `PROMPT`, `COMMAND`, `ANSWER` (AskUserQuestion answer `<=` question),
`INTERRUPT`, `DENIED` (permission or hook), `BLOCK` (hook error in a tool result), `HOOK` (Stop
feedback, e.g. the /minigame loop). Read it in slices (`wc -l`, then `sed -n`).

## 2. Find the moments

A moment is where the agent's direction and the user's diverged, or a guard fired:

- a `PROMPT` that corrects, repeats or narrows the previous request ("không phải", "ý t là",
  "again", "still", a pasted error), an `INTERRUPT`, an `ANSWER` that is not the recommended option;
- a `DENIED` / `BLOCK` that repeats across sessions (the agent keeps trying the same blocked thing);
- a `HOOK` line that fired again and again in one session (a loop that never closes a stage);
- a session title that repeats (the same task done twice, or redone).

For a moment that needs context, open that transcript (`~/.claude/projects/<slug>/<id>*.jsonl`,
slug = the project path with every non-alphanumeric character turned into `-`, id from the block
title) with `jq` around the timestamp; never load a whole transcript into context.

## 3. Group and check what already exists

Group moments into failure modes (at least two moments, or one costly one). For each, look for the
guard that should have caught it: `.claude/rules/`, the skills, the hooks, `docs/`, the memory
index (`MEMORY.md` in this project's memory folder). A mode already covered by a guard that failed
is more urgent than a new one.

## 4. Propose and stop

```
## Retro <from> → <to>: <n> sessions, <m> moments

### <failure mode>
Evidence: <session title [id], time — one line each, at most three>
Guard today: <file, or "none">
Proposal: <the smallest change: a rule line, a skill step, a hook check,
           a doc bullet, a memory note> — generic (template) or this game only
```

Order by cost of the failure. Keep a proposal generic only if it holds for any game on the
framework; otherwise it belongs to the game. Ask which proposals to take; each accepted one is
implemented through `/task`.
