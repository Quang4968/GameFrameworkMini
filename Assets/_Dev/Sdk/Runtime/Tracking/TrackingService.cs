using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk.Tracking
{
    // Fans one call out to every enabled provider.
    //
    // Two things worth remembering:
    //  - A provider that fails to init is DROPPED from the list rather than being called and having its errors ignored on every event.
    //  - Each provider call is wrapped in its own try/catch: one SDK throwing mid-way must not swallow the event for
    //    the remaining SDKs, and must absolutely never take down the call site - nobody wants to lose a level to a
    //    line of analytics.
    public sealed class TrackingService : ITrackingService, ISdkModule, ISdkInitReporter, IDisposable
    {
        const string Tag = "Tracking";

        readonly List<ITrackingProvider> candidates;
        readonly List<ITrackingProvider> active = new List<ITrackingProvider>();

        // Last value written per key, plus the user id. Kept because a property set before InitializeAsync finishes
        // would otherwise fan out to an empty list and vanish with no log and no exception. Replayed once init ends.
        readonly Dictionary<string, string> userProperties = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> globalParameters = new Dictionary<string, string>(StringComparer.Ordinal);
        string userId;

        bool disposed;

        // What the providers have been told. Read by the editor debug window so QA does not have to wait on
        // Firebase DebugView; never exposed on ITrackingService.
        public IReadOnlyDictionary<string, string> UserProperties => userProperties;

        public string ModuleName => "Tracking";

        public int InitOrder => SdkModuleOrder.Tracking;

        public event Action<SdkInitReport> InitCompleted;

        public TrackingService(IEnumerable<ITrackingProvider> providers)
        {
            candidates = new List<ITrackingProvider>(providers);
        }

        public async UniTask InitializeAsync(CancellationToken ct)
        {
            foreach (var provider in candidates)
            {
                var clock = Stopwatch.StartNew();
                var result = SdkInitResult.Success;
                string error = null;

                try
                {
                    if (await provider.InitializeAsync(ct))
                    {
                        active.Add(provider);
                        SdkLog.Info(Tag, $"enabled {provider.Name}");
                    }
                    else
                    {
                        result = SdkInitResult.Fail;
                        error = "provider declined";
                        SdkLog.Warn(Tag, $"{provider.Name} failed to init - dropped from the list.");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    result = SdkInitResult.Fail;
                    error = exception.Message;
                    SdkLog.Error(Tag, $"{provider.Name} threw during init - dropped from the list: {exception}");
                }
                finally
                {
                    clock.Stop();
                    Report(provider, clock.ElapsedMilliseconds, result, error);
                }
            }

            ReplayIdentity();
        }

        // A provider that is also an ISdkModule initialized itself before this loop and SdkRuntime already reported
        // it; reporting again here would count that step twice.
        void Report(ITrackingProvider provider, long elapsedMs, SdkInitResult result, string error)
        {
            if (provider is ISdkModule) return;

            InitCompleted?.Invoke(SdkInitReport.Provider(IdFor(provider), elapsedMs, result, error));
        }

        // Explicit literals rather than a lowercase of Name: the names are inconsistent already ("FirebaseAnalytics"
        // next to "tenjin"), and these ids become event names that can never change.
        static string IdFor(ITrackingProvider provider)
        {
            switch (provider.Name)
            {
                case "Crashlytics": return SdkInitIds.Crashlytics;
                case "FirebaseAnalytics": return SdkInitIds.FirebaseAnalytics;
                case "tenjin": return SdkInitIds.Mmp;
                case "adjust": return SdkInitIds.Mmp;
                case "log": return SdkInitIds.LogTracking;
                case "tiktok": return SdkInitIds.TikTokTracking;
                default: return provider.Name;
            }
        }

        // Pushes everything set before the providers were up. A provider dropped during init is not in `active` and
        // correctly receives nothing.
        void ReplayIdentity()
        {
            for (var i = 0; i < active.Count; i++)
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        active[i].SetUserId(userId);
                    }
                    catch (Exception error)
                    {
                        SdkLog.Error(Tag, $"{active[i].Name}.SetUserId replay failed: {error}");
                    }
                }

                foreach (var property in userProperties)
                {
                    try
                    {
                        active[i].SetUserProperty(property.Key, property.Value);
                    }
                    catch (Exception error)
                    {
                        SdkLog.Error(Tag, $"{active[i].Name}.SetUserProperty('{property.Key}') replay failed: {error}");
                    }
                }
            }
        }

        public void Log(string eventName)
        {
            var empty = new TrackingParams();
            Log(eventName, empty);
        }

        public void Log(string eventName, in TrackingParams parameters)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            var merged = parameters;
            foreach (var parameter in globalParameters)
            {
                if (!Contains(parameters, parameter.Key)) merged = merged.With(parameter.Key, parameter.Value);
            }

            for (var i = 0; i < active.Count; i++)
            {
                try
                {
                    active[i].LogEvent(eventName, merged);
                }
                catch (Exception error)
                {
                    SdkLog.Error(Tag, $"{active[i].Name}.LogEvent('{eventName}') failed: {error}");
                }
            }
        }

        public void LogAdRevenue(in AdRevenue revenue)
        {
            for (var i = 0; i < active.Count; i++)
            {
                try
                {
                    active[i].LogAdRevenue(revenue);
                }
                catch (Exception error)
                {
                    SdkLog.Error(Tag, $"{active[i].Name}.LogAdRevenue failed: {error}");
                }
            }
        }

        public void SetUserId(string userId)
        {
            if (this.userId == userId) return;
            this.userId = userId;

            for (var i = 0; i < active.Count; i++)
            {
                try
                {
                    active[i].SetUserId(userId);
                }
                catch (Exception error)
                {
                    SdkLog.Error(Tag, $"{active[i].Name}.SetUserId failed: {error}");
                }
            }
        }

        public void SetUserProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;

            // Unchanged value is dropped here: every publish crosses the JNI/ObjC bridge into both Firebase Analytics
            // and Crashlytics' custom keys, and the callers republish on a timer.
            if (userProperties.TryGetValue(key, out var previous) && previous == value) return;
            userProperties[key] = value;

            for (var i = 0; i < active.Count; i++)
            {
                try
                {
                    active[i].SetUserProperty(key, value);
                }
                catch (Exception error)
                {
                    SdkLog.Error(Tag, $"{active[i].Name}.SetUserProperty('{key}') failed: {error}");
                }
            }
        }

        public void SetGlobalParameter(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (string.IsNullOrEmpty(value)) globalParameters.Remove(key);
            else globalParameters[key] = value;
        }

        static bool Contains(in TrackingParams parameters, string key)
        {
            for (var i = 0; i < parameters.Count; i++)
            {
                if (parameters.GetKey(i) == key) return true;
            }

            return false;
        }

        public void LogError(Exception error, string context = null)
        {
            if (error == null) return;

            for (var i = 0; i < active.Count; i++)
            {
                try
                {
                    active[i].LogError(error, context);
                }
                catch (Exception nested)
                {
                    SdkLog.Error(Tag, $"{active[i].Name}.LogError failed: {nested}");
                }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            foreach (var provider in candidates) provider.Dispose();
            candidates.Clear();
            active.Clear();
            userProperties.Clear();
            globalParameters.Clear();
            userId = null;
        }
    }
}
