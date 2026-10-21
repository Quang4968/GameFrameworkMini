using System;
using System.Collections.Generic;
using MoodBoost.Sdk.Ads;
using MoodBoost.Sdk.Tracking;

namespace MoodBoost.Sdk.Unity
{
    // The ONLY PLACE in the whole system where the ads layer and the tracking layer meet.
    //
    // This is why the MoodBoost.Sdk.Ads assembly does not reference MoodBoost.Sdk.Tracking or vice versa: ads only
    // raises events on IAdEventStream, tracking only receives calls through ITrackingService, and joining the two ends
    // fits entirely in this file. Removing tracking from the project means deleting exactly this file, and the ads
    // layer notices no difference.
    //
    // Maps the ads layer's own vocabulary onto the four ad events of the tracking plan. AdRequested / AdShown /
    // AdFailed have no event in the plan and are deliberately not logged: a show that never reached the screen is
    // reported as ad_impression with is_show = 0, which is the same fact in the shape the dashboard expects - unless
    // the show was never an opportunity at all (Blocked, Disabled).
    // ad_offer_shown is not here either - it fires when the GAME offers the ad, which the SDK never sees.
    public sealed class AdTrackingBridge : IDisposable
    {
        readonly IAdEventStream ads;
        readonly ITrackingService tracking;

        // The last impression of each format, kept so a click or a completion can name the network that served it.
        // Neither event carries that itself, and the click always follows the impression for the same ad.
        readonly Dictionary<AdFormat, AdImpression> lastImpression = new Dictionary<AdFormat, AdImpression>(5);

        bool disposed;

        public AdTrackingBridge(IAdEventStream ads, ITrackingService tracking)
        {
            this.ads = ads;
            this.tracking = tracking;

            ads.ImpressionRecorded += OnImpression;
            ads.AdClicked += OnClicked;
            ads.LoadCompleted += OnLoadCompleted;
            ads.Completed += OnCompleted;
        }

        void OnImpression(AdImpression impression)
        {
            lastImpression[impression.Format] = impression;

            tracking.LogAdImpression(Info(impression), impression.Revenue, currency: impression.Currency);

            // Converts the ads layer's type into the tracking layer's type. The two mean the same thing but deliberately
            // are not shared - see the comment on AdRevenue.
            // The mediation name comes from the impression ITSELF rather than a global constant: with a multi-network
            // waterfall each display may be served by a different network, and merging two networks by mistake gives
            // wrong ROAS numbers with no way to separate them again.
            tracking.LogAdRevenue(new AdRevenue(
                impression.Mediation.ToString(),
                impression.Network,
                impression.AdUnitId,
                Format(impression.Format).ToParam(),
                impression.Placement,
                impression.Revenue,
                impression.Currency,
                impression.Precision));
        }

        void OnClicked(AdFormat format, string placement)
        {
            tracking.LogAdClick(Info(format, placement, adUnitId: null));
        }

        // A load pass knows which ad unit answered but not which mediation won it - the waterfall reports that only
        // once an impression is recorded - so ad_platform and ad_network on ad_request log as "null".
        void OnLoadCompleted(AdLoadReport report)
        {
            var info = new AdTrackingInfo(
                Format(report.Format), platform: null, network: null, report.AdUnitId, placement: null);

            tracking.LogAdRequest(
                info,
                report.Loaded,
                report.ElapsedMs,
                report.Loaded ? AdErrorType.None : Classify(report.Error),
                report.Loaded ? null : report.Error);
        }

        void OnCompleted(AdCompletion completion)
        {
            var info = Info(completion.Format, completion.Placement, completion.AdUnitId);

            if (completion.Result == AdResult.Completed || completion.Result == AdResult.Skipped)
            {
                tracking.LogAdComplete(
                    info,
                    completion.Result == AdResult.Completed ? AdEndType.Done : AdEndType.Quit,
                    completion.DurationMs);
                return;
            }

            // A rule said no (level gate, cooldown, another ad on screen) or ads are off (remove-ads, SDK not ready).
            // Neither is a failed opportunity, and logging them drags down the show rate on the dashboard.
            if (completion.Result == AdResult.Blocked || completion.Result == AdResult.Disabled) return;

            // Nothing reached the screen, so the network raised no impression of its own - this is the only record
            // that the show was asked for at all.
            tracking.LogAdImpression(
                info, value: 0d, shown: false, errorType: Classify(completion.Result), errorCode: completion.Result.ToString());
        }

        AdTrackingInfo Info(in AdImpression impression) => new AdTrackingInfo(
            Format(impression.Format),
            impression.Mediation.ToString(),
            impression.Network,
            impression.AdUnitId,
            impression.Placement);

        AdTrackingInfo Info(AdFormat format, string placement, string adUnitId)
        {
            lastImpression.TryGetValue(format, out var previous);

            return new AdTrackingInfo(
                Format(format),
                previous.Mediation == MediationId.None ? null : previous.Mediation.ToString(),
                previous.Network,
                string.IsNullOrEmpty(adUnitId) ? previous.AdUnitId : adUnitId,
                placement);
        }

        static TrackedAdFormat Format(AdFormat format)
        {
            switch (format)
            {
                case AdFormat.Rewarded: return TrackedAdFormat.Rewarded;
                case AdFormat.Interstitial: return TrackedAdFormat.Interstitial;
                case AdFormat.Banner: return TrackedAdFormat.Banner;
                case AdFormat.AppOpen: return TrackedAdFormat.AppOpen;
                case AdFormat.Mrec: return TrackedAdFormat.Mrec;
                default: return TrackedAdFormat.Interstitial;
            }
        }

        static AdErrorType Classify(AdResult result)
        {
            switch (result)
            {
                case AdResult.NotReady: return AdErrorType.NotReady;
                default: return AdErrorType.Unknown;
            }
        }

        // Networks report load failures as free-form English text, so the plan's normalized buckets can only be
        // recovered by matching on it. The raw message still goes out as error_code, which is what a fix is traced
        // from when a bucket here turns out to be wrong.
        static AdErrorType Classify(string error)
        {
            if (string.IsNullOrEmpty(error)) return AdErrorType.Unknown;

            var text = error.ToLowerInvariant();

            if (text.Contains("no fill") || text.Contains("no_fill") || text.Contains("nofill")) return AdErrorType.NoFill;
            if (text.Contains("timeout") || text.Contains("timed out")) return AdErrorType.Timeout;
            if (text.Contains("network") || text.Contains("connection") || text.Contains("offline")) return AdErrorType.Network;
            if (text.Contains("no mediation configured") || text.Contains("invalid") || text.Contains("configuration"))
                return AdErrorType.Config;

            return AdErrorType.Unknown;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            ads.ImpressionRecorded -= OnImpression;
            ads.AdClicked -= OnClicked;
            ads.LoadCompleted -= OnLoadCompleted;
            ads.Completed -= OnCompleted;

            lastImpression.Clear();
        }
    }
}
