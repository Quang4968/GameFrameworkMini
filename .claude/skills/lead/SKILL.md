---
name: lead
description: Run a batch of tasks toward one goal as the lead - split into tasks, settle open design with /design, present one batched plan for a single go-ahead, then execute task by task (yourself or a peer agent) holding the Editor lock, with supervisor checkpoints and a report of decisions and disagreements. Use for long or multi-part work the user wants handled with few interruptions.
argument-hint: <goal, or a list of tasks>
disable-model-invocation: true
---

# /lead $ARGUMENTS

You coordinate; you do not grind. Keep your context for decisions: delegate exploration and
implementation to agents, read their reports, decide. The user approves once per batch and is
asked again only for what the plan did not settle.

## 1. Orient

1. Read the project card in `CLAUDE.md` and the lookup rows the goal touches (docs: first 20 lines).
2. Duplicate check: `git fetch -q`, `git log --oneline HEAD..@{upstream}` and the integration
   branch the card names, `git status --porcelain`. Work already landed or half-done in the tree by
   another session is listed in the plan, not redone.
3. `.claude/hooks/editor-lock.sh status`: if another session holds the Editor, the batch can still
   plan and design; implementation waits for it.

## 2. Split and settle

- Split the goal into tasks small enough for one `/task` each; order them by dependency.
- A task with an open design question (several defensible answers) goes through `/design` first;
  its approved outcome is the task's plan input. Run independent `/design`s in parallel.
- A branch in the road (the task needs something that does not exist yet, such as authorization
  finding no authentication) becomes its own task before the one that needs it; never fill it in
  silently inside the other.

## 3. Batched plan and stop

One plan for the batch, each task in the `/task` step 2 shape (files and folders, DI variant, enums,
SOs, scenes, docs, test assertion) plus: who does it (you or a `peer`), what it depends on. Ask once
with `AskUserQuestion` (proceed / adjust). No edit before the answer; a changed direction means
re-plan and ask again.

## 4. Execute, task by task

Before the first brief, cut the batch's `feat/<slug>` / `fix/<slug>` from `origin/develop` as
`/task` step 0 does: peers never switch branches and nothing is edited on `develop`. Every task
commits on that branch; `/finish` merges it when the user says the batch is done.

For each task, in order:

1. Brief: the approved plan for this task only, the doc lines it must honour, what counts as a
   decision to hand back. Neutral wording: never "do A or B", always the goal and the constraints.
2. Run it: small tasks yourself through `/task` steps 3-6; larger ones through a `peer` agent in the
   foreground (one implementing peer at a time: one tree, one Editor). Design-only or read-only
   pieces may run as parallel peers.
3. Checkpoint: spawn `supervisor` with the brief and the peer's report or `git diff HEAD -- <paths>`.
   Answer its questions; fix what the answers show.
4. `DECISION NEEDED` from a peer: inside the approved plan's intent → decide and log it; changes a
   contract, a user-visible behaviour or another system → collect it for the user (batch the
   questions; keep working on what does not depend on them).
5. Take a peer's pushback seriously: if it is right, change the plan and log why.
6. `/verify` when the task's code is in; the next task starts only on GREEN.

## 5. Report

```
| task | result | files | tests | verify |
Decisions taken: <what, why, who proposed>
Disagreements: <peer vs lead, supervisor questions that changed something>
Waiting on you: <collected decisions, each with options and a recommendation>
```

Release the Editor lock (`.claude/hooks/editor-lock.sh release`). Commit only when the user asks,
one task per commit, staging each task's paths by name.
