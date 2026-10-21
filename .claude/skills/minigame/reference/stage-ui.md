# Stage `ui` - every screen from its demo

Plan section: Screens (the in-scope rows of `screens.md`, each with its demo).

## Order

Home → HUD → Win → Lose → OutOfLives → Settings → SettingsIngame → Out → the `Plan` screens the plan
turned on. The first popup decides the game's popup frame (`Panel` image); every later popup
reuses it.

## Per screen

1. `/ui-from-demo <screen>` steps 1-5; write the spec to `docs/<slug>/ui/<screen>.md` (asset list,
   layout, text and button tables). The run approves it (`autonomy.md`).
2. A sprite the demo needs and the project lacks: build without it only if it is decoration, log a
   decision and list it in the report; if it is a button body or the frame, mark the screen `fail`.
3. Popups: `/new-popup` with `Prefabs/UIPopup/PopupTest.prefab` as the root source; the controller
   honours `docs/stack/popup-behaviours.md` for that screen (rules come from the use cases and gates,
   never copied); register it in `PopupCatalog.asset` under its existing `PopupId`.
4. Home: the play button calls `HomeBootstrap.RequestPlay()`; counters only listen to service events.
   HUD: a prefab assigned to the MainGame scope's `gameplayHudPrefab`.
5. `/ui-from-demo` step 8: capture and compare row by row; three rounds at most, then log the rows
   still ✗.

## Done when

Every in-scope screen built and compared; the side-by-side images saved under
`Library/Toolkit/minigame/ui/` for the report; gate green.
