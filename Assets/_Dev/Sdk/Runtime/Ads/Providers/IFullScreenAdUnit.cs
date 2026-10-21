using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk.Ads.Providers
{
    // One full-screen ad unit (rewarded / interstitial / app open) belonging to a specific network.
    //
    // All three formats SHARE this interface because, to the caller, they differ in exactly one respect: rewarded has a
    // reward. And the reward is already encoded in AdResult.Completed. Sharing means an adapter writes only one class,
    // and AdService has only one load/retry loop.
    public interface IFullScreenAdUnit : IDisposable
    {
        AdFormat Format { get; }

        string AdUnitId { get; }

        bool IsReady { get; }

        // Fires one load request. Not awaitable: AdService drives the retry cadence itself via Loaded/LoadFailed.
        void Load();

        // Only callable when IsReady. Never throws - on failure it returns Failed.
        UniTask<AdResult> ShowAsync(string placement, CancellationToken ct);

        event Action Loaded;

        // The network's error message, already formatted into a readable string.
        event Action<string> LoadFailed;

        event Action<AdImpression> ImpressionRecorded;

        event Action Clicked;
    }
}
