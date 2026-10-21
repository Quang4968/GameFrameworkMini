using System;
using System.Collections.Generic;
using System.Globalization;

namespace MoodBoost.Sdk
{
    // The only way game code should read remote config.
    //
    // Four overloads all named Get, distinguished by the key's type, so the call site does not have to remember
    // GetDouble versus GetLong — and cannot accidentally call GetLong on a double-typed number key, which Firebase
    // would silently truncate.
    //
    // No overload takes a fallback: the default already lives in the key. That is the entire reason ConfigKey
    // exists.
    public static class RemoteConfigExtensions
    {
        public static bool Get(this IRemoteConfigService config, in ConfigKey<bool> key)
            => config.GetBool(key.Name, RemoteConfigDefaults.Bool(key));

        public static long Get(this IRemoteConfigService config, in ConfigKey<long> key)
            => config.GetLong(key.Name, RemoteConfigDefaults.Long(key));

        public static double Get(this IRemoteConfigService config, in ConfigKey<double> key)
            => config.GetDouble(key.Name, RemoteConfigDefaults.Double(key));

        public static string Get(this IRemoteConfigService config, in ConfigKey<string> key)
            => config.GetString(key.Name, RemoteConfigDefaults.String(key));
    }

    // Project-owned defaults are installed from SdkSettings before the SDK starts. The static
    // ConfigKey default remains a safe fallback for projects that have not configured an asset yet.
    public static class RemoteConfigDefaults
    {
        static readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal);

        public static void Set(IEnumerable<KeyValuePair<string, string>> values)
        {
            Values.Clear();
            if (values == null) return;
            foreach (var pair in values)
                if (!string.IsNullOrWhiteSpace(pair.Key)) Values[pair.Key] = pair.Value ?? string.Empty;
        }

        internal static bool Bool(in ConfigKey<bool> key)
            => Values.TryGetValue(key.Name, out var value) && bool.TryParse(value, out var parsed) ? parsed : key.Default;
        internal static long Long(in ConfigKey<long> key)
            => Values.TryGetValue(key.Name, out var value) && long.TryParse(value, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var parsed) ? parsed : key.Default;
        internal static double Double(in ConfigKey<double> key)
            => Values.TryGetValue(key.Name, out var value) && double.TryParse(value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var parsed) ? parsed : key.Default;
        internal static string String(in ConfigKey<string> key)
            => Values.TryGetValue(key.Name, out var value) ? value : key.Default;
    }
}
