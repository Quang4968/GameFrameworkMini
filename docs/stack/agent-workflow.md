# Agent workflow

How agent sessions share this project: the Editor lock, the path guard, and the hooks behind them.
Read before touching `.claude/hooks/`, `.claude/agents/` or `/retro`. The game pipeline and its loop
hook are in `minigame-pipeline.md`.

## Summary

- **One session drives the Unity Editor at a time.** `editor-lock.sh` takes
  `Library/Toolkit/editor-lock` in the **main checkout** (linked worktrees share it) on the first
  `unity command` that is not a pure read (`editor_status`, `*_status`, `console`, `get_*`,
  `find_*`, `list_*`). Other sessions are refused with the holder's id; without the lock a second
  session's recompile or Play interleaves with the first and discards its unsaved scene.
- The lock is keyed by `session_id`, which **subagents share with their parent**. It frees on
  `/task` step 7, on SessionEnd, or after 15 min unused; only the user breaks it
  (`editor-lock.sh release --force` from a terminal; the hook refuses it to agents).
- **One project per command.** The lock does not know which Editor a command reaches: with several
  Editors open, auto-detection may hit another project. `UNITY_PROJECT_PATH` or `--project-path`
  after the subcommand (`.claude/rules/unity-editor-cli.md`).
- **The path guard** (`protect-paths.sh`) refuses edits under `protected-paths.txt`, `.meta` edits
  and blanket staging. Only the user lifts it, per prefix, in `Library/Toolkit/unlock`.
- **Few hooks block.** PreToolUse: path guard, Editor lock. PostToolUse: file length (feedback,
  never blocks), `AGENTS.md` regeneration, test-run marker. Stop: the `/minigame` loop. SessionStart:
  orientation. No hook asks the agent questions.
- **A test never mints an API.** A member added so a test or fake compiles pins a design nobody
  made; later sessions bend the code to it. Guarded by `rules/tests.md` and item 14 of
  `arch-reviewer`.

**Start from**: `.claude/hooks/editor-lock.sh`, `.claude/hooks/protect-paths.sh`,
`.claude/settings.json`.

## Roles

```
user ── approves a /task plan or a /minigame build plan, runs grant.py, reads the report
 │
session ── /task (one change) or /minigame (whole game), holds the Editor lock
 └── arch-reviewer (agents/arch-reviewer.md) ── read-only review before a commit / at the end of a run
hooks ── protect-paths, editor-lock (PreToolUse) · check-file-length, build-agents-md, mark-tests-ran
         (PostToolUse) · minigame-continue (Stop) · session-start · editor-lock (SessionEnd)
```

`/retro` reads the week's transcripts and proposes changes to rules, skills, hooks or memory;
accepted ones go through `/task`.

## Traps

- A command that merely mentions `unity command` inside quotes or a grep pattern does not take the
  lock; only a command in command position does (start, after `;`, `&&`, `|`, `(`, `$(`). A script
  that calls `unity command` inside itself is invisible to the hook: call the CLI directly, as
  `.claude/rules/unity-editor-cli.md` already requires. An option before the subcommand
  (`unity command --project-path ... eval`) makes every call take the lock; put it after.
- Hooks write under `Library/Toolkit/`, which the path guard protects from agents' shell commands:
  change lock or session state through the hook scripts and `state.py`, never by editing those files.
- The path guard matches a protected prefix **anywhere** in a mutating command, so `Assets/Temp/x`
  trips the `Temp/` entry and a heredoc that mentions `Assets/Plugins/` in a doc string is refused.
  A `!prefix|reason` line in `protected-paths.txt` carves a writable folder out of a protected one
  (`!Temp/uiref/` for `/ui-from-demo`); a path through `..` never counts as carved.
- `build-agents-md.sh` run by hand waits on stdin: run it with `< /dev/null`.

## Test

No EditMode test covers the hooks. Change one, then replay it in a throwaway git repo with a
linked worktree: pipe hook JSON (`session_id`, `hook_event_name`, `tool_input`) into the script with
`CLAUDE_PROJECT_DIR` pointing at the repo, and check the exit code and stderr per scenario.
