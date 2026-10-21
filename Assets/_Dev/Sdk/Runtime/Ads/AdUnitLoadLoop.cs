using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk.Ads.Providers;

namespace MoodBoost.Sdk.Ads
{
    // Keeps one full-screen ad unit stocked: loads the first time, reloads after each show, and retries on a doubling
    // cadence when the network returns no-fill.
    //
    // Split out of AdService because this is the easiest part to get wrong and also the part repeated identically for
    // all three formats. The two bugs it exists to prevent:
    //  - Calling Load() again inside the LoadFailed callback -> a loop firing requests non-stop, burning battery and
    //    getting the IP blocked by the ad network.
    //  - Not calling Load() again after a show -> the second time the player taps, there is nothing left.
    //
    // The delay uses the unscaled clock: ads often show while the game has Time.timeScale = 0.
    sealed class AdUnitLoadLoop : IDisposable
    {
        const string Tag = "AdLoad";

        readonly IFullScreenAdUnit unit;
        readonly CancellationTokenSource cts;

        // NOT readonly: the retry cadence is read from remote config and therefore changes at runtime.
        AdPolicySettings settings;

        // Times the whole waterfall pass, which is what load_time means to the tracking plan: the wait the player
        // would have had, not the wait of whichever single network answered last.
        readonly Stopwatch clock = new Stopwatch();

        int failureCount;
        bool retryScheduled;
        bool disposed;

        public event Action<AdLoadReport> LoadCompleted;

        public AdUnitLoadLoop(IFullScreenAdUnit unit, AdPolicySettings settings, CancellationToken lifetime)
        {
            this.unit = unit;
            this.settings = settings;
            cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime);

            unit.Loaded += OnLoaded;
            unit.LoadFailed += OnLoadFailed;
        }

        // The new cadence applies to the NEXT failure; a wait already in flight runs out on the old cadence. Cutting it
        // short just to change a number would fire one unnecessary request.
        public void ApplySettings(in AdPolicySettings value)
        {
            settings = value;
        }

        // Called at init and after each show.
        public void RequestLoad()
        {
            if (disposed || unit.IsReady) return;

            // Not Restart: a pass may already be in flight - Load() no-ops then, and resetting the clock would make
            // that pass report only the time since the last reload request.
            if (!clock.IsRunning) clock.Restart();

            unit.Load();
        }

        void OnLoaded()
        {
            // One successful load means the network has recovered - do not keep the long wait of the previous failure streak.
            failureCount = 0;
            Report(true, string.Empty);
        }

        void OnLoadFailed(string error)
        {
            if (disposed || retryScheduled) return;

            Report(false, error);

            failureCount++;
            var delay = Math.Min(settings.RetryBaseSeconds * Math.Pow(2d, failureCount - 1), settings.RetryMaxSeconds);
            SdkLog.Info(Tag, $"{unit.Format} load failed {failureCount} time(s) ({error}) - retrying in {delay:0.#}s");

            retryScheduled = true;
            RetryAsync(delay).Forget();
        }

        void Report(bool loaded, string error)
        {
            // Load() returns early when the unit already had an ad, so the clock can be idle here with nothing to time.
            if (!clock.IsRunning) return;

            clock.Stop();
            LoadCompleted?.Invoke(new AdLoadReport(unit.Format, unit.AdUnitId, loaded, clock.ElapsedMilliseconds, error));
        }

        async UniTaskVoid RetryAsync(double delaySeconds)
        {
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(delaySeconds),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                retryScheduled = false;
            }

            RequestLoad();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            unit.Loaded -= OnLoaded;
            unit.LoadFailed -= OnLoadFailed;
            LoadCompleted = null;

            cts.Cancel();
            cts.Dispose();
        }
    }
}
