# Stage `balance` - level generator and difficulty simulation

Plan section: Difficulty (curve, tolerance, runs per level, the bot's skill).

The difficulty of every shipped level is measured, not guessed: a bot plays each level many times
through the same Core use cases the views call, and the generator is tuned until the measured
win-rate curve sits inside the plan's tolerance.

## Build

1. **Generator** (`Core/GameLogic/<Game>/<Game>LevelGenerator.cs`): pure, seeded
   (`System.Random(seed)`), driven by a parameter struct (`<Game>GenParams`: piece count, colours,
   blockers...) and guaranteed solvable by construction or by a solver check.
2. **Bot** (`Core/GameLogic/<Game>/<Game>Bot.cs`): picks a move from the legal moves; a `skill` 0..1
   blends the best move (by a cheap heuristic) with a random one. Skill values for "new player" and
   "average player" come from the plan.
3. **Simulator** (`templates/LevelSimulator.cs.tmpl` → `Core/GameLogic/<Game>/LevelSimulator.cs`):
   plays a level to win/lose/move cap with the use cases, returns `SimResult`.
4. **Balance run** (`templates/BalanceReportTests.cs.tmpl` → `Tests/EditMode/Core/<Game>/`): for each
   level band, N seeded runs, win rate and average moves, written as CSV to
   `Library/Toolkit/minigame/balance.csv`. Marked `[Explicit, Category("Balance")]` so `/verify` stays
   fast; run it with `unity command run_tests --mode EditMode --filter "BalanceReportTests" --include_explicit true`.
5. **Tuning loop**: compare the CSV with the plan's target per band; move one `GenParams` field per
   iteration in the direction that closes the gap; regenerate; rerun. Stop when every band is
   within tolerance, or after 12 iterations (log the best one as a decision).
6. **Export**: write the final levels as data (`<Root>/Data/<Game>/Levels/` or one JSON per level),
   the `GenParams` per band in a `<Game>DifficultySettingsSO`, and the curve table into
   `docs/<slug>/balance.md` (band, target, measured, runs).

## Done when

Every band within tolerance (or the closest result logged), levels exported, normal gate green.
The CSV and `balance.md` go into the report.
