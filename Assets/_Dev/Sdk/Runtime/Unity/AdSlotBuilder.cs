using System.Collections.Generic;
using MoodBoost.Sdk.Ads;
using MoodBoost.Sdk.Ads.Providers;

namespace MoodBoost.Sdk.Unity
{
    // Pairs each compiled adapter with the ad unit ids of that exact network, in the order declared in SdkSettings -
    // and that order is the waterfall order.
    //
    // Two sources have to agree: the define symbols decide WHICH adapters exist, the asset decides which networks have
    // ids to run with. A mismatch makes that network vanish silently, so this logs both directions explicitly.
    static class AdSlotBuilder
    {
        const string Tag = "Ads";

        public static List<AdMediationSlot> Build(
            IReadOnlyList<IAdProvider> providers, SdkSettings settings, IConsentProvider consent)
        {
            var slots = new List<AdMediationSlot>(providers.Count);
            // TTSDK has no network test suite; test devices are handled by remote config (ads_test_device_ids).
            const bool testMode = false;

            // The fake network and the null implementation have no entry of their own in the asset. They replace every
            // real network, so build exactly one slot.
            if (providers.Count == 1 && (providers[0].Id == MediationId.Fake || providers[0].Id == MediationId.None))
            {
                // The fake network brings its own id set rather than borrowing the first one in the asset: ids are just
                // strings sent to a real network, and there is no real network here. Borrowing would mean any format
                // left blank in SdkSettings is also off in the Editor - exactly what leaves the "Free" button
                // permanently dimmed despite MB_ADS_FAKE being on.
                var catalog = providers[0].Id == MediationId.Fake
                    ? BuildFakeCatalog()
                    : Borrow(settings);

                slots.Add(new AdMediationSlot(
                    providers[0].Id,
                    providers[0],
                    catalog,
                    catalog,
                    new AdProviderContext(string.Empty, userId: null, consent, testMode)));

                return slots;
            }

            var used = new HashSet<MediationId>();

            if (settings != null)
            {
                foreach (var config in settings.Mediations)
                {
                    var provider = Find(providers, config.mediation);

                    if (provider == null)
                    {
                        SdkLog.Warn(Tag, $"{config.mediation} is in SdkSettings but its define symbol " +
                            "is not enabled - skipping this network.");
                        continue;
                    }

                    if (!used.Add(config.mediation))
                    {
                        SdkLog.Warn(Tag, $"{config.mediation} is declared twice in SdkSettings - only the first is used.");
                        continue;
                    }

                    slots.Add(new AdMediationSlot(
                        config.mediation,
                        provider,
                        config.BuildCatalog(),
                        config.BuildTestCatalog(),
                        new AdProviderContext(config.appKey.Current, userId: null, consent, testMode)));
                }
            }

            // The other direction: an adapter compiled into the build for which the asset declares no ids just sits there
            // dead. Warn, because this is almost always a forgotten field rather than an intent.
            foreach (var provider in providers)
            {
                if (!used.Contains(provider.Id))
                {
                    SdkLog.Warn(Tag, $"the define symbol for {provider.Id} is enabled but SdkSettings " +
                        "declares no entry for it - this network will not run.");
                }
            }

            return slots;
        }

        // Every format has an id so every format can be enabled - the fake network makes no outside calls, so there is
        // nothing to misconfigure.
        static AdUnitCatalog BuildFakeCatalog()
        {
            var catalog = new AdUnitCatalog();

            catalog.Add(AdFormat.Rewarded, "fake_rewarded");
            catalog.Add(AdFormat.Interstitial, "fake_interstitial");
            catalog.Add(AdFormat.Banner, "fake_banner");
            catalog.Add(AdFormat.AppOpen, "fake_app_open");
            catalog.Add(AdFormat.Mrec, "fake_mrec");

            return catalog;
        }

        static AdUnitCatalog Borrow(SdkSettings settings)
            => settings != null && settings.Mediations.Count > 0
                ? settings.Mediations[0].BuildCatalog()
                : new AdUnitCatalog();

        static IAdProvider Find(IReadOnlyList<IAdProvider> providers, MediationId id)
        {
            for (var i = 0; i < providers.Count; i++)
            {
                if (providers[i].Id == id) return providers[i];
            }

            return null;
        }
    }
}
