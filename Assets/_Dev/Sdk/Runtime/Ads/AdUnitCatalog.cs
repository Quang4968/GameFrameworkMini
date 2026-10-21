using System.Collections.Generic;

namespace MoodBoost.Sdk.Ads
{
    // The table of ad unit ids of ONE network, per format. A format can hold SEVERAL ids, and their order is the order
    // they are tried in: the second is only asked once the first has answered that it has no inventory. That is how one
    // format runs a floor-price waterfall - a high-floor unit above a catch-all one.
    //
    // A format with no id gets no unit created by AdService, and every call for that format returns Disabled. This is
    // exactly how a format is turned off without touching code: clear its ids in SdkSettings and you are done.
    public sealed class AdUnitCatalog
    {
        const string Tag = "Ads";

        static readonly IReadOnlyList<string> Empty = new string[0];

        readonly Dictionary<AdFormat, List<string>> ids = new Dictionary<AdFormat, List<string>>(5);

        // Appends one rung. A blank id is dropped rather than stored: an empty field in the asset means "not filled in",
        // and handing it to a network is a request that can only fail.
        public void Add(AdFormat format, string adUnitId)
        {
            if (string.IsNullOrWhiteSpace(adUnitId)) return;

            var id = adUnitId.Trim();

            if (!ids.TryGetValue(format, out var list))
            {
                list = new List<string>(2);
                ids[format] = list;
            }

            // The same id twice would build two ad units on one dashboard unit: double the requests, while the lower
            // rung can never win because the upper one fills first. Always a copy-paste slip, so drop it.
            if (list.Contains(id))
            {
                SdkLog.Warn(Tag, $"{format} declares the ad unit id {id} twice - only the first is used.");
                return;
            }

            list.Add(id);
        }

        // The ids of a format in declaration order, i.e. in waterfall order. Empty when the format is off.
        public IReadOnlyList<string> Get(AdFormat format) => ids.TryGetValue(format, out var list) ? list : Empty;

        public bool Has(AdFormat format) => ids.ContainsKey(format);
    }
}
