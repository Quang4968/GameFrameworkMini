using System;
using System.Threading;
using UnityEngine;

namespace MoodBoost.Sdk.Unity
{
    // The SDK's single in-scene object, carrying DontDestroyOnLoad.
    //
    // Why it has to exist, even though the project's architecture dislikes objects living across scenes: ad and
    // analytics SDKs initialize ONCE per play session, while the LifetimeScope is wiped on every scene change (see
    // the scene-change / per-scene DI section of ARCHITECTURE.md). Re-initializing per scene loses loaded inventory,
    // double-counts sessions, and on some networks crashes.
    //
    // It holds exactly three things: a token living for the app's lifetime, the OnApplicationPause signal, and the
    // teardown command. Everything else still goes through the container as an ordinary service.
    public sealed class SdkHost : MonoBehaviour, IAppLifecycle
    {
        CancellationTokenSource lifetimeCts;

        public CancellationToken LifetimeToken => lifetimeCts?.Token ?? CancellationToken.None;

        public event Action Resumed;
        public event Action Paused;

        public event Action Disposing;

        public static SdkHost Create()
        {
            var go = new GameObject("[MoodBoost SDK]");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.NotEditable;

            var host = go.AddComponent<SdkHost>();
            host.lifetimeCts = new CancellationTokenSource();
            return host;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Paused?.Invoke();
            else Resumed?.Invoke();
        }

        void OnDestroy()
        {
            // Cancel the token BEFORE announcing teardown: retry loops waiting on Delay have to stop before the ad units
            // are disposed, otherwise they wake up and call into dead objects.
            lifetimeCts?.Cancel();

            Disposing?.Invoke();
            Disposing = null;

            lifetimeCts?.Dispose();
            lifetimeCts = null;

            Resumed = null;
            Paused = null;
        }
    }
}
