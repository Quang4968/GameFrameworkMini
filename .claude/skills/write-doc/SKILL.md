---
name: write-doc
description: Write or refresh docs/<system>.md in the project's summary-first format and add its lookup row to CLAUDE.md. User-invoked after a new system lands or when a doc drifted.
argument-hint: <system-slug>
disable-model-invocation: true
---

# /write-doc $ARGUMENTS

Format contract: `docs/stack/doc-template.md`. Game docs are Vietnamese by convention (existing
set); new docs may be English if the user prefers — ask once if the choice is not obvious.

## 1. Tone references

Read `docs/popups.md` and `docs/audio-haptics.md` with `limit: 25`: one long summary, one short.

## 2. Harvest invariants

Grep the system's files for header comments; every "must / never / only / append / silently"
sentence is a candidate bullet. Add: the `builder.Register*` choice and which scopes call it, any
append-only enum, any PlayerPrefs key, any cross-scene component rule, the test file that covers
the rules.

## 3. Write from the template

`${CLAUDE_SKILL_DIR}/templates/system-doc.md.tmpl`:

- title, at most two lines of scope;
- `## Tóm tắt` (or `## Summary`): 6–12 bullets, each a cross-file invariant with the silent
  consequence of breaking it, bold on the trap, no bullet restating a class name;
- `**Bắt đầu từ**:` (or `**Start from**:`) with 1–3 entry files relative to the scripts root;
- optional sections: flow (ASCII diagram in a fenced block), traps, test.

The 20-line contract: `head -20 docs/<slug>.md` must reach into the bullets (at most 3 lines of
preamble).

## 4. Lookup row

Add a row to `## Which doc to read` in `CLAUDE.md`: specific globs above generic rows, first match
wins. Name the new types and folders the way the table already does. `AGENTS.md` regenerates
through the hook; confirm with `git diff --stat AGENTS.md`.

## 5. Report

Print the summary block back to the user for a sanity read, and the lookup row added.
