# SDK (TikTok Mini Game)

The SDK in `Assets/_Dev/Sdk/` is the project's own copy of the MoodBoost SDK, cut down to what a TikTok
Mini Game uses: ads, tracking, haptics and remote config behind facades, with TTSDK as the one
provider. Read before touching `Assets/_Dev/Sdk/`, `SdkSettings`, `SdkSystemInstaller` or an ad or
tracking call site.

## Summary

- **Game code talks to facades only**: `IAdService` / `IAdEventStream` (`MoodBoost.Sdk.Ads`),
  `ITrackingService` / `ILevelTracking` (`.Tracking`), `IHapticsService` (`.Haptics`),
  `IRemoteConfigService`, `IAppLifecycle`, `IPlayerLevel` (`.Core`). `TTSDK` / `TT.*` code lives only in
  `Runtime/Providers/TikTokMiniGame/`; game code cannot reach it (asmdef boundary plus the
  `vendor-sdk-namespace` source rule). The SDK's menus stay under `MoodBoost/`.
- **`SdkProviderCatalog` is the only file that names a provider.** WebGL device: `TikTokAdProvider`,
  `TikTokTrackingProvider`, `TikTokHapticsProvider` (under `MB_TIKTOK`). Editor: the simulated ad
  network and `LogTrackingProvider`. Everything else: null implementations - the game runs without
  ads, never crashes.
- **Built once, registered everywhere.** `MoodBoostSdk.GetOrCreate` builds the cluster on first use
  (static + `DontDestroyOnLoad` host); every scope calls `RegisterSdkSystem(sdkSettings)`, which only
  re-registers the same instances. Boot initializes it (`InitializeSdkBootStep`).
- **`Show*Async` never throws and never hangs**; only `AdResult.Completed` may grant a reward.
  `IsReady` is for enabling a button, not for deciding whether to call `Show`.
- **Ad frequency lives in remote config** (`AdsRemoteConfig`: interstitial cooldowns, first level,
  retries). There is no remote backend on TikTok yet: the values are the code defaults overridden by
  `SdkSettings.RemoteConfigDefaults` (edited in **MoodBoost > Remote Config**).
- **Ads log themselves**: `AdTrackingBridge` sends `ad_request`, `ad_impression`, `mediation_ad_click`,
  `ad_complete`; the game logs only `ad_offer_shown`.
- **TikTok drops undeclared events**: every event name the game logs must be declared on the TikTok
  console; a rejected name is logged once.
- **TTSDK redirects `PlayerPrefs`** through a global-namespace class on a WebGL device; in the Editor it
  writes through to Unity's, so play-testing tools still see the keys.
- **One define**: `MB_TIKTOK` on WebGL (**MoodBoost > SDK Modules**), plus `MB_ADS_FAKE` for a device
  test build and `MB_SDK_LOG` for verbose logs. Other targets carry no SDK symbol.

**Start from**: `Runtime/Unity/SdkProviderCatalog.cs`, `Runtime/Unity/MoodBoostSdk.cs`,
`Scripts/Game/Installers/SdkSystemInstaller.cs`.

## Setting a game up

1. TTSDK in `Assets/Plugins/com.tiktok.minigame` (already in the template; reinstall or upgrade with
   **MoodBoost > TikTok Mini Game > Install TTSDK**, which imports the package kept in
   `Assets/_Dev/Sdk/Vendor~/TikTokMiniGame/`; TTSDK's build tool needs it under `Assets/Plugins`).
2. Build target WebGL; `MB_TIKTOK` on (SDK Modules).
3. **MoodBoost > SDK Settings** creates `Assets/SDKSetting/SdkSettings.asset` (per game, never copied):
   fill the TikTok block (`rewarded`, `interstitial` ad unit ids, tried in order) and assign the asset
   to the Boot, Home and MainGame scopes.
4. The appid goes into TTSDK's own `StarkBuilderSettings` (**TikTokGame/Build Minigame**); `/build-tiktok`
   checks every item above.

## The TikTok adapter

| | |
| --- | --- |
| Init | `TT.InitSDK`, 15 s cap; a non-zero code drops the ads layer |
| Formats | rewarded, interstitial (banner, MREC, app open turn themselves off) |
| Rewarded result | `OnClose(isEnded)`: `true` = `Completed`, `false` = `Skipped` |
| Errors | one `OnError`: during a show `Failed`, otherwise a load failure |
| Revenue | no ILRD: `ad_impression` carries revenue 0; revenue lives on the TikTok console |
| Pause | `AudioListener.pause` while the ad covers the page |
| Tracking | `TT.ReportEvent(name, params)`; the 16 `TrackingParams` slots become a JSON object; no user id, user properties or crash reports |
| Haptics | `TT.Vibrate*` behind `IHapticsService` |

Not covered yet: lifecycle (`TT.GetAppLifeCycle`; `OnApplicationPause` is unreliable on WebGL) and
payments (`TT.Pay` needs a server-created order).

## Using ads and tracking

```csharp
var result = await ads.ShowRewardedAsync(placement, ct);
if (result == AdResult.Completed) GrantReward();
```

A non-async call site uses the game's adapters (`SdkRewardedAdService`, `SdkInterstitialAdService`)
with `CancellationToken.None`: once an ad covers the screen it cannot be pulled back. To react to every
show (mute music), listen to `IAdEventStream`.

Tracking events are typed extension methods on `ITrackingService` (resource, ad, tutorial, startup,
user-property files in `Runtime/Tracking/`); level events go through `ILevelTracking`, which reads the
level from `IPlayerLevel`. Durations are milliseconds without ad time; an absent value logs as
`"null"`; renaming an event after data was sent loses its history - treat names in `TrackingEvents` as
final.

## Rules

- A vendor namespace outside `Runtime/Providers/<Vendor>/` is wrong; a new platform is one provider
  assembly with its own define, one branch in `SdkProviderCatalog`, and nothing else.
- `MoodBoost.Sdk.Ads` and `.Tracking` never reference each other; `AdTrackingBridge` joins them.
- `ISdkModule.InitializeAsync` never lets an exception escape: a network failure must not block boot.
- `static` only in `MoodBoostSdk`, `SdkHost` and `StartupFunnel`.
- The SDK knows nothing of the game (`SdkNeverReferencesGame` test) and never references VContainer
  outside `MoodBoost.Sdk.Unity` / `.Editor`.
