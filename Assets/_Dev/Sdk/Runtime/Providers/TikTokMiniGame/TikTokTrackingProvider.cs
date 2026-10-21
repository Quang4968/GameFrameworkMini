using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;

namespace MoodBoost.Sdk.Tracking.Providers
{
    // TikTok Mini Game analytics through TT.ReportEvent: the whole tracking plan reaches the TikTok console with no
    // game-side change. Only events - TTSDK has no ad revenue (ILRD) and no user properties, so those calls are no-ops
    // and revenue is read on the TikTok console.
    //
    // A rejected event (typically one not declared on the TikTok console) is logged once per name, so the gap shows up
    // in the log without one warning per level played.
    public sealed class TikTokTrackingProvider : ITrackingProvider
    {
        const string Tag = "TikTok/Tracking";

        // Same cap as the ads adapter: a container that never answers must not hold up boot.
        static readonly TimeSpan InitTimeout = TimeSpan.FromSeconds(15d);

        readonly HashSet<string> rejectedEvents = new HashSet<string>(StringComparer.Ordinal);

        bool ready;

        public string Name => "tiktok";

        // Calling InitSDK again from the ads adapter later is safe: SdkRuntime runs modules one after another, and a
        // second call after the first has answered returns success. Two calls in flight at once fail the first.
        public async UniTask<bool> InitializeAsync(CancellationToken ct)
        {
            var initSource = new UniTaskCompletionSource<bool>();
            TT.InitSDK((code, env) => initSource.TrySetResult(code == 0));

            var (timedOut, ok) = await initSource.Task
                .TimeoutWithoutException(InitTimeout, DelayType.UnscaledDeltaTime);

            ct.ThrowIfCancellationRequested();

            if (timedOut || !ok)
            {
                SdkLog.Warn(Tag, timedOut ? "InitSDK did not respond - no TikTok analytics." : "InitSDK failed.");
                return false;
            }

            ready = true;
            return true;
        }

        public void LogEvent(string eventName, in TrackingParams parameters)
        {
            if (!ready) return;

            try
            {
                TT.ReportEvent(new ReportEventParam
                {
                    eventName = eventName,
                    @params = ToJson(parameters),
                    fail = () => OnRejected(eventName),
                });
            }
            catch (Exception exception)
            {
                SdkLog.Warn(Tag, $"ReportEvent {eventName} threw: {exception.Message}");
            }
        }

        // No ILRD on TikTok: ad_impression still goes out through LogEvent, revenue is read on the TikTok console.
        public void LogAdRevenue(in AdRevenue revenue)
        {
        }

        // TTSDK has no user identity or user property API; TikTok attributes events to its own account.
        public void SetUserId(string userId)
        {
        }

        public void SetUserProperty(string key, string value)
        {
        }

        // No crash reporting on TikTok.
        public void LogError(Exception error, string context)
        {
        }

        public void Dispose() => ready = false;

        void OnRejected(string eventName)
        {
            if (!rejectedEvents.Add(eventName)) return;

            SdkLog.Warn(Tag, $"TikTok rejected {eventName} - is it declared on the console?");
        }

        static JsonData ToJson(in TrackingParams parameters)
        {
            var json = new JsonData();
            json.SetJsonType(JsonType.Object);

            for (var i = 0; i < parameters.Count; i++)
            {
                var value = parameters.GetValue(i);

                switch (value.Kind)
                {
                    case TrackingValueKind.Long:
                        json[parameters.GetKey(i)] = value.LongValue;
                        break;
                    case TrackingValueKind.Double:
                        json[parameters.GetKey(i)] = value.DoubleValue;
                        break;
                    default:
                        json[parameters.GetKey(i)] = value.StringValue ?? string.Empty;
                        break;
                }
            }

            return json;
        }
    }
}
