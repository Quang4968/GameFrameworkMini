using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk.Ads
{
    // The API game code calls. Deliberately thin: 3 awaitable methods plus 4 show/hide methods for framed ads, no
    // Load, no callbacks, no state to manage yourself.
    //
    // Load/retry/capping all live inside AdService. The call site only asks "show me this" and awaits the result - on
    // failure it gets NotReady/Failed, never a hang and never a throw.
    public interface IAdService
    {
        // The remove-ads pack was bought. Turning it on disables every format except rewarded immediately: rewarded is
        // something the player CHOOSES to watch for a reward, and the remove-ads pack does not buy that away.
        bool RemoveAdsPurchased { get; set; }

        // Views read this to enable/disable a button, not to decide whether to call Show -
        // Show checks again itself.
        bool IsReady(AdFormat format);

        UniTask<AdResult> ShowRewardedAsync(string placement, CancellationToken ct);

        UniTask<AdResult> ShowInterstitialAsync(string placement, CancellationToken ct);

        // App Open: called when the app returns from the background. A network without this format returns Disabled.
        UniTask<AdResult> ShowAppOpenAsync(string placement, CancellationToken ct);

        void ShowBanner(string placement = null);

        void HideBanner();

        void ShowMrec(AdAnchor anchor, string placement = null);

        void HideMrec();
    }
}
