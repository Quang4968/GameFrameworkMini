using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk;
using MoodBoost.Sdk.Unity;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Boot.BootSteps
{
    // Brings the SDK up in the Boot scene, before anything else.
    //
    // A negative Order so it certainly runs first: analytics has to be ready before the game fires its first event,
    // and ads need time to load inventory while the player is still on Home.
    //
    // This step NEVER breaks the boot: SdkRuntime swallows each module's error and returns normally. With no network
    // the game still reaches Home, just without ads.
    //
    // Joins the init started by SdkSceneStartup rather than starting a second one, and waits for exactly the part of
    // it Home depends on - through the consent popup, the ATT dialog, tracking and remote config. Ads, notifications
    // and IAP keep initializing in the background: their buttons light up when they are done, and holding the loading
    // screen for them would only keep the player waiting after they already tapped through the popup.
    //
    // Bounded by InitTimeout, because waiting is the whole point only while the wait ends. LevelPlay caps itself at
    // 15s, Unity IAP at 20s + 20s, and FirebaseApp.CheckAndFixDependenciesAsync has no timeout at all - a device with
    // broken Play Services would otherwise sit on the loading screen forever.
    public sealed class InitializeSdkBootStep : IBootStep
    {
        // Long enough for a slow network to still make it, short enough that a hung SDK does not hold the player
        // hostage on the loading screen.
        static readonly TimeSpan InitTimeout = TimeSpan.FromSeconds(10d);

        readonly ISdkRuntime sdk;
        readonly IPlayerLevel playerLevel;
        readonly ILevelProgressService levelProgress;

        public InitializeSdkBootStep(ISdkRuntime sdk, IPlayerLevel playerLevel, ILevelProgressService levelProgress)
        {
            this.sdk = sdk;
            this.playerLevel = playerLevel;
            this.levelProgress = levelProgress;
        }

        public int Order => -100;

        public string StepName => "Initialize SDK";

        public async UniTask ExecuteAsync(CancellationToken ct)
        {
            // LevelShow counts from 0 and only advances when the Win popup moves on, so +1 is the level
            // being played or just won. The service only reads PlayerPrefs, so it outlives Boot safely.
            playerLevel.SetProvider(() => levelProgress.LevelShow + 1);

            // The consent popup and the ATT dialog wait for a tap, and a player reading them is not a hung SDK: the
            // clock starts once the ATT step - the later of the two - has reported.
            await sdk.WaitForModuleAsync(SdkInitIds.TrackingAuthorization, ct);

            // Remote config is the last module Home reads on arrival (level rules, tutorial flags); tracking and user
            // properties sit before it in the order, so this covers them too. No CancellationTokenSource passed on
            // purpose: the timeout stops BOOT from waiting, it does not stop the init.
            var timedOut = await sdk.WaitForModuleAsync(SdkInitIds.RemoteConfig, ct)
                .TimeoutWithoutException(InitTimeout, DelayType.UnscaledDeltaTime);

            if (timedOut)
            {
                Debug.LogWarning(
                    $"[Boot] The SDK did not finish within {InitTimeout.TotalSeconds}s - going to Home without it.");
            }

            // Closes the funnel's SDK latch here rather than at the end of the whole init: loading_done measures the
            // part Home waited for, and says so when the wait gave up.
            StartupFunnel.MarkSdkReady(timedOut);
        }
    }
}
