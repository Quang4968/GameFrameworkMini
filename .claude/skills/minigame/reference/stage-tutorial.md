# Stage `tutorial` - first-session lessons

Plan section: Tutorial (lessons, the level each unlocks at, steps, reward).

## Build

- One `E_TutorialId` per lesson (appended after `HowToPlay`, which stays the level-1 lesson).
- Lessons are data in `TutorialSettings.asset`: steps built from the engine's step types
  (`Game/Views/Tutorial/`: hand hint, spotlight, tap-UI step), the unlock level, and the booster
  reward when a lesson grants one (`TutorialRewardGate` + the BoosterUnlock popup).
- Board steps point the hand at a world target through `TutorialScreenRect`; the board view exposes
  the target, the tutorial never reaches into the board.
- Feature intros (new mechanic popup) use `FeatureIntroSettings.asset` and `GameFeature` values.
- Unlock levels may be overridden by remote config (`onboard_unlock_level_<lesson>`); the asset holds
  the default.

## Done when

A fresh PlayerPrefs state plays level 1 through the HowToPlay lesson in Play mode, each later lesson
starts at its level (jump with the debug console), and the gate is green.
