using System;

namespace MoodBoost.Sdk.Ads
{
    // Everything worth noticing in the ads layer is raised here, and ONLY here.
    //
    // This is why the Ads assembly does not need to know Tracking exists: it only raises events, and whoever wants to
    // record them listens. AdTrackingBridge (in the Unity layer) is the single place joining the two. Changing the
    // tracking stack, or removing tracking entirely, touches not one line in this assembly.
    public interface IAdEventStream
    {
        // Carries the revenue - what ROAS is computed from. Listen to this, not to AdShown.
        event Action<AdImpression> ImpressionRecorded;

        event Action<AdFormat, string> AdRequested;

        event Action<AdFormat, string> AdShown;

        event Action<AdFormat, string> AdClicked;

        // format, placement, error message.
        event Action<AdFormat, string, string> AdFailed;

        // One finished load pass over a format's waterfall, with how long it took. Raised for full-screen formats
        // only - banners keep themselves stocked on the network's own refresh cycle.
        event Action<AdLoadReport> LoadCompleted;

        // The end of one attempt to show a full-screen ad, including the attempts that never reached the screen.
        event Action<AdCompletion> Completed;
    }
}
