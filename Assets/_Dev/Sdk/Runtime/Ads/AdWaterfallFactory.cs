using System;
using System.Collections.Generic;
using MoodBoost.Sdk.Ads.Providers;

namespace MoodBoost.Sdk.Ads
{
    static class AdWaterfallFactory
    {
        const string Tag = "Ads";

        public static IFullScreenAdUnit CreateFullScreen(
            AdFormat format,
            IReadOnlyList<AdMediationSlot> readySlots,
            Action<AdImpression> onImpression,
            Action onClicked)
        {
            var built = new List<(MediationId Id, IFullScreenAdUnit Unit)>(readySlots.Count);

            foreach (var slot in readySlots)
            {
                var adUnitIds = slot.Catalog.Get(format);

                for (var i = 0; i < adUnitIds.Count; i++)
                {
                    var created = slot.Provider.CreateFullScreen(format, adUnitIds[i]);
                    if (created == null)
                    {
                        SdkLog.Warn(Tag, $"{slot.Id} does not support {format} - dropping it from this format's waterfall.");
                        break;
                    }

                    built.Add((slot.Id, created));
                }
            }

            if (built.Count == 0) return null;

            var unit = new AdWaterfallFullScreenAdUnit(format, built);
            unit.ImpressionRecorded += onImpression;
            unit.Clicked += onClicked;
            return unit;
        }

        public static IBannerAdUnit CreateBanner(
            AdFormat format,
            IReadOnlyList<AdMediationSlot> readySlots,
            AdAnchor anchor,
            bool mrec)
        {
            var built = new List<(MediationId Id, IBannerAdUnit Unit)>(readySlots.Count);

            foreach (var slot in readySlots)
            {
                var adUnitIds = slot.Catalog.Get(format);

                for (var i = 0; i < adUnitIds.Count; i++)
                {
                    var created = slot.Provider.CreateBanner(adUnitIds[i], anchor, mrec);
                    if (created == null) break;

                    built.Add((slot.Id, created));
                }
            }

            return built.Count == 0 ? null : new AdWaterfallBannerAdUnit(format, built);
        }
    }
}
