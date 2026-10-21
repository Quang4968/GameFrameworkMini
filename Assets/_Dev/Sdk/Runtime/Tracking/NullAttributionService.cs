using System;

namespace MoodBoost.Sdk.Tracking
{
    // The "no MMP attached" implementation - used when every MMP define symbol is off, or when the API key is missing.
    //
    // Every method is a no-op and the two callback methods do NOT call back: "source unknown" is different from
    // "organic", and invoking the callback with an empty AttributionData would make listeners think they have an answer.
    public sealed class NullAttributionService : IAttributionService
    {
        public string Name => "none";

        public bool IsReady => false;

        public string InstallId => string.Empty;

        public void NotifyAppResumed() { }

        public void LogPurchase(in PurchaseInfo purchase) { }

        public void LogEvent(string eventName) { }

        public void SetConsent(bool granted) { }

        public void RequestAttribution(Action<AttributionData> onReady) { }

        public void RequestDeferredDeeplink(Action<string> onReady) { }
    }
}
