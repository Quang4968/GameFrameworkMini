using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk.Ads.Providers;

namespace MoodBoost.Sdk.Ads
{
    // The conductor of the ads layer: initializes networks, builds ad units from the catalog, keeps inventory ready,
    // applies the frequency rules, and raises events on IAdEventStream.
    //
    // It is the ONLY place in the whole system that knows about IAdProvider. Changing mediation means changing one
    // constructor argument; game code sees no difference.
    //
    // It also does not know tracking exists - see IAdEventStream.
    public sealed class AdService : IAdService, IAdEventStream, ISdkModule, ISdkInitReporter, IDisposable
    {
        const string Tag = "Ads";

        // The order is the waterfall order: the first slot is asked first.
        readonly IReadOnlyList<AdMediationSlot> slots;
        readonly List<AdMediationSlot> readySlots = new List<AdMediationSlot>(2);

        readonly IRemoteConfigService remoteConfig;
        readonly AdFrequencyPolicy frequency;
        readonly AdBannerController banners;
        readonly CancellationToken lifetime;

        // The current snapshot of the rules, read from remote config. Changeable at runtime, so NOT readonly.
        AdPolicySettings policy;

        readonly Dictionary<AdFormat, IFullScreenAdUnit> fullScreenUnits = new Dictionary<AdFormat, IFullScreenAdUnit>(3);
        readonly Dictionary<AdFormat, AdUnitLoadLoop> loadLoops = new Dictionary<AdFormat, AdUnitLoadLoop>(3);

        // The placement each format was last shown for. Clicks and impressions arrive from the network with no
        // placement of their own - the adapter is only told one when ForwardPlacementToNetwork is on - so without
        // this every click would be attributed to nowhere.
        readonly Dictionary<AdFormat, string> lastPlacement = new Dictionary<AdFormat, string>(3);

        bool providerReady;
        bool isShowingFullScreen;
        bool disposed;

        public bool RemoveAdsPurchased { get; set; }

        public string ModuleName => $"Ads ({string.Join("+", System.Linq.Enumerable.Select(slots, s => s.Id))})";

        public int InitOrder => SdkModuleOrder.Ads;

        public event Action<SdkInitReport> InitCompleted;

        public event Action<AdImpression> ImpressionRecorded;
        public event Action<AdFormat, string> AdRequested;
        public event Action<AdFormat, string> AdShown;
        public event Action<AdFormat, string> AdClicked;
        public event Action<AdFormat, string, string> AdFailed;
        public event Action<AdLoadReport> LoadCompleted;
        public event Action<AdCompletion> Completed;

        // Takes an IRemoteConfigService rather than a ready-made AdPolicySettings: ad frequency is something that has
        // to be tuned against data AFTER release, so it must not be frozen at construction time. With MB_REMOTE_CONFIG
        // off, pass a NullRemoteConfigService and every getter returns the ConfigKey default - there is no separate
        // branch for that case.
        public AdService(
            IReadOnlyList<AdMediationSlot> slots,
            IRemoteConfigService remoteConfig,
            IPlayerLevel playerLevel,
            ISdkClock clock,
            CancellationToken lifetime)
        {
            this.slots = slots;
            this.remoteConfig = remoteConfig;
            this.lifetime = lifetime;

            policy = AdsRemoteConfig.BuildPolicy(remoteConfig);
            frequency = new AdFrequencyPolicy(clock, playerLevel, policy);
            banners = new AdBannerController(
                readySlots,
                OnImpression,
                (format, placement, error) => AdFailed?.Invoke(format, placement, error),
                (format, placement) => AdShown?.Invoke(format, placement));

            if (remoteConfig != null) remoteConfig.Updated += OnRemoteConfigUpdated;
        }

        void OnRemoteConfigUpdated() => ApplyPolicy(AdsRemoteConfig.BuildPolicy(remoteConfig));

        // Pushes the new numbers down to everything holding a copy.
        //
        // Does not rebuild AdFrequencyPolicy or the load loops: rebuilding would wipe the recorded timestamps, and the
        // player would get an interstitial right after every config change.
        void ApplyPolicy(in AdPolicySettings value)
        {
            policy = value;
            frequency.ApplySettings(value);

            foreach (var loop in loadLoops.Values) loop.ApplySettings(value);

            banners.ApplyPolicy(value);
        }

        public async UniTask InitializeAsync(CancellationToken ct)
        {
            ApplyPolicy(AdsRemoteConfig.BuildPolicy(remoteConfig));

            await AdNetworkInitializer.InitializeAsync(slots, remoteConfig, readySlots, InitCompleted, ct);

            providerReady = readySlots.Count > 0;

            if (!providerReady)
            {
                SdkLog.Warn(Tag, "No network could init - every format off, the game still runs.");
                return;
            }

            // Only build units for formats that HAVE an id. A format missing its id is treated as nonexistent - that is
            // how a format is turned off by configuration rather than by code.
            CreateFullScreen(AdFormat.Rewarded);
            CreateFullScreen(AdFormat.Interstitial);
            CreateFullScreen(AdFormat.AppOpen);
        }

        void CreateFullScreen(AdFormat format)
        {
            var unit = AdWaterfallFactory.CreateFullScreen(
                format, readySlots, OnImpression, () => AdClicked?.Invoke(format, PlacementOf(format)));
            if (unit == null) return;

            fullScreenUnits[format] = unit;

            var loop = new AdUnitLoadLoop(unit, policy, lifetime);
            loop.LoadCompleted += report => LoadCompleted?.Invoke(report);
            loadLoops[format] = loop;
            loop.RequestLoad();
        }

        void OnImpression(AdImpression impression)
        {
            // Only fill in what the adapter could not know; a placement the network did report stays untouched.
            if (string.IsNullOrEmpty(impression.Placement))
            {
                impression = impression.WithPlacement(PlacementOf(impression.Format));
            }

            ImpressionRecorded?.Invoke(impression);
        }

        string PlacementOf(AdFormat format)
            => lastPlacement.TryGetValue(format, out var placement) ? placement : string.Empty;

        public bool IsReady(AdFormat format)
        {
            if (!providerReady) return false;
            if (RemoveAdsPurchased && format != AdFormat.Rewarded) return false;
            if (format == AdFormat.Banner && !policy.BannerEnabled) return false;

            switch (format)
            {
                case AdFormat.Banner:
                case AdFormat.Mrec:
                    return banners.IsReady(format);
                default:
                    return fullScreenUnits.TryGetValue(format, out var unit) && unit.IsReady;
            }
        }

        public UniTask<AdResult> ShowRewardedAsync(string placement, CancellationToken ct)
            => ShowFullScreenAsync(AdFormat.Rewarded, placement, ct);

        public UniTask<AdResult> ShowInterstitialAsync(string placement, CancellationToken ct)
            => ShowFullScreenAsync(AdFormat.Interstitial, placement, ct);

        public UniTask<AdResult> ShowAppOpenAsync(string placement, CancellationToken ct)
            => ShowFullScreenAsync(AdFormat.AppOpen, placement, ct);

        async UniTask<AdResult> ShowFullScreenAsync(AdFormat format, string placement, CancellationToken ct)
        {
            placement ??= string.Empty;

            // Remembered before any early return: a click or an impression can still arrive for the previous show.
            lastPlacement[format] = placement;

            // The remove-ads pack does not buy away rewarded: that is something the player chooses to watch for a reward.
            if (RemoveAdsPurchased && format != AdFormat.Rewarded)
                return Finish(format, placement, string.Empty, AdResult.Disabled, 0L);

            if (!providerReady || !fullScreenUnits.TryGetValue(format, out var unit))
                return Finish(format, placement, string.Empty, AdResult.Disabled, 0L);

            // Two overlapping ads means either a crash or one swallowing the other, depending on the network. Blocked
            // here rather than trusting the call site not to tap twice.
            if (isShowingFullScreen) return Finish(format, placement, unit.AdUnitId, AdResult.Blocked, 0L);

            if (!frequency.CanShow(format)) return Finish(format, placement, unit.AdUnitId, AdResult.Blocked, 0L);

            if (!unit.IsReady)
            {
                // Out of inventory: fire one load for the next tap and return immediately - do NOT wait for it. Waiting
                // here makes the player stare at a frozen screen for an unknown length of time.
                loadLoops[format].RequestLoad();
                AdFailed?.Invoke(format, placement, "not ready");
                return Finish(format, placement, unit.AdUnitId, AdResult.NotReady, 0L);
            }

            AdRequested?.Invoke(format, placement);

            isShowingFullScreen = true;
            SetPauseGame(true);

            var adUnitId = unit.AdUnitId;
            var showClock = Stopwatch.StartNew();

            AdResult result;
            try
            {
                // The game's placement name is not automatically a mediation placement - see
                // AdPolicySettings.ForwardPlacementToNetwork.
                var networkPlacement = policy.ForwardPlacementToNetwork ? placement : null;
                result = await unit.ShowAsync(networkPlacement, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception error)
            {
                SdkLog.Exception(Tag, error);
                result = AdResult.Failed;
            }
            finally
            {
                showClock.Stop();
                SetPauseGame(false);
                isShowingFullScreen = false;
            }

            // Once the screen has been taken over, the cooldown has to run, even if the player skipped.
            if (result == AdResult.Completed || result == AdResult.Skipped)
            {
                frequency.NotifyShown(format);
                AdShown?.Invoke(format, placement);
            }
            else
            {
                AdFailed?.Invoke(format, placement, result.ToString());
            }

            // Reload immediately for next time. This is why the second tap still has an ad.
            loadLoops[format].RequestLoad();

            return Finish(format, placement, adUnitId, result, showClock.ElapsedMilliseconds);
        }

        AdResult Finish(AdFormat format, string placement, string adUnitId, AdResult result, long durationMs)
        {
            Completed?.Invoke(new AdCompletion(format, placement, adUnitId, result, durationMs));
            return result;
        }

        public void ShowBanner(string placement = null)
            => banners.ShowBanner(providerReady, RemoveAdsPurchased, policy, placement);

        // Calls every network: only the one actually showing an ad does anything, the rest are no-ops.
        void SetPauseGame(bool pause)
        {
            for (var i = 0; i < readySlots.Count; i++) readySlots[i].Provider.SetPauseGame(pause);
        }

        public void HideBanner() => banners.HideBanner();

        public void ShowMrec(AdAnchor anchor, string placement = null)
            => banners.ShowMrec(providerReady, RemoveAdsPurchased, anchor, placement);

        public void HideMrec() => banners.HideMrec();

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            if (remoteConfig != null) remoteConfig.Updated -= OnRemoteConfigUpdated;

            foreach (var loop in loadLoops.Values) loop.Dispose();
            loadLoops.Clear();

            foreach (var unit in fullScreenUnits.Values)
            {
                unit.ImpressionRecorded -= OnImpression;
                unit.Dispose();
            }
            fullScreenUnits.Clear();

            banners.Dispose();

            foreach (var slot in slots) slot.Provider.Dispose();
            readySlots.Clear();
        }
    }
}
