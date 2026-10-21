---
name: task
description: Implement a code change end-to-end in this Unity project - doc lookup, plan, user go-ahead, edit, recompile, EditMode tests, baseline ratchet, doc update, architecture review. Use for any request to add, change or fix game code. Not for questions or research.
argument-hint: <what to change>
---

# /task $ARGUMENTS

The one loop every change goes through. It never edits before the user has approved the plan, and it
never commits unless asked. Work happens on a feature branch cut from `develop`; `/finish` merges it back.

## 0. Branch (read, no edits yet)

- `git fetch -q origin` and `git rev-parse --abbrev-ref HEAD`; `git status --porcelain` for the tree.
- On `develop` / `master`: the plan (step 2) proposes `feat/<slug>` or `fix/<slug>` (kebab-case
  English, from the task); the branch is cut only after the go-ahead:
  `git switch -c <name> --no-track origin/develop`. Nothing is edited on those branches.
- On a feature branch already: continue there if it is this task; if it is another task, or the
  tree holds uncommitted work that is not yours, ask before switching — one working tree, one branch.
- `origin/develop` missing → stop and say so; the user creates it.

## 1. Orient (read, no edits)

1. Restate the task in one line. If it is really a question, answer it and stop.
2. Read the project card in `CLAUDE.md`: note **Root**, **`<Game>`**, the scenes, the pool catalogs,
   the append-only enums, the "Play-testing consumes" keys. Every path you write below is relative
   to Root; every namespace starts with `<Game>`.
3. Map the task to a system with the **Which doc to read** table (first matching row wins). `Read`
   that doc with `limit: 20`. Read further only if the summary does not cover the change. Open the
   doc's **Start from** files by grepping to the symbol, not by reading whole files.
4. If the task is a new service / persisted data / settings SO → continue in `/new-system`; a new
   `MonoBehaviour` → `/new-view`; a new popup → `/new-popup`. Each of those has its own plan gate.
   UI built, rebuilt or restyled from a demo also runs `/ui-from-demo` steps 1–6: its spec joins
   the plan.

## 2. Plan and stop for the go-ahead

Write the plan as a short list:

- the branch: the current feature branch, or the `feat/<slug>` / `fix/<slug>` to cut from `origin/develop`;
- files to add or change, each with its folder from `.claude/rules/file-layout.md` and current line
  count (`wc -l`); any file that would cross 300 (or its baseline ceiling) is split first, and the
  split is part of the plan;
- which `builder.Register*` variant, if DI changes (decision table in `docs/stack/adding-a-system.md`);
- which append-only enum grows, which SettingsSO gains a field, which scene or prefab is touched;
- for UI built or restyled from a demo, the UI spec (demo, asset list, layout, text and button tables);
- which doc bullet and lookup row change;
- what the EditMode test will assert, against a contract that exists or that this plan names.

A plan that hinges on a design question with several defensible answers (data format, ownership,
sync model, algorithm) puts the options to the user with a recommendation and cites the choice.

Then **stop and ask** with `AskUserQuestion` (options: proceed / adjust). Do not touch a file until
the answer arrives. If the user changes the direction, re-plan and ask again.

## 3. Implement

0. Cut the branch decided in the plan if you are not on it yet.
1. Edit with the file tools. Re-read a file right before editing it (other sessions share the tree).
2. Protected paths (SDK, Plugins, Packages, TMP, vendor folders, `.meta`, `ProjectSettings/`) are
   blocked by a hook with a reason; do not work around it. Assets are created, moved or renamed
   through `AssetDatabase` in `unity command eval` so Unity writes the `.meta`.
3. Scene or prefab changes follow `.claude/rules/unity-editor-cli.md`: save the open scene before a
   recompile, reimport a prefab before editing it, never edit `.unity` / `.prefab` YAML by hand.
4. Never add an `ArchitectureBaseline` entry for new code.
5. A choice the plan did not settle that changes a contract, a tunable or a user-visible behaviour
   (including narrowing a requirement to make it fit) goes back to the user; it is not taken silently.
6. Before verifying, answer for yourself: which plan line and doc line does each changed contract
   rest on, and did an assertion that existed at `HEAD` change? An answer of "none" means stop and ask.

## 4. Verify

Run `/verify`. In short: `unity command recompile` → `recompile_status` until idle →
`console --level error` (fix until empty) → `run_tests --mode EditMode` (read failures from the
response) → if `BaselineHasNoPaidOffDebt` fails because this change paid debt off, lower or delete
that entry in the same change. `Blocked: the Unity Editor is held by ...` means another session is
using the Editor: finish what needs no Editor and report it; do not retry in a loop. UI built or
restyled from a demo also passes `/ui-from-demo` step 8 (side-by-side compare) before the report.

## 5. Document

Update the system's `docs/<system>.md` summary if an invariant changed; add a lookup row to
`CLAUDE.md` if new names were introduced (`AGENTS.md` regenerates itself). A new system gets its own
doc through `/write-doc`.

## 6. Review

Spawn the `arch-reviewer` agent on the working-tree diff. Fix every **Blocker**, then repeat step 4
if code changed. For each **Should fix**, either fix it or say why not in the report.

## 7. Report

- files changed, as paths;
- one line of test results (passed / failed counts);
- leftovers: files left over the limit, PlayerPrefs restored, anything skipped and why.

Then release the Editor for other sessions: `.claude/hooks/editor-lock.sh release`.

Commit only when the user asks, on the feature branch: Conventional Commit per
`.claude/rules/commits.md`, `git add <path>` for each path this task changed — never `git add -A`,
`git add .` or `git commit -a`. When the user says the task is done, run `/finish` to merge the
branch into `develop` and delete it.
