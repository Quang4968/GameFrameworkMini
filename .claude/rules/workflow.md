# Workflow

- Building a game from its assets is `/minigame`: one interactive phase (assets, plan, every input
  and grant), then an autonomous loop. The approved build plan counts as the go-ahead for every stage
  and every skill the loop calls (`/task`, `/new-*`, `/ui-from-demo` spec); nothing stops to ask until
  the run ends (`.claude/skills/minigame/reference/autonomy.md`).
- Any other task starts with `/task`: read the project card in `CLAUDE.md`, then the one doc for the
  system being touched, then plan. **Present the plan and stop for the user's go-ahead before the
  first edit** — every task, even a one-file fix. The user decides the direction, not the agent.
- A design question with several defensible answers is put to the user as options with a
  recommendation before it is planned.
- Read a doc's first 20 lines only (`Read` with `limit: 20`): the summary block holds the cross-file
  invariants no single file states. Read further only when the summary is not enough.
- Source files carry dense header comments: grep to the symbol and read around it, never whole files.
- Game-owned files live under the root named in the project card (template default `Assets/_Dev/`,
  layout `Scripts/{Core,Game,Editor,Tests}`, `Data/`, `Prefabs/`, `Scenes/`...). Rules and skills
  name paths relative to that root; the namespace prefix (`<Game>`) comes from the same card.
- A new service, persisted data or settings SO → `/new-system`; a new `MonoBehaviour` → `/new-view`;
  a new popup → `/new-popup`; UI built or restyled from a demo → `/ui-from-demo`; a new system doc
  → `/write-doc`.
- After every C# change run `/verify` (`unity command run_tests --mode EditMode` underneath); inside
  a `/minigame` run, `/verify --no-review` per stage and the review once at the end. A red
  architecture test is a code bug, never a baseline edit.
- Update the system's doc in the same change that alters its behaviour; no parallel doc set.
- `Assets/SDK/`, `Assets/Plugins/`, `Assets/Packages/`, `Assets/TextMesh Pro/`, vendor folders,
  `.meta` files, `Packages/` and `ProjectSettings/` are read-only; a hook blocks edits with the
  reason. SDK changes happen in the SDK repo and arrive through `/sdk-update`.
- Generated code must be indistinguishable from hand-written code: module namespace, naming,
  formatting, placement. No authorship banners, generated-code markers, prompt transcripts, task
  summaries, backup files, or files and folders named after an agent or model.
- Commit `.meta` files only when Unity generated them for a new or moved asset.
- Parallel agent sessions share one working tree and one Editor: re-read a file before editing it,
  stage only your own paths (never `git add -A`), and check `unity command editor_status` before a
  recompile or Play mode.
- One session drives the Editor at a time: a hook takes the Editor lock on the first `unity command`
  that is not a pure read and refuses other sessions until it is released (`/task` step 7, session
  end, or 15 min unused). Blocked means do the work that needs no Editor, never retry in a loop.
