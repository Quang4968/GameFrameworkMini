#if UNITY_EDITOR
using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MoodBoost.Sdk.Tracking
{
    // Prints every event and user property to the Unity console instead of sending it anywhere.
    //
    // It exists because the real providers cannot run in the Editor: Firebase Analytics needs a native library that
    // is not there on desktop, so the whole tracking fan-out is silent in play mode and there is no way to see
    // whether a call site fires at all. This gives one.
    //
    // Wrapped in #if UNITY_EDITOR, so no line of it reaches a build - the same guard as
    // SystemClockService.DebugOffset, and for the same reason: a debug channel with no way to leak out.
    // Uses Debug.Log rather than SdkLog.Info on purpose: Info is [Conditional("MB_SDK_LOG")] and would need a
    // project-wide define that also turns on the very noisy ad logs. Being visible IS this class's whole job.
    public sealed class LogTrackingProvider : ITrackingProvider
    {
        const string Tag = "[SDK/Tracking]";

        readonly StringBuilder builder = new StringBuilder();

        public string Name => "log";

        public UniTask<bool> InitializeAsync(CancellationToken ct) => UniTask.FromResult(true);

        public void LogEvent(string eventName, in TrackingParams parameters)
        {
            builder.Clear();
            builder.Append(eventName);

            for (var i = 0; i < parameters.Count; i++)
            {
                builder.Append(i == 0 ? " { " : ", ");
                builder.Append(parameters.GetKey(i)).Append('=').Append(parameters.GetValue(i));
            }

            if (parameters.Count > 0) builder.Append(" }");

            Debug.Log($"{Tag} {builder}");
        }

        public void LogAdRevenue(in AdRevenue revenue)
            => Debug.Log($"{Tag} ad_revenue {revenue.Value} {revenue.Currency} from {revenue.AdNetwork}");

        public void SetUserId(string userId) => Debug.Log($"{Tag} user_id = {userId}");

        public void SetUserProperty(string key, string value) => Debug.Log($"{Tag} prop {key} = {value}");

        public void LogError(Exception error, string context) { }

        public void Dispose() { }
    }
}
#endif
