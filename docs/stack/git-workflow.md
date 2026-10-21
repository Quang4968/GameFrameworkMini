# Git workflow

Branch model of a game cloned from the framework. The rules are in `.claude/rules/commits.md`;
`/task` and `/finish` are the two ends of the flow. No hook enforces it: the team keeps
`develop` and `master` clean by convention.

## Summary

- `develop` integrates, `master` releases. **Nobody edits or commits on either**; `/task` cuts
  the feature branch before its first edit.
- Every feature or fix is one branch cut from `origin/develop`, named `feat/<slug>` or
  `fix/<slug>` (kebab-case English). It is merged back with `git merge --no-ff` and then deleted,
  locally and on origin.
- One working tree, one branch at a time. The Unity Editor is bound to this folder, so `git
  worktree` is not an option; switching over someone else's uncommitted work needs their say-so.
- A merge into `develop` is followed by `/verify` on `develop`: it is the first time the feature
  meets the others.
- Azure DevOps refuses a branch named `dev` while `dev/*` branches exist (a ref cannot be both a
  file and a directory); that is why the integration branch is `develop`.
- `Assets/SDK/` is a git submodule (`.gitmodules` names the SDK repo and its branch); the game repo
  pins one SDK commit. Clone with `git clone --recurse-submodules`, or run
  `git submodule update --init` after a plain clone; an empty `Assets/SDK/` means that step was
  skipped. Only `/sdk-update` moves the pin; `Assets/SDK.meta` stays in the game repo.

**Start from**: `.claude/skills/task/SKILL.md` (step 0), `.claude/skills/finish/SKILL.md`.

## Flow

```
origin/develop ──┬──────────────────────────────┬─── merge --no-ff ───▶ develop ──▶ push
                 │                              │                           │
                 └─ git switch -c feat/<slug> ──┴─ commits ─ /verify GREEN ─┘   branch deleted
                    --no-track origin/develop
```

| Moment | Who | Command |
| --- | --- | --- |
| Plan approved in `/task` | agent | `git fetch origin && git switch -c feat/<slug> --no-track origin/develop` |
| Work | agent | edits, `/verify`, commits on request (`git add <path>` per file) |
| "Done" | `/finish` | `/verify` GREEN → commit leftovers (asked) → `git switch develop && git pull --ff-only origin develop` → `git merge --no-ff feat/<slug>` → `/verify --no-review` → `git push origin develop` (asked) → `git branch -d feat/<slug>` → `git push origin --delete feat/<slug>` (asked) |
| Release | human | `master` ← `develop`, outside the toolkit |

## Traps

- **Dirty tree on switch**: git refuses to switch when tracked changes would be overwritten; the
  agent asks instead of stashing someone else's work. Play-mode noise (`* SDF.asset`,
  `ProjectSettings/`) is reverted or left, never committed by accident — hence `git add <path>`.
- **`git pull --ff-only` fails**: local `develop` has commits that are not on origin (someone
  committed there by hand). Stop; the user decides (`git rebase` or reset), the agent does not.
- **Merge conflict**: reported with the file list and left in progress; `git merge --abort` only on
  request.
- **`git branch -d` refuses**: the branch holds commits that did not make it into `develop`; do not
  force-delete, look at `git log develop..<branch>`.
- **Long-lived branch**: merge `origin/develop` into the feature branch (`git merge origin/develop`)
  when it falls behind; a conflict is cheaper to solve on the feature side.
- **Submodule on one side of a switch**: a branch cut before `Assets/SDK/` became a submodule
  tracks the SDK files directly. Switching onto it from a submodule branch is refused ("untracked
  working tree files would be overwritten"): `git submodule deinit -f Assets/SDK` first, switch,
  and `git submodule update --init` after coming back. Do it while the Editor is unfocused; the
  folder is empty in between.

## Porting

The branch names live in the text of `.claude/rules/commits.md`, `/task`, `/lead` and `/finish`.
The framework repo itself has no `develop`; a cloned game creates it (`new-project-checklist.md`).
A project that keeps `main` instead of `master` changes nothing else.
