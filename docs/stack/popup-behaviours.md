# Popup and screen behaviours

What the framework's screens must do once a game rebuilds its UI from the demo. The base ships the
logic (Core rules, use cases, gates, presenters, `PopupService`) and no visuals; this file is the
contract a new popup or screen view honours. Read it before building any of the screens below.

## Summary

- **No visuals in the base.** Popup prefabs, Home/HUD/Shop/Daily Reward views and `Textures/UI/` are
  gone. Every `PopupId` value stays (index-stored, append only); its `PopupCatalog` row has no prefab
  until the game adds one, and `PopupService.OpenAsync` returns `null` for it without throwing.
- **Rules stay in Core**: `OnboardingRules`, `CanStartLevelUseCase`, `AwardWinCoinsUseCase`,
  `RefillLivesWithCoinsUseCase`, `ClaimAdRewardLivesUseCase`, `CanBuyBoosterUseCase`,
  `BuyBoosterWithCoinsUseCase`, `ClaimTutorialRewardUseCase`, `PlayerNameRules`. A view asks them and
  never copies a condition.
- **Gates own the pending action**: `OutOfLivesGate`, `BuyBoosterGate`, `TutorialRewardGate`. A popup reports success (`NotifyRefilled`, etc.) as its **last** call and
  calls `gate.Cancel()` on `PopupView.CloseRequested`, never in `OnDisable` (PopupHost hides a fresh
  instance at once, which would cancel the first open of the scene).
- **Wiring in `[Inject] Construct`, redraw on every open**: PopupHost reuses one instance, so state is
  recomputed in `OnEnable` / `PopupView.Opened`; `ad_offer_shown` is logged once per open, only while
  a loaded ad makes the button tappable.
- **Ad callbacks outlive the scene**: every SDK callback starts with `if (this == null) return;`;
  rewards land only in the reward callback, and scene loads run whatever the interstitial did.
- **Home's Play** is `HomeBootstrap.RequestPlay()`; out of lives it raises `LivesExhausted`, which
  `OutOfLivesGatePresenter` turns into the gate.
- **No IAP, no consent popup on TikTok**: `PopupId.Shop` and `PopupId.TrackingPermission` stay in
  the enum for the numbering only. What a short-of-coins tap does is the game's monetization plan.

**Start from**: `Game/Views/Popup/PopupService.cs`, `Game/Views/Popup/OutOfLivesGate.cs`,
`Game/Boot/HomeBootstrap.cs`.

## Win

- Buttons: next (plain claim), x2 (rewarded, placement `SdkSystemInstaller.WinDoubleRewardPlacement`,
  hidden when `WinRewardSettings.RewardedEnabled` is off, dimmed until `IsReady`), replay, home
  (removed when `OnboardingRules.AllowsHome(LevelShow)` is false).
- The plain claim shows an interstitial first; x2 never does (one full-screen ad per claim).
- Claim order: lives check (`ChargesLife` + `CanStartLevelUseCase`, else `outOfLivesGate.Open(retry)`)
  → coin flight → `AwardWinCoinsUseCase` → `LogResourceEarn(..., TrackingPlacements.LevelWin,
  EarnReason.Reward, "multiplied" when x2)` → `SetNextPlayType(Next)` → `AdvanceToNextLevel()` →
  close → load. The last onboarding level returns Home (`OnboardingRules.ReturnsHomeAfterWin`).
- The level label shows `LevelShow + 1`: the level is advanced only on next, so replay replays it.
- Upcoming feature bar: driven by `GetUpcomingFeatureUseCase` from `LevelShow`, no stored state.

## Lose

- Revive with coins (price from `LevelReviveController`, progressive) or a rewarded ad (placement
  `SdkSystemInstaller.RevivePlacement`, only when `OnboardingRules.OffersRewardedRevive`).
- Not enough coins: what the tap offers (rewarded coins, dimmed button) is the game's monetization plan.
- A failed revive (`TryReviveWithCoins/TryReviveFree` false) refreshes the buttons and stays open.
- Home and the X both leave the level (X = giving up the revive): interstitial first unless
  `GameRemoteConfig.SkipInterstitialAfterLoss`; in onboarding the exit restarts the level.
- Replay passes the lives check like Win.
- Optional peek: holding outside the frame hides the popup and the overlay to show the board.

## Out of lives

- Opens from the gate (blocked action) or the HUD "+" (lives not empty): title differs.
- Hearts up to `MaxLives`, filled/empty as separate sprites (UI tint multiplies, it cannot grey a red
  heart). Countdown from `ILifeService.TimeUntilNextLife`, refreshed by `LifeTicker`, never from the
  device clock; "Full lives!" when full.
- Buy refill: cost `GameRemoteConfig.LifeRefillCoinCost` (fallback `LifeSettingsSO`), enabled by
  `CanRefillLivesUseCase`; logs a coin spend and a life earn (`TrackingPlacements.OutOfLives`,
  `Exchange`). Free lives: rewarded `SdkSystemInstaller.ExtraLifePlacement`, gated by
  `ExtraLifeRewardedEnabled`, logs `EarnReason.WatchAds`. Lives refilled while open do not auto-close.

## Buy booster, booster unlock

- One popup for every `BoosterType`; which one comes from `BuyBoosterGate` / `TutorialRewardGate`.
- Buy: `CanBuyBoosterUseCase` decides; `BuyBoosterWithCoinsUseCase`, then the gate.
- Unlock: `ClaimTutorialRewardUseCase` is idempotent and the popup closes either way.
- A booster with no icon hides its slot.

## Daily reward, settings, profile, Home

- Daily reward: one page of 7 days holding today's slot; `DailyRewardAutoOpen` opens it once per
  launch on Home. A chest day reveals its items after the claim (already paid).
- Settings: music / sound / vibration switches map to 0/1 on `ISettingsService`; a sound or haptic
  plays when switched on; contact button hidden without a link; language flags switch
  `ILocalizationService`.
- Profile: name (limit `PlayerNameRules.MaxLength`) and avatar (`AvatarCatalogSO`) are pending until
  Save; X discards.
- Home HUD: coin and life counters only read service events; the play button and the gameplay HUD
  may skin by level difficulty from `LevelCatalogSO`; the top strip is hidden on the first level.
