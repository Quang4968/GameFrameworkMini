using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk.Ads.Providers
{
    // The ONLY EXTENSION POINT for mediation. Adding a network = write a class implementing this, add an asmdef with
    // defineConstraints, add one #if branch in SdkProviderCatalog. AdService is untouched, game code is untouched.
    //
    // The Create* methods return null when the network has no such format - AdService then treats that format as
    // nonexistent and returns Disabled. This is why an adapter never has to pretend to support something it lacks:
    // LevelPlay currently has no app open, so it returns null and app open turns itself off.
    public interface IAdProvider : IDisposable
    {
        // The identifier used to pair the adapter with the right set of ad unit ids in the configuration, and to stamp
        // impressions. The Name string is for logs only - matching on a string means a typo nobody reports.
        MediationId Id { get; }

        string Name { get; }

        // Returns false when init fails. AdService then drops the ads layer entirely rather than creating any ad unit -
        // creating an ad unit on an uninitialized SDK is the classic mediation crash.
        UniTask<bool> InitializeAsync(AdProviderContext context, CancellationToken ct);

        IFullScreenAdUnit CreateFullScreen(AdFormat format, string adUnitId);

        IBannerAdUnit CreateBanner(string adUnitId, AdAnchor anchor, bool mrec);

        // Pauses the game while an ad covers the screen. Networks that handle it themselves leave this empty.
        void SetPauseGame(bool pause);
    }
}
