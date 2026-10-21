# Phase 1 - The plan

The plan is the contract for an unattended run: anything left vague here becomes a guess in Phase 2.

## Inputs, in order

1. `asset_scan.py` result.
2. GDD (all of it - it is the one document the user wrote).
3. Every demo: `uiref.py grid <demo>` and read it. The demos reveal mechanics the GDD forgets
   (a booster bar means boosters, a star row means star rating).
4. `docs/stack/popup-behaviours.md` and `reference/screens.md`: what each screen must do.
5. The card's append-only enums: what the game will append (`BoosterType`, `GameFeature`,
   `E_TutorialId`, `AudioCue`, `PopupId`, `HapticProfile`).

## Questions

One `AskUserQuestion` round, at most four questions, each with a recommended option first. Ask only
what changes the build: core loop or win/lose when no GDD, level count, target session length,
difficulty shape, which `Plan` screens are in. A detail with a sensible default is decided, written
in the plan, and not asked.

## The plan file

`docs/<slug>/build-plan.md` from `templates/build-plan.md.tmpl`. Rules:

- Every number is written: win-rate targets per level band, tolerance, simulation runs per level,
  interstitial cap/cooldown/start level, revive cost steps, coin rewards, package budget.
- Every screen in scope names its demo file.
- Every stage lists its done-check (the test or capture that proves it).
- Monetization is rewarded and interstitial only: placements, triggers, caps. The short-of-coins
  rule (rewarded coins offer or dimmed button) is decided here.
- The section "Decisions the run may take alone" lists the defaults Phase 2 applies without asking.

## Presenting

Show the plan file path, a 10-line summary, the input list (appid, ad unit ids) and the grant table
from `permissions.md`, then stop. The approval message must contain the inputs; a missing appid or
ad unit id is the one thing still allowed to block, and it blocks here, not later.
