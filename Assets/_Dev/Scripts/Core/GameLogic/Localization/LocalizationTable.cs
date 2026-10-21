using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game.Core.GameLogic.Localization
{
    // Every key -> value table with the lookup chain current -> source -> key. The key itself is the
    // last resort (never an empty string) so a missing translation is visible on screen.
    public sealed class LocalizationTable
    {
        readonly Dictionary<string, Dictionary<string, string>> tables =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        readonly List<string> languages = new List<string>();

        public LocalizationTable(string sourceLanguage)
        {
            SourceLanguage = sourceLanguage ?? string.Empty;
            Current = SourceLanguage;
        }

        public string SourceLanguage { get; }

        public string Current { get; private set; }

        public IReadOnlyList<string> Languages => languages;

        // Empty values are dropped: an empty sheet cell means "not translated yet" and must fall
        // back to the source language rather than blank the label.
        public void SetTable(string language, IEnumerable<KeyValuePair<string, string>> entries)
        {
            if (string.IsNullOrEmpty(language)) return;

            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (entries != null)
            {
                foreach (var pair in entries)
                {
                    if (string.IsNullOrEmpty(pair.Key) || string.IsNullOrEmpty(pair.Value)) continue;
                    map[pair.Key] = pair.Value;
                }
            }

            if (!tables.ContainsKey(language)) languages.Add(language);
            tables[language] = map;
        }

        public bool HasLanguage(string language) => language != null && tables.ContainsKey(language);

        // True only when the language actually changed, so the service raises its event once.
        public bool TrySetCurrent(string language)
        {
            if (!HasLanguage(language) || language == Current) return false;
            Current = language;
            return true;
        }

        public bool Has(string key) => TryGet(key, out _);

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            return TryGet(key, out var value) ? value : key;
        }

        public string Format(string key, params object[] args)
        {
            var value = Get(key);
            if (args == null || args.Length == 0) return value;

            try
            {
                return string.Format(CultureInfo.InvariantCulture, value, args);
            }
            catch (FormatException)
            {
                // A mistyped placeholder in a translation must not break the popup showing it.
                return value;
            }
        }

        bool TryGet(string key, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(key)) return false;
            if (tables.TryGetValue(Current, out var current) && current.TryGetValue(key, out value)) return true;
            return tables.TryGetValue(SourceLanguage, out var source) && source.TryGetValue(key, out value);
        }
    }
}
