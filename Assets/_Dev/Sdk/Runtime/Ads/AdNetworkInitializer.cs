using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk.Ads
{
    static class AdNetworkInitializer
    {
        const string Tag = "Ads";

        public static async UniTask InitializeAsync(
            IReadOnlyList<AdMediationSlot> slots,
            IRemoteConfigService remoteConfig,
            List<AdMediationSlot> readySlots,
            Action<SdkInitReport> initCompleted,
            CancellationToken ct)
        {
            if (!CanInitialize(slots)) return;

            var useTestUnits = remoteConfig != null
                && AdTestDevices.Resolve(remoteConfig.Get(AdsRemoteConfig.TestDeviceIds));

            foreach (var slot in slots)
            {
                slot.UseTestUnits(useTestUnits);
                if (!ShouldInitialize(slot, useTestUnits)) continue;

                var report = await InitializeSlotAsync(slot, ct);
                initCompleted?.Invoke(report);

                if (report.Result == SdkInitResult.Success) readySlots.Add(slot);
                else SdkLog.Warn(Tag, $"{slot.Id} failed to init - dropping this network, the rest still run.");
            }
        }

        static bool CanInitialize(IReadOnlyList<AdMediationSlot> slots)
        {
            if (slots.Count == 0) return false;
            if (slots[0].Id == MediationId.Fake) return true;
            if (slots[0].Context.Consent.HasGdprConsent) return true;

            SdkLog.Warn(Tag, "The player has not consented to tracking - no network initialized, every format off.");
            return false;
        }

        static bool ShouldInitialize(AdMediationSlot slot, bool useTestUnits)
        {
            if (FakeAdInitGate.ShouldInitialize(slot.Id, useTestUnits, IsEditor())) return true;

            SdkLog.Warn(Tag, "Fake ads are compiled in, but this device is not in ads_test_device_ids - fake ads stay off.");
            return false;
        }

        static bool IsEditor()
        {
#if UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }

        static async UniTask<SdkInitReport> InitializeSlotAsync(AdMediationSlot slot, CancellationToken ct)
        {
            bool ok;
            string failure = null;
            var clock = Stopwatch.StartNew();

            try
            {
                ok = await slot.Provider.InitializeAsync(slot.Context, ct);
                if (!ok) failure = "network declined";
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception error)
            {
                SdkLog.Exception(Tag, error);
                failure = error.Message;
                ok = false;
            }

            clock.Stop();

            return SdkInitReport.Provider(
                IdFor(slot.Id),
                clock.ElapsedMilliseconds,
                ok ? SdkInitResult.Success : SdkInitResult.Fail,
                failure);
        }

        static string IdFor(MediationId id)
        {
            switch (id)
            {
                case MediationId.LevelPlay: return SdkInitIds.LevelPlay;
                case MediationId.Fake: return SdkInitIds.FakeAds;
                case MediationId.TikTok: return SdkInitIds.TikTokAds;
                default: return SdkInitIds.NullAds;
            }
        }
    }
}
