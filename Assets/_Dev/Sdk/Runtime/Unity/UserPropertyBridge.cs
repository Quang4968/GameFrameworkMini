using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk.Tracking;
using UnityEngine;

namespace MoodBoost.Sdk.Unity
{
    // The ONLY PLACE where attribution and device reachability reach tracking - the same shape and the same reason as
    // AdTrackingBridge: no line of game code has to remember these user property keys. Attribution is the null
    // service on TikTok, so only reachability carries data until an MMP provider exists.
    public sealed class UserPropertyBridge : ISdkModule, IDisposable
    {
        const string Tag = "UserProperties";

        // Walking out of Wi-Fi range mid-session is the only case the boot read and the resume hook miss. The publish
        // is deduped inside TrackingService, so the steady-state cost of this poll is one enum read.
        const float ConnectionPollSeconds = 10f;

        readonly ITrackingService tracking;
        readonly IAttributionService attribution;
        readonly IAppLifecycle lifecycle;
        readonly CancellationToken lifetime;

        bool disposed;
        bool attributionPublished;

        public string ModuleName => "UserProperties";

        public int InitOrder => SdkModuleOrder.UserProperties;

        public UserPropertyBridge(
            ITrackingService tracking,
            IAttributionService attribution,
            IAppLifecycle lifecycle,
            CancellationToken lifetime)
        {
            this.tracking = tracking;
            this.attribution = attribution;
            this.lifecycle = lifecycle;
            this.lifetime = lifetime;
        }

        public UniTask InitializeAsync(CancellationToken ct)
        {
            PublishConnection();
            RequestAttribution();

            if (lifecycle != null) lifecycle.Resumed += OnResumed;

            PollConnectionAsync().Forget();

            return UniTask.CompletedTask;
        }

        void OnResumed()
        {
            // Flight mode and network switches almost always happen while the app is in the background.
            PublishConnection();

            // Attribution answers late far more often than it answers wrong, so keep asking until it lands once.
            if (!attributionPublished) RequestAttribution();
        }

        void RequestAttribution()
        {
            if (attribution == null) return;

            tracking.SetAttributionId(attribution.InstallId);

            // Deliberately not awaited: the MMP's callback may arrive minutes late or, with no network, never.
            attribution.RequestAttribution(data =>
            {
                attributionPublished = true;
                tracking.SetAttribution(data);

                // The id often becomes readable only once the MMP has round-tripped.
                tracking.SetAttributionId(attribution.InstallId);
            });
        }

        void PublishConnection() => tracking.SetConnectionType(Read());

        static ConnectionType Read()
        {
            switch (Application.internetReachability)
            {
                case NetworkReachability.NotReachable: return ConnectionType.Offline;
                case NetworkReachability.ReachableViaLocalAreaNetwork: return ConnectionType.Wifi;
                case NetworkReachability.ReachableViaCarrierDataNetwork: return ConnectionType.MobileData;
                default: return ConnectionType.Unknown;
            }
        }

        async UniTaskVoid PollConnectionAsync()
        {
            try
            {
                while (!disposed)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(ConnectionPollSeconds),
                        DelayType.UnscaledDeltaTime,
                        PlayerLoopTiming.Update,
                        lifetime);

                    PublishConnection();
                }
            }
            catch (OperationCanceledException)
            {
                // The app is shutting down.
            }
            catch (Exception error)
            {
                SdkLog.Error(Tag, $"connection poll stopped: {error}");
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            if (lifecycle != null) lifecycle.Resumed -= OnResumed;
        }
    }
}
