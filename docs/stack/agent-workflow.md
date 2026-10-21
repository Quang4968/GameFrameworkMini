# Agent workflow

How agent sessions share this project: lead, peers and supervisor (SLP), the Editor lock, and the
hooks behind them. Read before touching `.claude/hooks/`, `.claude/agents/` or the skills below.

## Summary

- **One session drives the Unity Editor at a time.** `editor-lock.sh` takes
  `Library/Toolkit/editor-lock` in the **main checkout** (linked worktrees share it) on the first
  `unity command` that is not a pure read (`editor_status`, `*_status`, `console`, `get_*`,
  `find_*`, `list_*`). Other sessions are refused with the holder's id; without the lock a second
  session's recompile or Play interleaves with the first and discards its unsaved scene.
- The lock is keyed by `session_id`, which **subagents share with their parent**: a lead and its
  peers hold one lock. It frees on `/task` step 7, on SessionEnd, or after 15 min unused; only the
  user breaks it (`editor-lock.sh release --force` from a terminal; the hook refuses it to agents).
- **The supervisor asks, it never judges.** A question like "which plan line asked for `X`?" makes
  the agent re-examine; "you violated `X`" makes it invent a fault to agree. Keep every question in
  `supervisor-events.sh`, `invariant-paths.txt` and `agents/supervisor.md` neutral.
- Two supervisor tiers. **Cheap**: `supervisor-events.sh` on Stop/SubagentStop fires on a Core
  contract grown beside its test, an existing assertion rewritten, a file edited 5+ times, or an
  edit under `invariant-paths.txt`; once per question per session, only for files this session
  edited (`track-edits.sh`). **Thinking**: the `supervisor` agent, called by `/lead` and `/design`
  at checkpoints. Prompt-type hooks are not used: they see hook metadata, not the diff.
- **A test never mints an API.** A member added so a test or fake compiles pins a design nobody
  made; later sessions bend the code to it. Guarded by `rules/tests.md`, the `contract` and
  `assert` supervisor events, and item 14 of `arch-reviewer`.
- **Peers may disagree.** `agents/peer.md` lets a peer push back and propose an option the lead did
  not list, and makes it hand back unauthorised choices as `DECISION NEEDED` instead of deciding.
  A lead that offers only "A or B" gets only A or B.
- `/design` runs **blind** lanes: one neutral brief, the lead's own hypothesis kept out of it, lanes
  never pooled into one conversation (the strongest arguer wins, not the best design).
- `invariant-paths.txt` is **per game**, like `protected-paths.txt`: the template ships the seam
  entries; a game replaces them with its own invariants and the doc that holds each one.

**Start from**: `.claude/hooks/editor-lock.sh`, `.claude/hooks/supervisor-events.sh`,
`.claude/skills/lead/SKILL.md`.

## Roles

```
user ── approves one batched plan, answers collected decisions, reads the report
 │
lead (/lead) ── splits, runs /design, briefs peers, decides inside the plan, holds the Editor lock
 ├── peer (agents/peer.md) ── one brief; pushback + DECISION NEEDED
 ├── Plan lanes (/design) ── blind, parallel, read-only
 └── supervisor (agents/supervisor.md) ── open questions at checkpoints
hooks ── editor-lock (PreToolUse, SessionEnd) · track-edits (PostToolUse) · supervisor-events (Stop)
```

Use `/lead` for long or multi-part work, `/design` for questions with several defensible answers,
plain `/task` for everything else. `/retro` reads the week's transcripts and proposes changes to
rules, skills, hooks or memory; accepted ones go through `/task`.

## Traps

- A command that merely mentions `unity command` inside quotes or a grep pattern does not take the
  lock; only a command in command position does (start, after `;`, `&&`, `|`, `(`, `$(`). A script
  that calls `unity command` inside itself is invisible to the hook: call the CLI directly, as
  `.claude/rules/unity-editor-cli.md` already requires.
- Hooks write under `Library/Toolkit/`, which the path guard protects from agents' shell commands:
  change lock or session state through the hook scripts, never by editing those files.
- The path guard matches a protected prefix **anywhere** in a mutating command, so `Assets/Temp/x`
  trips the `Temp/` entry. A `!prefix|reason` line in `protected-paths.txt` carves a writable folder
  out of a protected one (`!Temp/uiref/` for `/ui-from-demo`); a path through `..` never counts as
  carved. The user's one-off override stays `Library/Toolkit/unlock`.
- Stop hooks feed back once per turn (`stop_hook_active`); a question over the limit of three per
  stop waits for the next one rather than being dropped.

## Test

No EditMode test covers the hooks. Change one, then replay it in a throwaway git repo with a
linked worktree: pipe hook JSON (`session_id`, `hook_event_name`, `tool_input`) into the script with
`CLAUDE_PROJECT_DIR` pointing at the repo, and check the exit code and stderr per scenario.
