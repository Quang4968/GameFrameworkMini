using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk
{
    // The "no remote config" implementation — used when MB_REMOTE_CONFIG is off. Every getter returns exactly the
    // fallback passed in, so game code has no special branch for this case.
    public sealed class NullRemoteConfigService : IRemoteConfigService
    {
        public bool IsReady => false;

        public RemoteConfigFetchStatus FetchStatus => RemoteConfigFetchStatus.Disabled;

        public string FetchError => "Remote Config is not included in this build.";

        public IReadOnlyList<RemoteConfigDebugValue> ActivatedValues => Array.Empty<RemoteConfigDebugValue>();

        public event Action Updated
        {
            add { }
            remove { }
        }

        public UniTask FetchAsync(CancellationToken ct) => UniTask.CompletedTask;

        public string GetString(string key, string fallback = "") => fallback;

        public long GetLong(string key, long fallback = 0) => fallback;

        public double GetDouble(string key, double fallback = 0d) => fallback;

        public bool GetBool(string key, bool fallback = false) => fallback;
    }
}
