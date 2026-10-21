---
name: sdk-update
description: Bring the shared SDK folder to a new ref and reconcile the game with it - facade assembly list, ArchitectureConfig, define symbols, installer, docs, architecture tests. User-invoked; the SDK is read-only inside the game repo.
argument-hint: [ref]
disable-model-invocation: true
---

# /sdk-update $ARGUMENTS

The SDK lives at the path in the project card (`Assets/SDK/`, `MoodBoost.Sdk.*` assemblies) and is
read-only here: it is edited in its own repo and arrives as a whole. Read `Assets/SDK/README.md`
section "Dropping it into a new project" before starting.

## 1. Preflight

- Uncommitted edits inside the SDK stop the update: `git -C Assets/SDK status --porcelain` when it is
  a submodule, `git status --porcelain -- Assets/SDK/` otherwise. If it is not empty, stop: those
  edits belong in the SDK repo; ask the user how they want them moved. A submodule showing only
  ` M Assets/SDK` in the parent's status has moved to new commits that are not recorded yet - that
  is a pending bump, not an edit: continue, and reconcile from the recorded ref
  (`git ls-tree HEAD Assets/SDK`) rather than from the checked-out one.
- Record the current state: `git -C Assets/SDK rev-parse HEAD` when it is a submodule
  (`.gitmodules` maps the path), else the "Current ref" line the last update left in the report.

## 2. Bring in the new ref

- **Submodule**: `git -C Assets/SDK fetch` → show `git -C Assets/SDK log --oneline HEAD..<ref>` →
  after the user confirms, `git -C Assets/SDK checkout <ref>` (default: the SDK's default branch
  tip; ask for the tag if none was given).
- **Not yet a submodule**: tell the user to replace the folder wholesale from the SDK repo at
  `<ref>` (including every `.meta`) and to keep `Assets/SDK.meta`; never hand-merge files.

## 3. Reconcile the boundary

`git diff --stat -- Assets/SDK/`, then compare:

- `find Assets/SDK -name '*.asmdef'` and each `defineConstraints` against
  `ArchitectureConfig.SdkFacadeAssemblies`, `SdkDiAssemblies`, `SdkForbiddenAssemblyPrefixes`,
  `VendorAssemblyPrefixes`, the `vendor-sdk-namespace` regex in `SourceRules.cs`, and the
  `references` of the Game and Core.Tests asmdefs. Propose the exact edits; a new provider assembly
  is a vendor unless the README says it is a facade.
- The installer entry point (`RegisterMoodBoostSdk` or the wrapper named in the card) — new
  parameters or new registered interfaces mean every scope's call site changes.
- `MB_*` defines the providers expect vs `grep -n MB_ ProjectSettings/ProjectSettings.asset`.
  Toggle them through the SDK settings window, or via `eval` `PlayerSettings.SetScriptingDefineSymbols`
  after the user confirms; `ProjectSettings/` is hook-protected from direct edits.
- Skills the SDK ships: every folder in `Assets/SDK/.claude/skills/` has a relative symlink
  `.claude/skills/<name> -> ../../Assets/SDK/.claude/skills/<name>` (`ls -l .claude/skills`). Add the
  link for a new one, remove the link of one the SDK dropped; never copy the folder over the link.

## 4. Compile and test

`unity command recompile` → `recompile_status` → `console --level error` →
`run_tests --mode EditMode`. `AssemblyDependencyTests` is the gate for this change; a red
`GameUsesSdkOnlyThroughFacade` means a game file reached past the facade — fix the game file.

## 5. Document and report

Update any game doc that names a changed API and the "Current ref" note. Report: old ref → new ref, files
changed outside the SDK, defines changed, tests. Commit on request:
`chore(sdk): bump moodboost sdk to <ref>`.
