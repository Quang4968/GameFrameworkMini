using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk.Tracking
{
    // The EXTENSION POINT for each tracking source: Firebase Analytics, Crashlytics, Adjust, AppsFlyer...
    //
    // A provider that does not handle some area leaves that method empty - Crashlytics only implements LogError and
    // SetUserId, the rest are no-ops. That way TrackingService never has to know who can do what.
    public interface ITrackingProvider : IDisposable
    {
        string Name { get; }

        // Returns false when init fails; TrackingService drops that provider from the fan-out list instead of calling
        // into a SDK that is not ready on every event afterwards.
        UniTask<bool> InitializeAsync(CancellationToken ct);

        void LogEvent(string eventName, in TrackingParams parameters);

        void LogAdRevenue(in AdRevenue revenue);

        void SetUserId(string userId);

        void SetUserProperty(string key, string value);

        void LogError(Exception error, string context);
    }
}
