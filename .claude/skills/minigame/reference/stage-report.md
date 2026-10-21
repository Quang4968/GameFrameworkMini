# Stage `report` - review and hand-over

1. `/verify` with the review: the `arch-reviewer` agent on the whole branch
   (`git diff <baseline>...HEAD` plus untracked files). Fix its blockers in this stage (one attempt
   each), list the rest.
2. `docs/<slug>/report.md` from `templates/report.md.tmpl`: stage table (done / blocked, commit,
   one line), blockers with reasons, `decisions.md` digest, balance curve, UI compare images, smoke
   captures, build sizes, missing assets (sprites, clips), the device test checklist, and what the
   user does next (device test, `/finish` to merge).
3. Update `CLAUDE.md`'s project card for the game (`<Game>` prefix, docs lookup rows for the game's
   systems) and write `docs/<slug>.md` as the game's system doc (`/write-doc` format).
4. Commit, then `state.py done report`, then `SendUserFile` the report (status `proactive`).
