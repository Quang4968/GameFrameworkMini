using System;
using System.Collections.Generic;
using Game.Game.Data;
using UnityEditor;

namespace Game.Editor.Localization
{
    // Finds the project's localization settings and reads its synced tables for editor tools.
    public static class LocalizationEditorAssets
    {
        public static LocalizationSettingsSO FindSettings()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(LocalizationSettingsSO)))
            {
                var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettingsSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings != null) return settings;
            }
            return null;
        }

        // Empty when the language has no synced table yet.
        public static Dictionary<string, string> ReadTable(LocalizationSettingsSO settings, string language)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var entry = settings != null ? settings.Find(language) : null;
            if (entry == null || entry.Table == null) return map;
            foreach (var pair in LocalizationTableFile.Read(entry.Table))
                if (!string.IsNullOrEmpty(pair.Key)) map[pair.Key] = pair.Value;
            return map;
        }
    }
}
