using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk.Ads.Providers;
using TTSDK;

namespace MoodBoost.Sdk.Ads.Mediation
{
    // TikTok's interstitial: the rewarded unit without a reward, so closing is always Completed. Same OnError rule -
    // a pending show turns it into Failed, otherwise it is a load failure.
    sealed class TikTokInterstitialAdUnit : IFullScreenAdUnit
    {
        const string Tag = "TikTok/Interstitial";

        readonly TTInterstitialAd ad;

        UniTaskCompletionSource<AdResult> showSource;
        string currentPlacement;

        public AdFormat Format => AdFormat.Interstitial;

        public string AdUnitId { get; }

        public bool IsReady => ad.IsLoaded();

        public event Action Loaded;
        public event Action<string> LoadFailed;
        public event Action<AdImpression> ImpressionRecorded;
        public event Action Clicked;

        public TikTokInterstitialAdUnit(string adUnitId)
        {
            AdUnitId = adUnitId;
            ad = TT.CreateInterstitialAd(new CreateInterstitialAdParam { InterstitialAdId = adUnitId });

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

        void OnClose()
        {
            if (showSource == null) return;

            ImpressionRecorded?.Invoke(TikTokImpression.Create(AdFormat.Interstitial, currentPlacement, AdUnitId));
            Finish(AdResult.Completed);
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
