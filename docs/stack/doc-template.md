# System doc template

Every system under `docs/` opens with a summary block the agent reads in isolation (`Read` with
`limit: 20`), so the first 20 lines must carry the invariants. Existing Vietnamese docs use
`## Tóm tắt` / `**Bắt đầu từ**:`; new docs may use `## Summary` / `**Start from**:`. Position is what
matters, not the heading language.

## Summary

- Title, at most two lines of scope, then the summary block: 6–12 bullets, each a **cross-file
  invariant** or **the only place** something happens, with the silent consequence of breaking it.
- Bold the load-bearing word; name concrete types in backticks; link sibling docs relatively.
- Close the block with `**Start from**:` naming 1–3 entry files relative to the scripts root.
- Body sections explain flows (ASCII diagrams in fenced blocks), decision tables and traps. No
  changelog, no restating class names, no step-by-step narration of code.
- Update the doc in the same change that alters the behaviour it describes, and add a lookup row
  to `CLAUDE.md` (specific globs above generic ones; first match wins).

**Start from**: `docs/popups.md` (long summary), `docs/audio-haptics.md` (short summary).

## Skeleton

```markdown
# <System>

<One or two lines: what this system owns and when to read this file.>

## Summary

- **<Invariant>**: <what holds across files, and what silently breaks when it does not>.
- <The only place X happens is `Type.Method`; doing it elsewhere causes Y>.
- <Enum stored by index> takes new values **appended at the end** only.
- <Registration>: `builder.Register<Feature>(...)` in every scope that hosts <View>; missing
  registration breaks the whole container of that scene.
- See [<sibling>.md](<sibling>.md) for <related concern>.

**Start from**: `Core/Services/I<Feature>Service.cs`, `Game/Services/PlayerPrefs<Feature>Service.cs`.

## Flow

```
<caller> --I<Feature>Service--> <Service> --event--> <View>
```

## Traps

- <Trap>: <why it happens, what to do instead>.

## Test

- `Tests/EditMode/Core/<Folder>/<Subject>Tests.cs` covers <rules>; fakes are nested in the test class.
```

## Checks before committing a doc

- `head -20 docs/<system>.md` reaches into the bullets (at most 3 lines of preamble).
- No bullet restates a class name; every bullet names a consequence.
- `CLAUDE.md` has a lookup row for the new names, and `AGENTS.md` was regenerated.
