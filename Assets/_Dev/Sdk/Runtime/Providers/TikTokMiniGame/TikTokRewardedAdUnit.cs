using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk.Ads.Providers;
using TTSDK;

namespace MoodBoost.Sdk.Ads.Mediation
{
    // TikTok's rewarded video. Unlike LevelPlay the reward arrives in the close callback itself (isEnded), so there is
    // no reward/close ordering to reconcile.
    //
    // TTSDK reports load and show failures on the same OnError: a failure while a show is pending is that show's
    // Failed, any other is a load failure.
    sealed class TikTokRewardedAdUnit : IFullScreenAdUnit
    {
        const string Tag = "TikTok/Rewarded";

        readonly TTRewardedVideoAd ad;

        UniTaskCompletionSource<AdResult> showSource;
        string currentPlacement;

        public AdFormat Format => AdFormat.Rewarded;

        public string AdUnitId { get; }

        public bool IsReady => ad.IsLoaded();

        public event Action Loaded;
        public event Action<string> LoadFailed;
        public event Action<AdImpression> ImpressionRecorded;
        public event Action Clicked;

        public TikTokRewardedAdUnit(string adUnitId)
        {
            AdUnitId = adUnitId;
            ad = TT.CreateRewardedVideoAd(new CreateRewardedVideoAdParam { AdUnitId = adUnitId });

            ad.OnLoad += OnLoad;
            ad.OnError += OnError;
            ad.OnClose += OnClose;
        }

        public void Load() => ad.Load();

        public UniTask<AdResult> ShowAsync(string placement, CancellationToken ct)
        {
            // A second show while one is pending would orphan the first awaiter.
            if (showSource != null) return UniTask.FromResult(AdResult.Failed);

            currentPlacement = placement;
            showSource = new UniTaskCompletionSource<AdResult>();

            // ct is not tied in: once the ad covers the page it cannot be pulled back (see LevelPlayRewardedAdUnit).
            var task = showSource.Task;

            try
            {
                ad.Show();
            }
            catch (Exception exception)
            {
                SdkLog.Warn(Tag, $"Show threw: {exception.Message}");
                Finish(AdResult.Failed);
            }

            return task;
        }

        void OnLoad() => Loaded?.Invoke();

        void OnError(int code, string message)
        {
            var error = $"{code}: {message}";

            if (showSource != null)
            {
                SdkLog.Warn(Tag, $"failed to show: {error}");
                Finish(AdResult.Failed);
                return;
            }

            LoadFailed?.Invoke(error);
        }

        void OnClose(bool isEnded)
        {
            if (showSource == null) return;

            ImpressionRecorded?.Invoke(TikTokImpression.Create(AdFormat.Rewarded, currentPlacement, AdUnitId));
            Finish(isEnded ? AdResult.Completed : AdResult.Skipped);
        }

        void Finish(AdResult result)
        {
            var source = showSource;
            showSource = null;
            source?.TrySetResult(result);
        }

        public void Dispose()
        {
            Finish(AdResult.Failed);

            ad.OnLoad -= OnLoad;
            ad.OnError -= OnError;
            ad.OnClose -= OnClose;
            ad.Destroy();

            Loaded = null;
            LoadFailed = null;
            ImpressionRecorded = null;
            Clicked = null;
        }
    }
}
