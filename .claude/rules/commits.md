# Commits and branches

Branches:

- `develop` is the integration branch, `master` the release branch. Nobody edits or commits on
  either one. Every feature or bug fix lives on its own branch cut from `develop`: `feat/<slug>`
  or `fix/<slug>`, kebab-case English (`feat/daily-reward-popup`).
- `/task` cuts the branch after the plan is approved; `/finish` merges it back with
  `git merge --no-ff` into `develop`, pushes `develop`, and deletes the branch locally and remotely.
- One working tree, one branch at a time: the Unity Editor is bound to this folder, so worktrees are
  out. Never switch branches over a dirty tree that holds someone else's work; ask first.
- Long form: `docs/stack/git-workflow.md`.

Commits:

- English only. Subject `<type>(<scope>): <imperative summary>`, under 72 characters, lowercase
  type (`feat`, `fix`, `refactor`, `chore`, `docs`, `test`). No body unless the change needs
  essential context.
- Describe the product or code change, not the process used to make it.
- Never mention the agent, AI, LLMs, prompts, co-authorship or generated content in messages,
  bodies, trailers, branch names, code comments or documentation unless explicitly requested.
  No `Co-Authored-By` trailers.
- Commit only when asked, on the feature branch. Stage the paths this task changed, one by one.

Examples: `feat(popup): add pause dialog`, `fix(input): prevent duplicate tap events`,
`chore(deps): update addressables package`.
