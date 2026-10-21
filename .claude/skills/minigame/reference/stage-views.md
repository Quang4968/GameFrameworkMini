# Stage `views` - board, input and the gameplay seam

Plan sections: Gameplay spec (feel, input), Level format.

## Build

- Subclass `MainGameLifetimeScope` as `<Game>LifetimeScope` (`Game/Installers/<Game>/`), override
  `RegisterGameplay(builder)`: the level repository, the session, the board views and a
  `<Game>Bootstrap` entry point. Swap the component on the MainGame scene's scope object (keep every
  serialized field).
- Views follow `/new-view` (injection in `Construct`, LitMotion with stored handles, pooled effects).
  They render state and forward input to the use cases; no rule in a `MonoBehaviour`.
- The seam is `GameplayBridge`: on a win call `ReportWon()`, on a loss `ReportLost()`; set
  `CanReviveProvider` / `ReviveHandler` (what a revive does to the board) and `PauseHandler`
  (input hold while a popup is up); `ItemsRemainingProvider` feeds tracking.
- Board art comes from the gameplay sprites found in Phase 0; sizes for a portrait 1080x1920 view.
- Anything short-lived spawns through `IObjectPoolService` and is declared in `PoolCatalog.asset`.
- A popup sets `Time.timeScale = 0`: board motion that must keep going runs on unscaled time.

## Done when

Play mode from MainGame loads level 1, input plays it, and winning or losing raises the bridge event
(log it once with `Debug.Log` behind `#if UNITY_EDITOR`, read it with `console`, then remove it).
Capture `Temp/views.png` at the card's portrait size for the report.
