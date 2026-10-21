using System;
using System.Collections.Generic;
using MoodBoost.Sdk.Tracking;

namespace MoodBoost.Sdk.Unity
{
    // The ONLY PLACE where the init-reporting layer and the tracking layer meet, the same shape as AdTrackingBridge:
    // SdkRuntime and the modules only raise SdkInitReport, tracking only receives calls, and deleting this file removes
    // the startup funnel without any other layer noticing.
    public sealed class SdkInitTrackingBridge : IDisposable
    {
        readonly ISdkRuntime runtime;
        readonly ITrackingService tracking;
        readonly IReadOnlyList<ISdkInitReporter> reporters;

        bool disposed;

        public SdkInitTrackingBridge(
            ISdkRuntime runtime, ITrackingService tracking, IEnumerable<ISdkInitReporter> reporters)
        {
            this.runtime = runtime;
            this.tracking = tracking;

            var list = new List<ISdkInitReporter>();
            foreach (var reporter in reporters)
            {
                if (reporter == null) continue;
                reporter.InitCompleted += OnInitCompleted;
                list.Add(reporter);
            }

            this.reporters = list;

            if (runtime.IsReady) StartupFunnel.MarkSdkReady();
            else runtime.Ready += OnRuntimeReady;
        }

        void OnInitCompleted(SdkInitReport report)
        {
            StartupFunnel.MarkInit(report);

            // SdkRuntime swallows a module's exception so the boot survives it. Swallowed means it is no longer an
            // uncaught exception, so Crashlytics would never see it - this is the only line that gets it there.
            if (report.Result == SdkInitResult.Fail)
            {
                tracking.LogError(new SdkInitException(report), $"sdk_init:{report.Id}");
            }

            // The providers are final and the identity has been replayed onto them (tracking, order 100), and the user
            // properties are published (order 150), so everything queued can now go out carrying the player's segment.
            if (report.IsModule && report.Id == SdkInitIds.UserProperties) StartupFunnel.Flush();
        }

        void OnRuntimeReady()
        {
            StartupFunnel.MarkSdkReady();

            // A second flush is a no-op, and it is what saves the funnel if user_properties ever leaves the module list.
            StartupFunnel.Flush();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            for (var i = 0; i < reporters.Count; i++) reporters[i].InitCompleted -= OnInitCompleted;

            runtime.Ready -= OnRuntimeReady;
        }
    }
}
