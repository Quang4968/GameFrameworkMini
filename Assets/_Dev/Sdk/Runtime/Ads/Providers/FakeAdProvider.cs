using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk.Ads.Providers
{
    // A simulated ad network for the Editor: load latency, fake revenue, and a placeholder panel (FakeAdScreen) - enough
    // to click through the UI flow, and to see which format and ad unit a button really asked for, without building to a
    // real device.
    //
    // By default it ALWAYS has inventory (fillRate = 1): the fake network is only enabled while a developer is testing
    // a specific flow, and a random no-fill there merely dims a button inexplicably without checking anything extra. To
    // exercise the no-fill branch, construct FakeAdProvider directly with a small fillRate.
    //
    // Selected by the MB_ADS_FAKE define, then gated by AdService so device builds only initialize it on remote-config
    // test devices. It lives in the Ads assembly because it depends on no third-party SDK.
    public sealed class FakeAdProvider : IAdProvider
    {
        readonly double fillRate;
        readonly double loadSeconds;

        public MediationId Id => MediationId.Fake;

        public string Name => "Fake";

        public FakeAdProvider(double fillRate = 1d, double loadSeconds = 0.5d)
        {
            this.fillRate = fillRate;
            this.loadSeconds = loadSeconds;
        }

        public UniTask<bool> InitializeAsync(AdProviderContext context, CancellationToken ct)
        {
            SdkLog.Warn(nameof(FakeAdProvider), "Running the SIMULATED ad network - no real revenue.");
            return UniTask.FromResult(true);
        }

        public IFullScreenAdUnit CreateFullScreen(AdFormat format, string adUnitId)
            => new FakeFullScreenAdUnit(format, adUnitId, fillRate, loadSeconds);

        public IBannerAdUnit CreateBanner(string adUnitId, AdAnchor anchor, bool mrec)
            => new FakeBannerAdUnit(adUnitId, mrec ? AdFormat.Mrec : AdFormat.Banner, loadSeconds);

        public void SetPauseGame(bool pause) { }

        public void Dispose() { }

        sealed class FakeFullScreenAdUnit : IFullScreenAdUnit
        {
            readonly double fillRate;
            readonly double loadSeconds;
            readonly System.Random random = new System.Random();

            bool loading;

            public AdFormat Format { get; }
            public string AdUnitId { get; }
            public bool IsReady { get; private set; }

            public event Action Loaded;
            public event Action<string> LoadFailed;
            public event Action<AdImpression> ImpressionRecorded;
            public event Action Clicked;

            public FakeFullScreenAdUnit(AdFormat format, string adUnitId, double fillRate, double loadSeconds)
            {
                Format = format;
                AdUnitId = adUnitId;
                this.fillRate = fillRate;
                this.loadSeconds = loadSeconds;
            }

            public void Load()
            {
                if (loading || IsReady) return;
                loading = true;
                LoadAsync().Forget();
            }

            async UniTaskVoid LoadAsync()
            {
                await UniTask.Delay(TimeSpan.FromSeconds(loadSeconds), DelayType.UnscaledDeltaTime);
                loading = false;

                if (random.NextDouble() > fillRate)
                {
                    LoadFailed?.Invoke("fake no-fill");
                    return;
                }

                IsReady = true;
                Loaded?.Invoke();
            }

            public UniTask<AdResult> ShowAsync(string placement, CancellationToken ct)
            {
                IsReady = false;

                // Recorded on DISPLAY, like a real network: a rewarded ad the player closes early was still shown.
                ImpressionRecorded?.Invoke(new AdImpression(
                    Format, placement, AdUnitId, "fake", 0.012d, "USD", "estimated", "VN"));

                // The panel decides the outcome, so the skip branch is reachable in the Editor. Rewarded keeps the
                // longer watch time, which is what exposes a place that forgets to cancel its token on a mid-ad
                // scene change.
                var seconds = Format == AdFormat.Rewarded ? 3d : 1d;
                return FakeAdScreen.ShowAsync(Format, AdUnitId, placement, seconds, ct);
            }

            public void Dispose()
            {
                Loaded = null;
                LoadFailed = null;
                ImpressionRecorded = null;
                Clicked = null;
            }
        }

        sealed class FakeBannerAdUnit : IBannerAdUnit
        {
            readonly AdFormat format;
            readonly double loadSeconds;

            public string AdUnitId { get; }
            public bool IsLoaded { get; private set; }

            public event Action Loaded;
            public event Action<AdImpression> ImpressionRecorded;
            public event Action<string> LoadFailed;

            public FakeBannerAdUnit(string adUnitId, AdFormat format, double loadSeconds)
            {
                AdUnitId = adUnitId;
                this.format = format;
                this.loadSeconds = loadSeconds;
            }

            public void Load() => LoadAsync().Forget();

            async UniTaskVoid LoadAsync()
            {
                await UniTask.Delay(TimeSpan.FromSeconds(loadSeconds), DelayType.UnscaledDeltaTime);
                IsLoaded = true;
                Loaded?.Invoke();
            }

            public void Show()
            {
                if (!IsLoaded) return;
                ImpressionRecorded?.Invoke(new AdImpression(
                    format, string.Empty, AdUnitId, "fake", 0.0004d, "USD", "estimated", "VN"));
            }

            public void Hide() { }

            public void Dispose()
            {
                Loaded = null;
                ImpressionRecorded = null;
                LoadFailed = null;
            }
        }
    }
}
