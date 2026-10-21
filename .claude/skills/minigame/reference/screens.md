# Screens the framework expects a game to draw

The base ships no visuals. Each row is one screen the pipeline builds in the `ui` stage, the demo
folder `asset_scan.py` looks in (`<Root>/Textures/UI/<Folder>/*demo*.png|jpg`), and where its
behaviour contract lives. `Required` screens block Phase 0 when their demo is missing; `Plan`
screens become required when the build plan turns their feature on.

| Folder | Screen | Where | PopupId | Need | Contract |
| --- | --- | --- | --- | --- | --- |
| `Home` | Home: play button, level label, coin and life counters, settings button | HomeScene canvas | - | Required | popup-behaviours "Home" |
| `Hud` | Gameplay HUD: level label, settings gear, booster bar | MainGame, built by `GameplayHudPresenter` from the scope's HUD prefab | - | Required | popup-behaviours "Home" (HUD bullets) |
| `Win` | Win popup | MainGame | `Win` | Required | popup-behaviours "Win" |
| `Lose` | Lose popup with revive | MainGame | `Lose` | Required | popup-behaviours "Lose" |
| `OutOfLives` | Out of lives / refill | Home and MainGame | `OutOfLives` | Required | popup-behaviours "Out of lives" |
| `Settings` | Settings from Home | HomeScene | `Settings` | Required | popup-behaviours "Settings" |
| `SettingsIngame` | Settings over gameplay, with Home/Replay | MainGame | `SettingsIngame` | Required | popup-behaviours "Settings" |
| `Out` | Leave-level confirmation (costs a life) | MainGame | `Out` | Required | `docs/unlimited-lives.md` |
| `Pause` | Pause | MainGame | `Pause` | Plan | - |
| `BuyBooster` | Buy a booster for coins | MainGame | `BuyBooster` | Plan (boosters) | popup-behaviours "Buy booster" |
| `BoosterUnlock` | Booster unlocked reward | MainGame | `BoosterUnlock` | Plan (boosters + tutorial) | popup-behaviours "Buy booster" |
| `NewFeature` | New mechanic intro | MainGame | `NewFeature` | Plan (features) | - |
| `DailyReward` | Daily reward page | HomeScene | `DailyReward` | Plan | `docs/daily-reward.md` |
| `Profile` | Name and avatar | HomeScene | `Profile` | Plan | popup-behaviours "Profile" |
| `Loading` | Scene transition | `SceneTransitionSettings.loadingPrefab` | - | Plan | `LoadingTransitionView` |
| `Tutorial` | Hand, spotlight, lesson text | MainGame | - | Plan (tutorial) | `Game/Views/Tutorial/` |

Never offered on TikTok: `Shop` (no IAP) and `TrackingPermission` (TikTok's own consent). Their
`PopupId` values stay for the numbering only.
