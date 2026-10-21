---
name: finish
description: Close a feature or fix branch - verify green, commit what is left, merge it into develop with --no-ff, push develop, delete the branch locally and remotely. Use when the user says the task is done, ready to merge, or asks to finish the branch.
argument-hint: [branch]
---

# /finish $ARGUMENTS

Ends the branch `/task` cut. Every step that leaves the machine (push, remote delete) is asked
first; a conflict or a red gate stops the flow instead of being papered over.

## 0. Preconditions

- `git rev-parse --abbrev-ref HEAD` is a `feat/*` or `fix/*` branch (or the one named as the
  argument). On `develop` / `master`: stop — nothing to finish there.
- `git status --porcelain`: list what is dirty. Files this task did not touch (another session's
  work, Play-mode noise such as `* SDF.asset`, `ProjectSettings/`) are named and left alone.
- `git fetch -q origin`; `origin/develop` must exist.

## 1. Gate

Run `/verify`. The verdict must be **GREEN**. If it is not, report the failing rows and stop; the
user decides whether to fix first or override explicitly. **Editor busy** (here or after the merge)
stops the flow the same way: name the session holding the Editor; never retry in a loop.

## 2. Commit what belongs to the task

Uncommitted task files → ask, then commit them per `.claude/rules/commits.md` (Conventional Commit,
`git add <path>` per file). Show `git log --oneline origin/develop..HEAD` so the user sees what
will be merged.

## 3. Merge into develop

```bash
git switch develop
git pull --ff-only origin develop        # local develop diverged? stop and ask
git merge --no-ff <branch>               # default merge message; keeps the feature's history
```

A conflict stops the flow: report the conflicting files, leave the merge in progress for the user
(or `git merge --abort` if they say so). Never resolve a conflict silently.

After a clean merge, `/verify --no-review` on `develop`: the merge is the first time this feature
meets the others.

## 4. Push and delete (asked one by one)

```bash
git push origin develop
git branch -d <branch>                   # -d refuses if something was not merged; do not force
git push origin --delete <branch>        # only if the branch existed on origin
```

## 5. Report

`develop` head, the merge commit, the commits it brought in, branches deleted, anything left dirty.
The next `/task` starts from `origin/develop` again.

Then release the Editor for other sessions: `.claude/hooks/editor-lock.sh release`.
