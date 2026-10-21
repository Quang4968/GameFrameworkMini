---
paths:
  - "**/Scripts/Tests/**/*.cs"
---
# Tests

- NUnit, EditMode, no scene. Test class `<Subject>Tests`; namespace mirrors the Core folder
  (`<Game>.Core.Tests.<Folder>`); method names `Method_Scenario_Expected`.
- Fakes are private nested `sealed class FakeX : IX` inside the test class; no mocking library.
  Shared builders live in a `*Fixtures.cs` beside the tests.
- A test targets a contract that already exists or that the approved plan names. Never add or change
  a member of a Core interface, domain type, enum or asset field so a test or its fake compiles: the
  test would pin an API nobody designed, and the next change bends the code to fit it. Stop and
  re-plan instead.
- An existing assertion changes only when the plan says that behaviour changes. A red test that pins
  something the docs never agreed on is a question for the user, not a reason to bend the code or
  to rewrite the assertion silently.
- `Tests/EditMode/Architecture/` fails on the silent breaks the compiler cannot see (layering, SDK
  boundaries, coroutines, tween libraries, Pool Boss statics, singletons, `async void`, `Find*`, raw
  `Instantiate`, direct `DateTime`, retired names, files over 300 lines).
- `ArchitectureBaseline` lists debt that predates the tests. Never add an entry to make new code
  pass; fix the code. Paying debt off fails `BaselineHasNoPaidOffDebt` on purpose: lower or remove
  that entry in the same change.
- `ArchitectureConfig` is the only project-specific file and must match the project card in
  `CLAUDE.md`. Long form: `docs/stack/architecture-tests.md`.
