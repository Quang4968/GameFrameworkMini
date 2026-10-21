using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk.Ads;
using Game.Core.Services;

namespace Game.Game.Services
{
    // Binds Core's I_InterstitialAdService to the SDK, mirroring SdkRewardedAdService.
    //
    // Frequency capping — cooldowns and the first level — lives in the SDK (AdService returns
    // Blocked), so nothing here and nothing in the popups counts levels or measures a cooldown.
    public sealed class SdkInterstitialAdService : I_InterstitialAdService
    {
        readonly IAdService ads;
        readonly string placement;

        public bool IsReady => ads.IsReady(AdFormat.Interstitial);

        public SdkInterstitialAdService(IAdService ads, string placement)
        {
            this.ads = ads;
            this.placement = placement;
        }

        public void Show(Action onClosed)
        {
            ShowAsync(onClosed).Forget();
        }

        async UniTaskVoid ShowAsync(Action onClosed)
        {
            // CancellationToken.None for the same reason as the rewarded service: once the ad covers
            // the screen there is nothing to cancel back to.
            await ads.ShowInterstitialAsync(placement, CancellationToken.None);

            // Every AdResult lands here on purpose — see I_InterstitialAdService.Show.
            onClosed?.Invoke();
        }
    }
}
