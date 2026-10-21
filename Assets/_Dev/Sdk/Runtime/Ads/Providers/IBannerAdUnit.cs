using System;

namespace MoodBoost.Sdk.Ads.Providers
{
    // A framed ad that stays on screen (banner, MREC).
    //
    // Fundamentally different from full-screen in that it has no "result": it cannot be awaited and has no end. The
    // network refreshes it on its own cadence, and each refresh is a new impression - so ImpressionRecorded here fires
    // many times within a single Show.
    public interface IBannerAdUnit : IDisposable
    {
        string AdUnitId { get; }

        bool IsLoaded { get; }

        void Load();

        void Show();

        void Hide();

        // Needed by the mediation waterfall: without a "has inventory" signal, nobody knows when a failed upper rung
        // allows moving down to the next one. The full-screen version has had Loaded from the start; this side lacked it
        // because back when there was only one network AdService did not need to know.
        event Action Loaded;

        event Action<AdImpression> ImpressionRecorded;

        event Action<string> LoadFailed;
    }
}
