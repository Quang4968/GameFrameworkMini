# Stage `monet` - rewarded and interstitial

Plan section: Monetization. TTSDK serves rewarded and interstitial only; there is no IAP and no shop.

## Placements the framework already has

| Placement | Constant | Where |
| --- | --- | --- |
| Revive | `SdkSystemInstaller.RevivePlacement` | Lose popup |
| x2 win coins | `SdkSystemInstaller.WinDoubleRewardPlacement` | Win popup |
| Extra lives | `SdkSystemInstaller.ExtraLifePlacement` | Out of lives popup |
| Level complete interstitial | `SdkSystemInstaller.LevelCompletePlacement` | Win claim, Lose exit |

## Build

- Wire each placement the plan keeps through `IRewardedAdService` / `I_InterstitialAdService`; a
  new placement gets a constant next to these and its own remote-config enable flag in
  `RemoteConfigKeys` (default from the plan).
- Short of coins: apply the plan's rule. Rewarded coins → a placement (`coins_ad`), a `CoinSettingsSO`
  amount, and `LogResourceEarn(..., EarnReason.WatchAds)`; dimmed → the button follows
  `CanBuyBoosterUseCase` / revive affordability.
- Interstitial frequency is the SDK's `AdPolicySettings` (cooldowns, first level), set from the
  plan through `SdkSettings.RemoteConfigDefaults`; never count in game code.
- Reward lands only in the reward callback; every callback starts with `if (this == null) return;`.
- `ad_offer_shown` is logged once per popup open, only with a loaded ad.

## Done when

In the Editor (simulated ads), each placement shows, pays once and logs; interstitial respects the
first-level rule; gate green.
