using System.Collections.Generic;
using System.Diagnostics;
using MoodBoost.Sdk.Tracking;
using UnityEngine;

namespace MoodBoost.Sdk.Unity
{
    // Records the launch timeline and holds it until tracking can actually send it.
    //
    // Static, like MoodBoostSdk and SdkHost, and for the same reason those two are: app_launch happens at
    // BeforeSplashScreen where there is no scene and no LifetimeScope, splash_loaded happens in a scene that
    // deliberately has no DI at all, and the marks have to survive two scene changes before a consumer exists.
    //
    // Why the queue lives here rather than inside TrackingService: the per-provider init events are raised WHILE
    // TrackingService is still filling its provider list, so logging them straight through would send each one to a
    // different subset of providers - Firebase would never receive its own init event. One queue, flushed once, keeps
    // every step measured by the same instrument.
    public static class StartupFunnel
    {
        const string Tag = "Startup";

        // The funnel is ~15 marks. The cap is a tripwire for something else logging during boot, not a budget.
        const int MarkCapacity = 24;

        enum MarkKind
        {
            AppLaunch,
            SplashLoaded,
            Init,
            LoadingDone,
        }

        readonly struct Mark
        {
            public readonly MarkKind Kind;
            public readonly SdkInitReport Report;
            public readonly long SinceLaunchMs;
            public readonly long AuxMs;
            public readonly bool WaitedOnSdk;
            public readonly bool SdkTimedOut;
            public readonly bool WasBackgrounded;

            public Mark(MarkKind kind, long sinceLaunchMs, bool wasBackgrounded, SdkInitReport report = default,
                long auxMs = 0L, bool waitedOnSdk = false, bool sdkTimedOut = false)
            {
                Kind = kind;
                Report = report;
                SinceLaunchMs = sinceLaunchMs;
                AuxMs = auxMs;
                WaitedOnSdk = waitedOnSdk;
                SdkTimedOut = sdkTimedOut;
                WasBackgrounded = wasBackgrounded;
            }
        }

        static readonly Stopwatch Clock = new Stopwatch();
        static readonly List<Mark> Pending = new List<Mark>(MarkCapacity);

        static ITrackingService tracking;
        static bool flushed;
        static bool overflowWarned;
        static bool wasBackgrounded;
        static bool splashMarked;
        static bool sdkReady;
        static bool homeRevealed;
        static bool loadingDoneSent;
        static bool sdkTimedOut;
        static long sdkReadyMs;
        static long homeRevealedMs;

        public static long ElapsedMs => Clock.ElapsedMilliseconds;

        // Every field is reset HERE rather than in a field initializer. Play Mode runs with domain reload disabled, so
        // static initializers survive between sessions while this method runs again on each one - a clock started in an
        // initializer would report the second session's splash in minutes, and a leftover tracking reference would log
        // into the previous session's disposed service.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void MarkLaunch()
        {
            Application.focusChanged -= OnFocusChanged;
            Application.focusChanged += OnFocusChanged;

            Pending.Clear();
            tracking = null;
            flushed = false;
            overflowWarned = false;
            wasBackgrounded = false;
            splashMarked = false;
            sdkReady = false;
            homeRevealed = false;
            loadingDoneSent = false;
            sdkTimedOut = false;
            sdkReadyMs = 0L;
            homeRevealedMs = 0L;

            // Everything before this line - process start, IL2CPP load, engine boot - is outside the funnel. On a slow
            // Android device that is the largest single term of the whole launch, so it is reported rather than hidden.
            var engineStartupMs = (long)(Time.realtimeSinceStartupAsDouble * 1000d);

            Clock.Restart();

            Queue(new Mark(MarkKind.AppLaunch, engineStartupMs, wasBackgrounded: false));
        }

        public static void MarkSplashLoaded()
        {
            if (splashMarked) return;
            splashMarked = true;

            Queue(new Mark(MarkKind.SplashLoaded, ElapsedMs, wasBackgrounded));
        }

        public static void MarkInit(in SdkInitReport report)
        {
            Queue(new Mark(MarkKind.Init, ElapsedMs, wasBackgrounded, report));
        }

        // timedOut: Boot stopped waiting and went to Home without the SDK. The latch still closes - otherwise
        // loading_done would never fire on exactly the devices worth hearing about - but it closes on the timeout,
        // not on the SDK, and loading_done says so.
        public static void MarkSdkReady(bool timedOut = false)
        {
            if (sdkReady) return;
            sdkReady = true;
            sdkTimedOut = timedOut;
            sdkReadyMs = ElapsedMs;

            TrySendLoadingDone();
        }

        // The loading overlay has faded out and the first playable screen is on the player's screen.
        public static void MarkHomeRevealed()
        {
            if (homeRevealed) return;
            homeRevealed = true;
            homeRevealedMs = ElapsedMs;

            TrySendLoadingDone();
        }

        internal static void Attach(ITrackingService service)
        {
            tracking = service;
        }

        // Sends everything queued so far and switches to sending live. Called once the tracking providers are final,
        // the identity has been replayed onto them, and the user properties have been published.
        public static void Flush()
        {
            if (flushed) return;
            flushed = true;

            for (var i = 0; i < Pending.Count; i++) Send(Pending[i]);

            Pending.Clear();
        }

        static void TrySendLoadingDone()
        {
            if (loadingDoneSent || !sdkReady || !homeRevealed) return;
            loadingDoneSent = true;

            Queue(new Mark(
                MarkKind.LoadingDone, ElapsedMs, wasBackgrounded,
                auxMs: sdkReadyMs, waitedOnSdk: sdkReadyMs >= homeRevealedMs, sdkTimedOut: sdkTimedOut));
        }

        static void Queue(in Mark mark)
        {
            if (flushed)
            {
                Send(mark);
                return;
            }

            // Drops the NEWEST rather than the oldest: the first marks are app_launch and the early inits, which is
            // exactly what this exists to capture.
            if (Pending.Count >= MarkCapacity)
            {
                if (overflowWarned) return;
                overflowWarned = true;
                SdkLog.Warn(Tag, $"more than {MarkCapacity} marks before tracking was live - dropping the rest.");
                return;
            }

            Pending.Add(mark);
        }

        static void Send(in Mark mark)
        {
            if (tracking == null) return;

            switch (mark.Kind)
            {
                case MarkKind.AppLaunch:
                    tracking.LogAppLaunch(mark.SinceLaunchMs);
                    break;
                case MarkKind.SplashLoaded:
                    tracking.LogSplashLoaded(mark.SinceLaunchMs, mark.WasBackgrounded);
                    break;
                case MarkKind.Init:
                    tracking.LogSdkInit(mark.Report, mark.SinceLaunchMs, mark.WasBackgrounded);
                    break;
                case MarkKind.LoadingDone:
                    tracking.LogLoadingDone(
                        mark.SinceLaunchMs, mark.AuxMs, mark.WaitedOnSdk, mark.SdkTimedOut, mark.WasBackgrounded);
                    break;
            }
        }

        // Wall-clock spans keep running while the app sits in the background, so a launch interrupted by a phone call
        // reports tens of seconds. The flag lets those sessions be filtered out instead of silently "corrected".
        static void OnFocusChanged(bool focused)
        {
            if (!focused) wasBackgrounded = true;
        }
    }
}
