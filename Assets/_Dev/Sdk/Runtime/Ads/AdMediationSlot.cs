using MoodBoost.Sdk.Ads.Providers;

namespace MoodBoost.Sdk.Ads
{
    // One mediation network with all three things it needs to run bundled together: the adapter, ITS OWN set of ad
    // unit ids, and the init context (app key, consent, test mode).
    //
    // It exists because ad unit ids cannot be shared between networks: LevelPlay's ids and AdMob's ids are two
    // completely different sets of strings. Back when there was only one mediation, AdService held exactly one
    // AdUnitCatalog; now each network has one, and the slot is what keeps them together.
    //
    // The ORDER of the slot list is the waterfall order: the first slot is asked first.
    public sealed class AdMediationSlot
    {
        public readonly MediationId Id;
        public readonly IAdProvider Provider;
        public readonly AdProviderContext Context;

        readonly AdUnitCatalog live;
        readonly AdUnitCatalog test;

        bool useTest;

        // The id set currently in use. The slot carries BOTH sets because when it is built we do not yet know whether
        // this device is a test device - the test device list lives in remote config, which is only fetched at
        // SdkModuleOrder.RemoteConfig. AdService settles the choice at init, before building the first ad unit.
        public AdUnitCatalog Catalog => useTest ? test : live;

        public AdMediationSlot(
            MediationId id,
            IAdProvider provider,
            AdUnitCatalog catalog,
            AdUnitCatalog testCatalog,
            AdProviderContext context)
        {
            Id = id;
            Provider = provider;
            live = catalog;
            test = testCatalog ?? catalog;
            Context = context;
        }

        // Call EXACTLY ONCE, before AdService builds any ad unit. Changing it afterwards leaves already-built units on
        // the old ids, i.e. a half-and-half state - part test, part live, the hardest kind to track down.
        public void UseTestUnits(bool value) => useTest = value;
    }
}
