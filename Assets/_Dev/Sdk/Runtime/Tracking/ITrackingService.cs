using System;

namespace MoodBoost.Sdk.Tracking
{
    // The API game code calls. A single Log method, with no distinction about where the event goes - how many
    // tracking sources are enabled is a matter of configuration, not of the call site.
    public interface ITrackingService
    {
        void Log(string eventName);

        void Log(string eventName, in TrackingParams parameters);

        void LogAdRevenue(in AdRevenue revenue);

        void SetUserId(string userId);

        void SetUserProperty(string key, string value);

        // Appends a shared dimension to every later event. Used for experiment/config versions so
        // callers cannot forget the dimension on one of the many typed event helpers.
        void SetGlobalParameter(string key, string value);

        void LogError(Exception error, string context = null);
    }
}
