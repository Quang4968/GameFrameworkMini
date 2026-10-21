---
name: minigame
description: Build a complete TikTok Mini Game from the art already in the project - check the assets, settle one detailed plan with the user (gameplay, level generator and difficulty simulation, UI from demos, SFX/VFX, tutorial, SDK, monetization, build) and every permission it needs, then run the whole pipeline autonomously stage by stage to a TikTok build, with no further input. Use when asked to make, build or finish a game from its assets, or to resume a /minigame run.
argument-hint: <game-slug> | resume | status
---

# /minigame $ARGUMENTS

One entry point for a whole game. The user is in the loop exactly once, in Phase 1; after the plan
is approved every stage runs, verifies and commits on its own. Long form:
`docs/stack/minigame-pipeline.md` (`limit: 20` first). Everything here is relative to the project
card in `CLAUDE.md` (Root, `<Game>`, scenes, canvas reference, TMP presets).

Tools in `${CLAUDE_SKILL_DIR}/scripts/` (run with `python3`):

- `asset_scan.py [--root <Root>] [--json]` - inventory and the missing list; exit 1 when art is missing.
- `state.py <cmd>` - the run state in `Library/Toolkit/minigame/state.json`
  (`init <slug>`, `start`, `next`, `done <id> [note]`, `fail <id> <reason>`, `pause`, `resume`, `status`).

The Stop hook `.claude/hooks/minigame-continue.sh` keeps the session working while the state says
`running` and a stage is open. Never stop a running pipeline to ask; `state.py pause` is the only
way to hand the turn back on purpose.

## Arguments

- `<game-slug>`: kebab-case, names `docs/<game-slug>/`. New run → Phase 0.
- `resume`: `state.py status`; `planning` → Phase 1, `running` → Phase 2 at `state.py next`.
- `status`: print `state.py status` and stop.

## Phase 0 - Assets (no questions yet)

`python3 ${CLAUDE_SKILL_DIR}/scripts/asset_scan.py`. It classifies everything under the card's asset
roots and checks, per screen in `reference/screens.md`, that a demo exists.

- **Art missing** (gameplay sprites, or a demo for a required screen) → print the exact list, the
  folder each file goes in, and stop. Nothing else starts until the art is there.
- GDD and SFX are optional: note what is absent, it becomes a Phase 1 question or a fallback.

Details: `reference/phase0-assets.md`.

## Phase 1 - Plan (the only interactive phase)

1. Read the GDD if present, the demos (`uiref.py grid` each), and `docs/stack/popup-behaviours.md`.
2. Ask only what the GDD and art do not answer, in one `AskUserQuestion` round (up to 4 questions,
   each with a recommended default). No GDD → core loop, win/lose, level count, session length,
   difficulty curve are the first candidates.
3. `state.py init <slug>`, then write `docs/<slug>/build-plan.md` from
   `templates/build-plan.md.tmpl`: every section filled, every number explicit (targets, caps,
   cooldowns, budgets). Details: `reference/phase1-plan.md`.
4. Collect **every** input and permission the run will need, in the same message as the plan:
   TikTok appid, rewarded and interstitial ad unit ids, and the grant table of
   `reference/permissions.md` with the one command the user runs (`scripts/grant.py`). Nothing may
   be asked after this point.
5. Stop for the go-ahead (the one stop of the run). Adjust → rewrite the plan, ask again.
6. On approval (inputs given, `grant.py` run): `state.py init <slug>` happened at step 3, now
   `state.py start`; cut `feat/<slug>` (repo without git → `git init` and a baseline commit first,
   staging paths by name); create `docs/<slug>/decisions.md` from its template. Go to Phase 2.

## Phase 2 - Autonomous loop

Repeat until `state.py next` prints `done`:

1. `state.py next` → the stage id and its reference file. Read that file, then the build plan section
   it names.
2. Do the stage. Every rule in `reference/autonomy.md` applies: which gates are auto-approved, how a
   decision is taken and logged, Editor discipline, retries.
3. Gate: `/verify --no-review` (compile, console, EditMode tests, 300 lines, baseline). Green →
   commit the stage's paths by name (`feat(<slug>): <stage>`), `state.py done <id> "<one line>"`.
   Red → fix; after three failed attempts `state.py fail <id> "<reason>"` and move on.

| id | Stage | Reference |
| --- | --- | --- |
| core | Gameplay rules, domain, use cases, tests | `reference/stage-core.md` |
| views | Board views, input, `GameplayBridge` wiring | `reference/stage-views.md` |
| balance | Level generator, simulation, difficulty tuning | `reference/stage-balance.md` |
| ui | Every screen from its demo | `reference/stage-ui.md` |
| feel | SFX and VFX | `reference/stage-feel.md` |
| tutorial | First-session lessons | `reference/stage-tutorial.md` |
| sdk | TikTok SDK wiring and tracking | `reference/stage-sdk.md` |
| monet | Rewarded and interstitial placements | `reference/stage-monet.md` |
| smoke | Play-mode run through the whole flow | `reference/stage-smoke.md` |
| build | Size fixes and the TikTok package | `reference/stage-build.md` |
| report | Architecture review and the final report | `reference/stage-report.md` |

## Phase 3 - Report

The `report` stage writes `docs/<slug>/report.md` from `templates/report.md.tmpl` (stages, blockers,
decisions, balance curve, build sizes, device test checklist), sends it with `SendUserFile`, and
ends the run with `state.py done report`.
