# Stage `sdk` - TikTok SDK and tracking

Plan sections: SDK, Tracking. Inputs from Phase 1: appid, rewarded and interstitial ad unit ids.

## Wire

1. `SdkSettings`: create it with **MoodBoost > SDK Settings** (`menu --path`), or reuse the one in
   `Assets/SDKSetting/` (granted path). Fill the TikTok block: `rewarded`, `interstitial` arrays.
   Assign the asset to the `sdkSettings` field of the Boot, Home and MainGame scopes.
2. Defines: WebGL holds `MB_TIKTOK` and nothing mobile; every other target is empty.
3. TTSDK: `Assets/Plugins/com.tiktok.minigame` present (else **MoodBoost > TikTok Mini Game >
   Install TTSDK**); `StarkBuilderSettings` gets the appid (`.claude/skills/build-tiktok/scripts/trigger_build.cs`
   shows the field names).
4. Ad policy defaults (interstitial cooldowns, first level) go into `SdkSettings.RemoteConfigDefaults`
   with the plan's numbers.

## Tracking

Game events go through `ITrackingService` only; the framework already logs the level lifecycle
(`LevelTrackingPresenter`), resource earn/spend at its own call sites, and user properties
(`UserPropertyPublisher`). The stage adds the plan's game-specific events with `TrackingPlacements`
constants and checks that every new coin or life change logs its earn/spend.

## Done when

`/build-tiktok --preflight-only` reports no miss in the SDK rows (appid, `MB_TIKTOK`, ad units,
no mobile define, no mobile assembly named in game code); gate green.
