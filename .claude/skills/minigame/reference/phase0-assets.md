# Phase 0 - Assets

Goal: know before any question whether the game can be built from what is in the project.

## Run

`python3 ${CLAUDE_SKILL_DIR}/scripts/asset_scan.py` (add `--json` for the machine-readable form; never
redirect it into `Library/`, the path guard blocks that). Exit 1 = art missing. Phase 1 reruns it with
`--with <Plan screens in scope>` so their demos become required too.

## What counts

| Kind | Where | Missing means |
| --- | --- | --- |
| Gameplay art | `<Root>/Textures/` outside `UI/`, plus `<Root>/Prefabs/`, models under `<Root>/Mesh/` | stop: nothing to build the board from |
| Screen demos | `<Root>/Textures/UI/<Folder>/*demo*` per `screens.md` | stop for `Required` rows; `Plan` rows are checked again in Phase 1 |
| UI sprites | the same folders as their demo, plus `Textures/UI/Common/` | not checked here: `/ui-from-demo` step 2 lists them per screen, and a missing one becomes a Phase 1 question |
| GDD | `docs/<slug>/gdd.*`, or any `gdd*`/`*design*` `.md`/`.pdf`/`.docx` under `docs/` | Phase 1 asks the core questions |
| SFX | `.wav/.mp3/.ogg` under `<Root>/Audio*/` | the `feel` stage leaves cues silent and lists them in the report |
| Music | an audio file named `*music*`/`*bgm*` | silent menu and gameplay |
| VFX | prefabs with a `ParticleSystem` under `<Root>/Prefabs/` or a vendor pack the game added | the `feel` stage builds particle effects on the game's own sprites |
| Fonts | `<Root>/Fonts/` TMP presets | the card's presets are used |

## Stop message

When art is missing, print one table (file kind, folder, example name) and stop without asking
anything. Example: `Textures/UI/Win/win_demo.png - the Win popup as it should look, 1080 wide`.
