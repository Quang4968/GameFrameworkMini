using System;
using System.Globalization;

namespace MoodBoost.Sdk
{
    // One remote config parameter, declared exactly ONCE and carrying everything worth knowing about it: name,
    // type, default value, description, group.
    //
    // This is the fundamental difference from calling GetDouble("ads_cooldown", 30d) directly: there the default
    // is scattered across every call site, so two places reading the same key with two different defaults is only
    // a matter of time — and nothing reports it. Here the default is a property of the key, and the call site has
    // no chance to redeclare it.
    //
    // That is also what makes exporting a Firebase template possible: everything the console needs is already in
    // the declaration.
    //     public static class GameConfig
    //     {
    //         public static readonly ConfigKey<double> Cooldown =
    //             ConfigKey.Double("ads_interstitial_cooldown_seconds", 30d, "Pause between two interstitials.", "Ads");
    //     }
    //
    //     var seconds = remoteConfig.Get(GameConfig.Cooldown);
    //
    // Declare with `public static readonly` inside a static class — the Editor template exporter looks for keys in
    // exactly that shape, so any other form never reaches the console.
    public readonly struct ConfigKey<T> : IConfigParameter
    {
        // The parameter name on Firebase. NOT changeable once published: renaming makes every older client lose the
        // value, and the historical data cannot be merged back together.
        public readonly string Name;

        // Used before the fetch completes, with no network, when the key is not on the console yet, or when
        // MB_REMOTE_CONFIG is off entirely. This is the value the game runs on during the first launch, so it has to
        // be a playable configuration rather than a zero for form's sake.
        public readonly T Default;

        // Goes straight into the description field on the console. Whoever tweaks the numbers there is usually not
        // the person who wrote the code, so write it for them.
        public readonly string Description;

        public readonly string Group;

        readonly ConfigValueType valueType;

        internal ConfigKey(string name, T value, ConfigValueType valueType, string description, string group)
        {
            Name = name;
            Default = value;
            this.valueType = valueType;
            Description = description ?? string.Empty;
            Group = group ?? string.Empty;
        }

        string IConfigParameter.Name => Name;

        string IConfigParameter.Description => Description;

        string IConfigParameter.Group => Group;

        ConfigValueType IConfigParameter.ValueType => valueType;

        string IConfigParameter.DefaultAsTemplateValue => Format(Default, valueType);

        static string Format(T value, ConfigValueType type)
        {
            switch (type)
            {
                case ConfigValueType.Bool:
                    // Firebase only accepts lowercase "true"/"false"; .NET's bool.ToString()
                    // returns "True".
                    return (bool)(object)value ? "true" : "false";

                case ConfigValueType.Number:
                    // InvariantCulture is required, not cosmetic: a machine with a VN locale writes
                    // "30,5" and Firebase reads a completely different number.
                    return Convert.ToString(value, CultureInfo.InvariantCulture);

                default:
                    return value as string ?? string.Empty;
            }
        }
    }

    // Factory for ConfigKey. It exists so a key's type comes from the method name rather than being typed by hand:
    // with ConfigKey.Double(...) there is no way to accidentally create a number key whose default is a string.
    public static class ConfigKey
    {
        public static ConfigKey<bool> Bool(string name, bool value, string description = null, string group = null)
            => new ConfigKey<bool>(name, value, ConfigValueType.Bool, description, group);

        public static ConfigKey<long> Long(string name, long value, string description = null, string group = null)
            => new ConfigKey<long>(name, value, ConfigValueType.Number, description, group);

        public static ConfigKey<double> Double(string name, double value, string description = null, string group = null)
            => new ConfigKey<double>(name, value, ConfigValueType.Number, description, group);

        public static ConfigKey<string> String(string name, string value, string description = null, string group = null)
            => new ConfigKey<string>(name, value ?? string.Empty, ConfigValueType.String, description, group);

        // Identical to String for reading (still GetString), different only in the template: declaring the JSON
        // valueType lets the console check the syntax for you instead of letting an editor paste in a broken string.
        public static ConfigKey<string> Json(string name, string value, string description = null, string group = null)
            => new ConfigKey<string>(name, value ?? "{}", ConfigValueType.Json, description, group);
    }
}
