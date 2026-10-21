using System;

namespace Game.Core.Services
{
    // Full-screen ad shown between two moments of play. Core only knows the interface; wiring it to
    // a mediation SDK is Game's job, exactly like IRewardedAdService.
    //
    // The one difference from IRewardedAdService is the callback contract below, and it is the whole
    // point of keeping the two apart.
    public interface I_InterstitialAdService
    {
        // Whether an ad is loaded right now. Call sites do NOT need to check it before Show — Show
        // handles every failure itself. Read it only to decide what the UI looks like.
        bool IsReady { get; }

        // onClosed ALWAYS runs — ad watched, skipped, capped, not loaded, or ads removed. The flow
        // behind the ad must never be held hostage by it, so there is nothing to reward and nothing
        // to branch on.
        void Show(Action onClosed);
    }
}
