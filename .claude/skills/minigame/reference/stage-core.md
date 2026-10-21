# Stage `core` - gameplay rules

Plan sections: Gameplay spec, Level format.

## Build

- Domain types in `Scripts/Core/Domain/<Game>/` (board, pieces, level data as plain C#), rules as pure
  functions in `Scripts/Core/GameLogic/<Game>/`, whole player actions in `Scripts/Core/UseCases/<Game>/`
  (`TapXUseCase.Execute(state, input) -> outcome`). Core references nothing but the BCL and Unity math.
- The level format is plain data (JSON or a serializable class) with a loader in `Game/Services/`;
  Core receives the parsed data, never a file path.
- Win and lose are a rule (`<Game>OutcomeRules`) the views call - never a view's own check.
- Append the game's values to `BoosterType`, `GameFeature`, `E_TutorialId`, `AudioCue`, `PopupId`
  at the end only.
- Implement `ILevelCatalog` (level count, difficulty per index) on the game's level repository.

## Tests

`Scripts/Tests/EditMode/Core/<Game>/`: one test file per rule and per use case. Cover: a solvable
starting state, every win path, every lose path, each booster, and a fixture level that round-trips
through the loader. Fakes implement existing interfaces only - a test never adds a member to Core.

## Done when

The gate is green and a scripted sequence of use-case calls in a test plays a fixture level to a win.
