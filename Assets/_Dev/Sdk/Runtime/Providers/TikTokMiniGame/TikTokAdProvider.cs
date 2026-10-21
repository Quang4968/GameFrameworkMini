using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk.Ads.Providers;
using TTSDK;
using UnityEngine;

namespace MoodBoost.Sdk.Ads.Mediation
{
    // TikTok Mini Game ads through TTSDK. The whole assembly disappears when MB_TIKTOK is off, and it only exists
    // on WebGL and in the Editor (ttsdk.dll is WebGL-only, ttsdk-editor.dll mocks it in the Editor).
    //
    // TTSDK has rewarded and interstitial only: every other format returns null so AdService turns it off. There is no
    // app key - the ad unit ids alone identify the game.
    public sealed class TikTokAdProvider : IAdProvider
    {
        const string Tag = "TikTok";

        // Same cap as LevelPlay: a container that never answers must not hang boot.
        static readonly TimeSpan InitTimeout = TimeSpan.FromSeconds(15d);

        bool initialized;
        bool audioPausedBeforeAd;

        public MediationId Id => MediationId.TikTok;

        public string Name => "TikTok";

        public async UniTask<bool> InitializeAsync(AdProviderContext context, CancellationToken ct)
        {
            if (initialized) return true;

            var initSource = new UniTaskCompletionSource<bool>();

            TT.InitSDK((code, env) =>
            {
                if (code != 0) SdkLog.Error(Tag, $"InitSDK failed with code {code}.");
                initSource.TrySetResult(code == 0);
            });

            var (timedOut, ok) = await initSource.Task
                .TimeoutWithoutException(InitTimeout, DelayType.UnscaledDeltaTime);

            ct.ThrowIfCancellationRequested();

            if (timedOut)
            {
                SdkLog.Warn(Tag, $"InitSDK did not respond within {InitTimeout.TotalSeconds}s - dropping the ads layer.");
                return false;
            }

            initialized = ok;
            return ok;
        }

        public IFullScreenAdUnit CreateFullScreen(AdFormat format, string adUnitId)
        {
            if (!initialized) return null;

            switch (format)
            {
                case AdFormat.Rewarded:
                    return new TikTokRewardedAdUnit(adUnitId);

                case AdFormat.Interstitial:
                    return new TikTokInterstitialAdUnit(adUnitId);

                default:
                    SdkLog.Warn(Tag, $"TTSDK has no {format} format - this format is off.");
                    return null;
            }
        }

        public IBannerAdUnit CreateBanner(string adUnitId, AdAnchor anchor, bool mrec) => null;

        // The ad plays its own sound over the page; the game's audio would otherwise keep running underneath. The
        // previous value comes back on close so an ad never unmutes audio something else had paused.
        public void SetPauseGame(bool pause)
        {
            if (pause)
            {
                audioPausedBeforeAd = AudioListener.pause;
                AudioListener.pause = true;
                return;
            }

            AudioListener.pause = audioPausedBeforeAd;
        }

        public void Dispose() => initialized = false;
    }
}
