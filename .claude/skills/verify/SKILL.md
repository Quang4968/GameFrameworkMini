---
name: verify
description: Prove the working tree compiles and passes the architecture gate - recompile, console, EditMode tests, 300-line check, baseline ratchet, stray-file check, template hygiene, architecture review. Use before any commit, after any C# change, or when asked whether the change is green.
argument-hint: [--no-review]
---

# /verify $ARGUMENTS

Read-only apart from the Editor recompile. It never commits and never edits code; it reports.

## 0. Editor

`unity command editor_status`. If `Library/Pipeline/.unity-pipeline-port` is missing or the command
fails, stop and report **Editor not running** — do not invent a result. If the status is `compiling`
or a domain reload is in progress, wait 10 s and retry (three times). A `recompile` or `run_tests`
refused with `Blocked: the Unity Editor is held by ...` means another session drives the Editor:
report **Editor busy** with the holder, run the local steps (3, 5, 6), and stop; never retry in a loop.

## 1. Compile

`unity command recompile` → poll `unity command recompile_status` until `"compiling": false` →
`unity command console --level error`. Any compiler error = fail; list each `file:line message`.

## 2. Tests

`unity command run_tests --mode EditMode`. Parse `Summary` (`Total`, `Passed`, `Failed`) and every
non-passed result's `FullName` + `Message` from the response (`test_status` reports `no_tests`
afterwards, so this response is the only record).

## 3. Line limit (local, no Editor)

Resolve the scripts root from the project card. For every `.cs` under it:
`awk 'END { if (NR > 300) print FILENAME ": " NR }'`. A listed file is fine while it stays at or
under its `ArchitectureBaseline.FileLines` ceiling; anything else over 300 is a fail.

## 4. Baseline ratchet

If `BaselineHasNoPaidOffDebt` failed in step 2, print the exact entries to lower or delete. If
`NoNewViolations` failed, print the rule id, file, lines and the rule's fix text — the fix is in the
code, not in the baseline.

## 5. Stray files

`git status --porcelain`, then flag:

- a `.meta` without its asset, or an asset without its `.meta`;
- PNG captures under `Assets/` (`Temp/*.png` from `capture_game_view`);
- a modified `* SDF.asset` (TMP atlas dirtied by Play mode → `git checkout -- <file>`);
- modified `ProjectSettings/`, `Packages/`, or anything under a protected path.

## 6. Template hygiene

- `grep -rn "<Game>" .claude docs/stack` (the concrete prefix from the project card) must print
  nothing: template files never name the game.
- `grep -rn 'Assets/_Dev' .claude/skills .claude/rules` must print nothing except the one default
  mention in `workflow.md`.
- `ArchitectureConfig.ScriptsRoot` and `GameAssemblyPrefix` match the project card.
- `.claude/hooks/build-agents-md.sh` followed by `git diff --quiet AGENTS.md` — a diff means
  `AGENTS.md` was stale; leave it regenerated and say so.

## 7. Review

Unless `--no-review`: spawn the `arch-reviewer` agent on `git diff HEAD` plus untracked files under
the scripts root. Quote its Blockers verbatim.

## 8. Report

One fixed table, then details only for failing rows:

```
| compile | console | tests (p/f) | lines | baseline | stray | hygiene | review | verdict |
```

Verdict is **GREEN** only when every column is clean and the reviewer returned READY.
