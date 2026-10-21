using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk
{
    // Runs each ISdkModule in ascending InitOrder, sequentially rather than in parallel.
    //
    // Sequential is deliberate: consent must finish before ads, remote config must finish before ads read the
    // frequency. Running in parallel loses that order and the bugs surface as "sometimes wrong" on real devices.
    //
    // Each module is wrapped in its own try/catch: one dead SDK must not take the whole boot with it. The game
    // still reaches the Home screen, just without that one piece.
    public sealed class SdkRuntime : ISdkRuntime, ISdkInitReporter
    {
        const string Tag = "Runtime";

        readonly IReadOnlyList<ISdkModule> modules;

        // Lets a second caller await the init already in flight instead of walking away from it. Boot relies on that:
        // SdkSceneStartup starts the init and the boot step then awaits this before loading Home.
        UniTaskCompletionSource initialization;

        readonly HashSet<string> reportedIds = new HashSet<string>();
        readonly List<(string id, UniTaskCompletionSource source)> moduleWaiters =
            new List<(string, UniTaskCompletionSource)>();

        public bool IsReady { get; private set; }

        public event Action<SdkInitReport> InitCompleted;

        public event Action Ready;

        public SdkRuntime(IEnumerable<ISdkModule> modules)
        {
            this.modules = modules.OrderBy(module => module.InitOrder).ToArray();
        }

        public async UniTask InitializeAsync(CancellationToken ct)
        {
            // The boot scene can run again (returning to the main menu, hot reload in the Editor) while the SDK
            // lives across scenes, so a second call must never re-init.
            if (IsReady) return;

            if (initialization != null)
            {
                await initialization.Task;
                return;
            }

            initialization = new UniTaskCompletionSource();

            try
            {
                await RunModulesAsync(ct);
            }
            finally
            {
                IsReady = true;

                // Completed in a finally so a cancelled or throwing init never leaves the other callers waiting on a
                // task that will not finish - Boot would sit on the loading screen forever.
                initialization.TrySetResult();
                ReleaseModuleWaiters(null);

                Ready?.Invoke();
            }
        }

        public UniTask WaitForModuleAsync(string moduleId, CancellationToken ct)
        {
            if (IsReady || reportedIds.Contains(moduleId)) return UniTask.CompletedTask;

            var source = new UniTaskCompletionSource();
            moduleWaiters.Add((moduleId, source));
            return source.Task.AttachExternalCancellation(ct);
        }

        async UniTask RunModulesAsync(CancellationToken ct)
        {
            for (var i = 0; i < modules.Count; i++)
            {
                var module = modules[i];

                if (ct.IsCancellationRequested)
                {
                    ReportCancelled(i);
                    break;
                }

                var id = IdFor(module);
                var clock = Stopwatch.StartNew();
                var result = SdkInitResult.Success;
                string error = null;

                try
                {
                    await module.InitializeAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    result = SdkInitResult.Fail;
                    error = exception.Message;
                    SdkLog.Error(Tag, $"module {module.ModuleName} failed to init, skipping: {exception}");
                }
                finally
                {
                    clock.Stop();

                    // A module that gave up without throwing gets the last word on how it went.
                    if (result == SdkInitResult.Success && module is ISdkModuleOutcome outcome)
                    {
                        result = outcome.InitResult;
                        error = outcome.InitError;
                    }

                    if (ShouldLogInitDone(id, result))
                    {
                        SdkLog.Info(Tag, $"init {module.ModuleName} done");
                    }

                    Report(SdkInitReport.Module(id, clock.ElapsedMilliseconds, result, error));
                }
            }
        }

        void Report(SdkInitReport report)
        {
            reportedIds.Add(report.Id);
            InitCompleted?.Invoke(report);
            ReleaseModuleWaiters(report.Id);
        }

        // null releases every waiter: the init is over, so nothing further will ever report.
        void ReleaseModuleWaiters(string moduleId)
        {
            for (var i = moduleWaiters.Count - 1; i >= 0; i--)
            {
                if (moduleId != null && moduleWaiters[i].id != moduleId) continue;

                moduleWaiters[i].source.TrySetResult();
                moduleWaiters.RemoveAt(i);
            }
        }

        // A module never attempted still gets a report: an absent step and a failed step look identical on the
        // dashboard, and only one of them is worth waking someone up for.
        void ReportCancelled(int from)
        {
            for (var i = from; i < modules.Count; i++)
            {
                Report(SdkInitReport.Module(IdFor(modules[i]), 0L, SdkInitResult.Skipped, "cancelled"));
            }
        }

        // Keyed on InitOrder rather than ModuleName: the order is the one identifier that is already stable, while
        // AdService builds its name from the configured waterfall and IAP's contains spaces.
        static string IdFor(ISdkModule module)
        {
            switch (module.InitOrder)
            {
                case SdkModuleOrder.Crashlytics: return SdkInitIds.Crashlytics;
                case SdkModuleOrder.Consent: return SdkInitIds.Consent;
                case SdkModuleOrder.TrackingAuthorization: return SdkInitIds.TrackingAuthorization;
                case SdkModuleOrder.Tracking: return SdkInitIds.Tracking;
                case SdkModuleOrder.UserProperties: return SdkInitIds.UserProperties;
                case SdkModuleOrder.RemoteConfig: return SdkInitIds.RemoteConfig;
                case SdkModuleOrder.Notifications: return SdkInitIds.Notifications;
                case SdkModuleOrder.Ads: return SdkInitIds.Ads;
                case SdkModuleOrder.Iap: return SdkInitIds.Iap;

                // An external module slotted between the known orders. Its own name is the only thing left to go on.
                default: return "module_" + module.InitOrder;
            }
        }

        static bool ShouldLogInitDone(string id, SdkInitResult result)
        {
            if (result == SdkInitResult.Skipped) return false;
            if (id == SdkInitIds.Tracking || id == SdkInitIds.UserProperties) return false;

            return result == SdkInitResult.Success;
        }
    }
}
