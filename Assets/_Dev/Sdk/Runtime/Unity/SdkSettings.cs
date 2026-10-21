using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoodBoost.Sdk.Unity
{
    // The entire configuration of the SDK, a single asset dragged into the LifetimeScope. This SDK builds TikTok Mini
    // Games: the TikTok ad unit block, the consent declaration and the remote-config defaults are all it holds.
    //
    // A format with no id declared is fully off - see AdUnitCatalog. A format may declare SEVERAL ids: they become the
    // waterfall of that format. Ad frequency lives in AdsRemoteConfig (defaults below), not here.
    [CreateAssetMenu(menuName = "MoodBoost/SDK Settings", fileName = "SdkSettings")]
    public sealed class SdkSettings : ScriptableObject
    {
        [Serializable]
        public struct RemoteConfigDefault
        {
            public string key;
            public string value;
        }
        [Serializable]
        public struct PlatformId
        {
            [SerializeField] string android;
            [SerializeField] string ios;

            // One id for every platform: the TikTok block has a single target, so it fills both slots.
            internal PlatformId(string id)
            {
                android = id;
                ios = id;
            }

            public string Current
            {
                get
                {
#if UNITY_IOS && !UNITY_EDITOR
                    return ios;
#else
                    return android;
#endif
                }
            }
        }

        // The TikTok Mini Game ad units. TTSDK has rewarded and interstitial only.
        [Serializable]
        public struct TikTokAdUnits
        {
            [Tooltip("Rewarded video ad unit ids from the TikTok console, tried in this order. Empty = rewarded off.")]
            public string[] rewarded;

            [Tooltip("Interstitial ad unit ids from the TikTok console, tried in this order. Empty = interstitial off.")]
            public string[] interstitial;

            // The shape AdSlotBuilder and AdService read, so they need no TikTok branch.
            public MediationConfig ToMediation() => new MediationConfig
            {
                mediation = Ads.MediationId.TikTok,
                adUnits = new AdUnitIds { rewarded = ToIds(rewarded), interstitial = ToIds(interstitial) },
            };

            static PlatformId[] ToIds(string[] ids)
                => ids == null ? null : Array.ConvertAll(ids, id => new PlatformId(id));
        }

        // One network's ad unit ids, all five formats. A format holds a LIST because it can run several ad units at
        // once: the order is the order they are tried in, so a high-floor unit can sit above a catch-all one.
        [Serializable]
        public struct AdUnitIds
        {
            public PlatformId[] rewarded;
            public PlatformId[] interstitial;
            public PlatformId[] banner;
            public PlatformId[] appOpen;
            public PlatformId[] mrec;

            public Ads.AdUnitCatalog BuildCatalog()
            {
                var catalog = new Ads.AdUnitCatalog();
                Fill(catalog, Ads.AdFormat.Rewarded, rewarded);
                Fill(catalog, Ads.AdFormat.Interstitial, interstitial);
                Fill(catalog, Ads.AdFormat.Banner, banner);
                Fill(catalog, Ads.AdFormat.AppOpen, appOpen);
                Fill(catalog, Ads.AdFormat.Mrec, mrec);
                return catalog;
            }

            // The id set used on test devices, except that a format with NO test id falls back to the live ids of that
            // same format. That way filling in test ids for AdMob alone is enough: LevelPlay publishes no test ids, and
            // leaving it blank keeps it running live ids like every other device.
            //
            // If a blank format meant fully off instead, a test device would lose whatever formats had not been filled in
            // yet, with symptoms identical to an integration bug - half a day gone before tracing it to the id table.
            public Ads.AdUnitCatalog BuildCatalogOver(in AdUnitIds live)
            {
                var catalog = new Ads.AdUnitCatalog();
                FillOver(catalog, Ads.AdFormat.Rewarded, rewarded, live.rewarded);
                FillOver(catalog, Ads.AdFormat.Interstitial, interstitial, live.interstitial);
                FillOver(catalog, Ads.AdFormat.Banner, banner, live.banner);
                FillOver(catalog, Ads.AdFormat.AppOpen, appOpen, live.appOpen);
                FillOver(catalog, Ads.AdFormat.Mrec, mrec, live.mrec);
                return catalog;
            }

            static void Fill(Ads.AdUnitCatalog catalog, Ads.AdFormat format, PlatformId[] entries)
            {
                if (entries == null) return;

                for (var i = 0; i < entries.Length; i++) catalog.Add(format, entries[i].Current);
            }

            // A test list REPLACES the live one for that format rather than merging into it: a waterfall built half from
            // test ids and half from live ids would put real impressions on a QA device, which is the exact thing test
            // ids exist to prevent.
            static void FillOver(
                Ads.AdUnitCatalog catalog, Ads.AdFormat format, PlatformId[] test, PlatformId[] live)
            {
                Fill(catalog, format, HasAny(test) ? test : live);
            }

            static bool HasAny(PlatformId[] entries)
            {
                if (entries == null) return false;

                for (var i = 0; i < entries.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(entries[i].Current)) return true;
                }

                return false;
            }
        }

        // The configuration of ONE mediation network. Ad unit ids cannot be shared between networks - LevelPlay's ids and
        // AdMob's ids are two completely different sets of strings - so each network carries its own set.
        [Serializable]
        public struct MediationConfig
        {
            [Tooltip("Which network. Must match an enabled define symbol, otherwise this network is skipped.")]
            public Ads.MediationId mediation;

            [Tooltip("The network's app key. LevelPlay calls it appKey, MAX calls it sdkKey. AdMob does NOT use " +
                "this field - its app id is declared under Google Mobile Ads > Settings.")]
            public PlatformId appKey;

            [Tooltip("The LIVE ad unit ids, used by players. A format with no id is fully off. Several ids for " +
                "one format are tried in the order listed here.")]
            public AdUnitIds adUnits;

            [Tooltip("Ad unit ids used on TEST DEVICES - devices whose id is in ads_test_device_ids on " +
                "Remote Config. A format left empty here makes test devices use its live ids.")]
            public AdUnitIds testAdUnits;

            public Ads.AdUnitCatalog BuildCatalog() => adUnits.BuildCatalog();

            public Ads.AdUnitCatalog BuildTestCatalog() => testAdUnits.BuildCatalogOver(adUnits);
        }

        [Header("TikTok Mini Game")]
        [SerializeField] TikTokAdUnits tikTok;

        // Built once so Mediations stays reference-stable; an Inspector edit drops it in OnValidate.
        [NonSerialized] MediationConfig[] tikTokMediations;

        [Header("Consent")]
        [Tooltip("Declares players as under the age of consent: ads get restricted and eCPM drops, so enable it " +
            "only when the game genuinely targets children.")]
        [SerializeField] bool tagForUnderAgeOfConsent;

        // Remote Config fields are hidden from this Inspector and edited in MoodBoost > Remote Config instead: that window
        // lists every declared key next to its code default, so a default typed here by hand could name a key that does
        // not exist and nothing would say so. RemoteConfigWindow looks both fields up by name - rename them there too.

        // Per-project overrides of ConfigKey defaults: the value a player gets before the first fetch or offline. A key
        // absent from this list runs on the default declared in code.
        [HideInInspector]
        [SerializeField] RemoteConfigDefault[] remoteConfigDefaults = Array.Empty<RemoteConfigDefault>();

        public IReadOnlyList<MediationConfig> Mediations => tikTokMediations ??= new[] { tikTok.ToMediation() };

        public bool TagForUnderAgeOfConsent => tagForUnderAgeOfConsent;

        public IReadOnlyList<RemoteConfigDefault> RemoteConfigDefaults => remoteConfigDefaults;

        void OnValidate() => tikTokMediations = null;
    }
}
