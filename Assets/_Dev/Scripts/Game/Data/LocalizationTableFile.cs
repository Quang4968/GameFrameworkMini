using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Game.Data
{
    // The synced table format, {"entries":[{"k":"settings.title","v":"Settings"}]}: one file per
    // sheet language column, read at runtime and written by the editor sync.
    public static class LocalizationTableFile
    {
        public static List<KeyValuePair<string, string>> Read(TextAsset file)
        {
            var entries = new List<KeyValuePair<string, string>>();
            if (file == null) return entries;

            var parsed = JsonUtility.FromJson<TableJson>(file.text);
            if (parsed?.entries == null)
            {
                Debug.LogWarning($"[{nameof(LocalizationTableFile)}] {file.name} has no \"entries\" array; skipped.");
                return entries;
            }

            foreach (var entry in parsed.entries)
                if (entry != null) entries.Add(new KeyValuePair<string, string>(entry.k, entry.v));
            return entries;
        }

        public static string Write(IEnumerable<KeyValuePair<string, string>> entries)
        {
            var list = new List<EntryJson>();
            foreach (var pair in entries) list.Add(new EntryJson { k = pair.Key, v = pair.Value });
            return JsonUtility.ToJson(new TableJson { entries = list.ToArray() }, true);
        }

        [Serializable]
        sealed class TableJson
        {
            public EntryJson[] entries;
        }

        [Serializable]
        sealed class EntryJson
        {
            public string k;
            public string v;
        }
    }
}
