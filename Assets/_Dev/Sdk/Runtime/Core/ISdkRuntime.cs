using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk
{
    // The single entry point to the whole SDK. The game only knows this interface at boot; after that it uses
    // IAdService / ITrackingService / IRemoteConfigService like any other service.
    public interface ISdkRuntime
    {
        // true once every module has finished InitializeAsync — including modules that failed. This means "done
        // trying", not "everything is fine": a failed module falls back to its own null implementation.
        bool IsReady { get; }

        // Raised once, right after IsReady turns true. A subscriber attaching later is never called, so check IsReady
        // first and subscribe only if it is still false.
        event Action Ready;

        // Safe to call from several places: the first call runs the modules, and any call made while that one is still
        // running awaits the same init rather than returning early.
        UniTask InitializeAsync(CancellationToken ct);

        // Completes once the module with this SdkInitIds id has reported - at once if it already has, and no later
        // than IsReady even for an id this build never reports. Lets a boot timeout start after the player-paced
        // steps (the tracking prompt, the ATT dialog) instead of counting the player's reading time as SDK work.
        UniTask WaitForModuleAsync(string moduleId, CancellationToken ct);
    }
}
