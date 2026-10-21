# Running unattended

The approved build plan replaces every per-task stop of the normal workflow. This file is what
changes, and what does not, while `state.py status` says `running`.

## Gates that are auto-approved

| Normally | In the run |
| --- | --- |
| `/task` step 2 stops for the plan | the stage reference is the plan: no stop |
| `/new-system`, `/new-view`, `/new-popup` stop for the go-ahead | no stop; their checklists still apply |
| `/ui-from-demo` step 6 stops on the spec | the spec is written to `docs/<slug>/ui/<screen>.md` and approved by the run; step 8's ✓/✗ compare is the gate |
| A question to the user | a decision (below) |
| `/verify` runs the arch review | `/verify --no-review` per stage; the review runs once in `report` |

What never changes: the rules in `.claude/rules/` (layering, 300 lines, append-only enums, pooling,
DI per scene), protected paths not granted in Phase 1, and the stage gate.

## Decisions

When the plan does not answer something:

1. Take the default the plan's "Decisions the run may take alone" names; else the option that keeps
   the framework's existing behaviour; else the smallest one that satisfies the screen contract.
2. Append one line to `docs/<slug>/decisions.md`: stage, question, choice, why.
3. Never pause for it. `state.py pause` is for one case only: the Editor is unreachable after the
   retries below.

## Retries and blockers

- A stage gets three attempts (`state.py fail` counts them). The third failure marks it `blocked`,
  records the reason, and the run moves on; later stages that depend on it note the gap.
- Compile errors are fixed in the same attempt; a red architecture test is a code fix, never a
  baseline edit.

## Editor discipline

- Every `unity command` targets this project: `UNITY_PROJECT_PATH` is set in `.claude/settings.json`;
  without it, put `--project-path "<repo root>"` after the subcommand. Before the first mutating call
  of a stage, `editor_status` and check `projectPath`.
- Editor unreachable: wait with `editor_status` every 30 s for 5 minutes, then `state.py pause`
  with the reason (the user restarts it and runs `/minigame resume`).
- Save the open scene before a recompile; reimport a prefab before and after editing it; snapshot
  and restore the card's play-testing PlayerPrefs around Play mode; revert the TMP atlas afterwards.
- The path guard blocks any shell command that names a protected path or a `.meta` file, even
  inside a string: put such code in a script file under the scratchpad and run the file.

## Commits

`git add` each path the stage touched (never `-A`), commit `feat(<slug>): <stage summary>` on
`feat/<slug>`. No push, no merge: `/finish` is the user's.
