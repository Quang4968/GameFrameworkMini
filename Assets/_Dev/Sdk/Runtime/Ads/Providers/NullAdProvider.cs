using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk.Ads.Providers
{
    // The "no ad network wired up" implementation - used when no mediation define symbol is enabled.
    //
    // Init returns true rather than false: the ads layer is still built normally, only no format exists. That way the
    // game behaves exactly like the build with ads - the "Free" button appears dimmed, exactly as a player sees it while
    // an ad is still loading.
    public sealed class NullAdProvider : IAdProvider
    {
        public MediationId Id => MediationId.None;

        public string Name => "None";

        public UniTask<bool> InitializeAsync(AdProviderContext context, CancellationToken ct)
        {
            SdkLog.Warn(nameof(NullAdProvider),
                "No mediation define symbol is enabled - every ad is off, the game runs normally.");
            return UniTask.FromResult(true);
        }

        public IFullScreenAdUnit CreateFullScreen(AdFormat format, string adUnitId) => null;

        public IBannerAdUnit CreateBanner(string adUnitId, AdAnchor anchor, bool mrec) => null;

        public void SetPauseGame(bool pause) { }

        public void Dispose() { }
    }
}
