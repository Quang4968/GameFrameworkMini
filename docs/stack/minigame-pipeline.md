# Minigame pipeline

How `/minigame` turns a project's art into a TikTok Mini Game with one user approval. Read before
touching `.claude/skills/minigame/`, `minigame-continue.sh` or a running game's `docs/<slug>/`.

## Summary

- **One interactive phase.** Phase 0 checks the art, Phase 1 asks at most one round of questions,
  writes `docs/<slug>/build-plan.md` and collects every input (appid, ad unit ids) and grant; after
  the approval nothing asks again. A question the plan did not settle is a logged decision
  (`docs/<slug>/decisions.md`), never a stop.
- **Art first.** `asset_scan.py` exits 1 when gameplay art or a demo of a required screen
  (`reference/screens.md`) is missing, and the run does not start. The base has no UI: every screen is
  built from its demo.
- **The state file is the loop.** `Library/Toolkit/minigame/state.json` (`state.py`) holds the eleven
  stages; the Stop hook `minigame-continue.sh` hands the turn back while it says `running` and a stage
  is open. Brakes: 30 stops on one stage count as a failed attempt, three failed attempts block the
  stage and the run moves on, 400 stops pause the run.
- **Grants are the user's.** `grant.py`, run by the user at the end of Phase 1, writes
  `.claude/settings.local.json` (git add/commit, build scripts, `UNITY_PROJECT_PATH`) and
  `Library/Toolkit/unlock` (`ProjectSettings/`, `Assets/SDKSetting/`); `--revoke` removes exactly
  that. The agent never edits those files. `git commit` and `unity command build` must stay out of
  the `ask` list of `.claude/settings.json`, or the local allow is overruled and the run stalls.
- **Every stage ends on the gate**: `/verify --no-review` green, then one commit of the stage's paths
  on `feat/<slug>`. The arch review runs once, in the `report` stage. No push, no merge.
- **Target the Editor explicitly.** Several Editors may run at once and the CLI's auto-detection
  picks any of them; `UNITY_PROJECT_PATH`, or `--project-path` after the subcommand.
- **Difficulty is measured.** The `balance` stage plays every level with a seeded bot through the
  game's own use cases and tunes the generator until each band's win rate is inside the plan's
  tolerance (explicit `BalanceReportTests`, CSV in `Library/Toolkit/minigame/balance.csv`).
- **TikTok only**: rewarded and interstitial, no IAP, no consent popup, `MB_TIKTOK` the one define.

**Start from**: `.claude/skills/minigame/SKILL.md`, `.claude/skills/minigame/reference/autonomy.md`,
`.claude/hooks/minigame-continue.sh`.

## Flow

```
/minigame <slug>
  Phase 0  asset_scan.py ── art missing ──> list + stop
  Phase 1  GDD + demos -> questions (one round) -> build-plan.md + inputs + grant table
           user: answers, runs grant.py, approves
           state.py start, feat/<slug>
  Phase 2  loop: state.py next -> stage reference -> work -> /verify --no-review
                 green: commit, state.py done      red x3: state.py fail -> blocked, next
           Stop hook: running + open stage -> "continue"
  Phase 3  report stage: arch review, docs/<slug>/report.md, SendUserFile, state done
```

## Stages

`core` → `views` → `balance` → `ui` → `feel` → `tutorial` → `sdk` → `monet` → `smoke` → `build` →
`report`; each has `reference/stage-<id>.md` with what to build and its done-check.

## Traps

- The path guard blocks any shell command naming `Library/`, a `.meta` file or another protected
  path, even in a redirect or a string: write such code to a scratch script and run the file.
  `state.py` writes the state itself, so its command line never names `Library/`.
- `stop_hook_active` is ignored on purpose: the loop must survive many stops in a row. The brakes
  above are what ends a stuck run.
- `state.py init` refuses while a run is `running` or `paused`; finish or resume it first.
- A resumed session (`/minigame resume`) restarts at the open stage: work committed before the
  interruption is on the branch, uncommitted work is in the tree; read `git status` first.

## Test

`state.py` and `grant.py` run against a scratch copy (`CLAUDE_PROJECT_DIR` for `state.py`, a copied
tree for `grant.py`); the hook is replayed by piping `{}` into it with `CLAUDE_PROJECT_DIR` pointing at
a scratch folder that holds a state file in each status.
