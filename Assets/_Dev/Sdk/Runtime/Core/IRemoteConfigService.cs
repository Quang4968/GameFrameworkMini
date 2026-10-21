using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk
{
    public enum RemoteConfigFetchStatus
    {
        Disabled,
        Waiting,
        Fetching,
        Activated,
        Failed,
    }

    public readonly struct RemoteConfigDebugValue
    {
        public readonly string Key;
        public readonly string Value;

        public RemoteConfigDebugValue(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    // Configuration fetched remotely (Firebase Remote Config, or our own server later).
    //
    // Every getter takes a fallback and NEVER throws: fetch not finished, no network, key typed wrong — all of
    // them return the fallback. That way call sites do not have to check IsReady before each read, and the game
    // behaves identically with or without remote config.
    public interface IRemoteConfigService
    {
        bool IsReady { get; }

        // A compact diagnostic surface for the in-game dev console. Gameplay must keep using
        // the fallback getters below: remote config is intentionally allowed to fail silently
        // for players, while testers still need to see exactly what happened.
        RemoteConfigFetchStatus FetchStatus { get; }

        string FetchError { get; }

        // Remote values from the last successful activation. This is intentionally diagnostic-only:
        // gameplay always reads an individual typed key with its own fallback.
        IReadOnlyList<RemoteConfigDebugValue> ActivatedValues { get; }

        UniTask FetchAsync(CancellationToken ct);

        string GetString(string key, string fallback = "");

        long GetLong(string key, long fallback = 0);

        double GetDouble(string key, double fallback = 0d);

        bool GetBool(string key, bool fallback = false);

        // Raised after each activation of a new config. Anything read once at boot (ad frequency) listens to this
        // to pick up new values without relaunching the app.
        event Action Updated;
    }
}
