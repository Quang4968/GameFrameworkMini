using System;
using System.Collections.Generic;
using MoodBoost.Sdk.Ads.Providers;

namespace MoodBoost.Sdk.Ads
{
    sealed class AdBannerController : IDisposable
    {
        readonly IReadOnlyList<AdMediationSlot> readySlots;
        readonly Action<AdImpression> onImpression;
        readonly Action<AdFormat, string, string> onFailed;
        readonly Action<AdFormat, string> onShown;

        IBannerAdUnit banner;
        IBannerAdUnit mrec;
        AdAnchor mrecAnchor;

        public AdBannerController(
            IReadOnlyList<AdMediationSlot> readySlots,
            Action<AdImpression> onImpression,
            Action<AdFormat, string, string> onFailed,
            Action<AdFormat, string> onShown)
        {
            this.readySlots = readySlots;
            this.onImpression = onImpression;
            this.onFailed = onFailed;
            this.onShown = onShown;
        }

        public bool IsReady(AdFormat format)
        {
            switch (format)
            {
                case AdFormat.Banner:
                    return banner != null && banner.IsLoaded;
                case AdFormat.Mrec:
                    return mrec != null && mrec.IsLoaded;
                default:
                    return false;
            }
        }

        public void ApplyPolicy(in AdPolicySettings policy)
        {
            if (!policy.BannerEnabled) banner?.Hide();
        }

        public void ShowBanner(bool providerReady, bool removeAdsPurchased, in AdPolicySettings policy, string placement)
        {
            if (removeAdsPurchased || !providerReady || !policy.BannerEnabled) return;

            if (banner == null)
            {
                banner = CreateBanner(AdFormat.Banner, AdAnchor.Bottom, mrec: false);
                if (banner == null) return;

                banner.ImpressionRecorded += onImpression;
                banner.LoadFailed += error => onFailed?.Invoke(AdFormat.Banner, placement ?? string.Empty, error);
                banner.Load();
            }

            banner.Show();
            onShown?.Invoke(AdFormat.Banner, placement ?? string.Empty);
        }

        public void HideBanner() => banner?.Hide();

        public void ShowMrec(
            bool providerReady,
            bool removeAdsPurchased,
            AdAnchor anchor,
            string placement)
        {
            if (removeAdsPurchased || !providerReady) return;

            if (mrec != null && mrecAnchor != anchor)
            {
                mrec.Dispose();
                mrec = null;
            }

            if (mrec == null)
            {
                mrec = CreateBanner(AdFormat.Mrec, anchor, mrec: true);
                if (mrec == null) return;

                mrecAnchor = anchor;
                mrec.ImpressionRecorded += onImpression;
                mrec.LoadFailed += error => onFailed?.Invoke(AdFormat.Mrec, placement ?? string.Empty, error);
                mrec.Load();
            }

            mrec.Show();
            onShown?.Invoke(AdFormat.Mrec, placement ?? string.Empty);
        }

        public void HideMrec() => mrec?.Hide();

        public void Dispose()
        {
            banner?.Dispose();
            banner = null;
            mrec?.Dispose();
            mrec = null;
        }

        IBannerAdUnit CreateBanner(AdFormat format, AdAnchor anchor, bool mrec)
            => AdWaterfallFactory.CreateBanner(format, readySlots, anchor, mrec);
    }
}
