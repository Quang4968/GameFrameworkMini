using System;

namespace MoodBoost.Sdk.Tracking
{
    // The MMP (mobile measurement partner): Tenjin, Adjust, AppsFlyer. The thing that answers "which ad did this
    // player come from" and attributes revenue back to that campaign.
    //
    // WHY THIS IS NOT MERGED INTO ITrackingProvider
    //
    // Ad revenue and the user id overlap with analytics, and for exactly that part the MMP DOES implement
    // ITrackingProvider, so it receives them through the existing fan-out without any extra wiring.
    //
    // The methods below have no analytics counterpart, and pushing them into ITrackingProvider would force Firebase
    // and Crashlytics to declare empty methods forever. So they live here instead. Events are among them: the MMP plan
    // is its own sheet, not the analytics plan, so the MMP gets LogEvent below and ignores the fan-out's events.
    //
    // SWITCHING TO A DIFFERENT MMP
    //
    // Write a class implementing both ITrackingProvider and this interface, add a define symbol, add a branch in
    // SdkProviderCatalog. Game code is untouched: call sites only ever see IAttributionService.
    public interface IAttributionService
    {
        // "tenjin", "adjust", "appsflyer", or "none" when no MMP is attached.
        string Name { get; }

        // false before init finishes, with a missing API key, or with no MMP at all. Never needed to guard a call - every
        // method below is a safe no-op before it is ready. Branch on it only before persisting that the MMP heard
        // something, as IapTrackingBridge's first-purchase flag does.
        bool IsReady { get; }

        // Ping a new session. MUST be called every time the app returns from the background, not only at launch: the
        // MMP counts sessions and derives retention from this, and missing calls make retention numbers drop for no
        // explicable reason. The SDK already hooks IAppLifecycle.Resumed so the game does not call it by hand.
        void NotifyAppResumed();

        // IAP revenue. Separate from ITrackingService because the MMP needs the receipt to validate with the store,
        // while analytics does not care.
        void LogPurchase(in PurchaseInfo purchase);

        // An event of the MMP plan - names in MmpEvents. Only the name reaches Tenjin, so two things that must be told
        // apart on the dashboard need two names, not one name with a parameter.
        void LogEvent(string eventName);

        // Tracking consent. Call again every time the player changes their choice in the consent form.
        void SetConsent(bool granted);

        // The MMP's own id for this install, empty until the MMP is up. Kept off AttributionData on purpose: that
        // struct answers "which ad did the player come from", while this id exists for organic installs too, and it
        // does not depend on the attribution callback ever firing.
        string InstallId { get; }

        // Install source. The callback may fire late (the MMP has to ask its server) or NEVER fire at all if the
        // network is down - do not block the UI waiting for it.
        void RequestAttribution(Action<AttributionData> onReady);

        // Deferred deeplink: the link the player tapped BEFORE installing the game, returned by the MMP after the
        // first launch. The callback receives an empty string when there is no link.
        void RequestDeferredDeeplink(Action<string> onReady);
    }
}
