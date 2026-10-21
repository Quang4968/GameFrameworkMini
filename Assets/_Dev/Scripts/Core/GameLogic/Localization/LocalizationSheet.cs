using System;
using System.Collections.Generic;

namespace Game.Core.GameLogic.Localization
{
    // The parsed sheet: one ordered key -> value table per language column and the issue report.
    public sealed class LocalizationSheet
    {
        readonly List<string> languages = new List<string>();
        readonly Dictionary<string, Dictionary<string, string>> tables =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        readonly Dictionary<string, List<string>> order =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);

        public IReadOnlyList<string> Languages => languages;

        public List<string> Issues { get; } = new List<string>();

        // Null for a language the sheet has no column for.
        public IReadOnlyDictionary<string, string> Table(string language) =>
            language != null && tables.TryGetValue(language, out var table) ? table : null;

        // Rows in sheet order, so the written table diffs cleanly against the previous sync.
        public List<KeyValuePair<string, string>> Entries(string language)
        {
            var entries = new List<KeyValuePair<string, string>>();
            if (language == null || !order.TryGetValue(language, out var keys)) return entries;
            var table = tables[language];
            foreach (var key in keys) entries.Add(new KeyValuePair<string, string>(key, table[key]));
            return entries;
        }

        internal void AddLanguage(string language)
        {
            if (tables.ContainsKey(language)) return;
            languages.Add(language);
            tables[language] = new Dictionary<string, string>(StringComparer.Ordinal);
            order[language] = new List<string>();
        }

        internal void Add(string language, string key, string value)
        {
            if (!tables.TryGetValue(language, out var table) || table.ContainsKey(key)) return;
            table[key] = value ?? string.Empty;
            order[language].Add(key);
        }
    }
}
