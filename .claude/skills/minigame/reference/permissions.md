# Grants collected in Phase 1

Phase 2 must never stop on a permission prompt or a path guard. Everything the stages will do is
granted once, at the end of Phase 1, **by the user**: the agent never edits `.claude/settings*.json`
or `Library/Toolkit/unlock` itself (the path guard and the permission classifier refuse it, rightly).

## What the user runs

One command, shown in its own `bash` block with the plan:

```bash
python3 .claude/skills/minigame/scripts/grant.py
```

| Grant | Why | Where `grant.py` writes it |
| --- | --- | --- |
| `git add`, `git commit` | one commit per stage on `feat/<slug>` | `.claude/settings.local.json` allow |
| `.claude/skills/build-tiktok/scripts/*`, `python3 .claude/skills/minigame/scripts/*` | the build watcher, `state.py` | same |
| `UNITY_PROJECT_PATH` = this checkout | every `unity command` hits this project's Editor | `.claude/settings.local.json` env (next session on) |
| `ProjectSettings/` | `build` stage size fixes, WebGL template, quality levels | `Library/Toolkit/unlock` |
| `Assets/SDKSetting/` | `sdk` stage: TikTok block, ad units | `Library/Toolkit/unlock` |

`git commit` and `unity command build` must not sit in the `ask` list of `.claude/settings.json`
(an `ask` rule wins over a local `allow`); `git push`, `reset`, `checkout`, `clean`, `merge`, `stash`
stay there, and `/finish` (merge, push) is never part of the run.

After the run, `grant.py --revoke` removes exactly these grants; the report reminds the user.

## Rules

- The plan message lists this table, the inputs (appid, ad unit ids) and the command; approval is
  the user's reply after running it.
- Record the grants in `docs/<slug>/decisions.md` under "Grants".
- A permission the plan did not foresee is not requested mid-run: the stage that needs it is marked
  `fail` with the reason, and the report lists it.
- TTSDK `StarkBuilderSettings` (appid, `OutputDir` outside the project, default `~/Builds/<slug>-tiktok`)
  is written through `eval` in the `sdk` stage; it is not a protected path.
