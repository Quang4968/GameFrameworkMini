using System.Collections.Generic;
using System.IO;
using Game.Core.GameLogic.Localization;
using Game.Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Localization
{
    // Writes one <code>.json table per sheet language column and links new tables into the
    // settings asset. The sheet is the source of truth: a table is rewritten whole on every sync.
    public static class LocalizationSync
    {
        public static string ExportUrl(LocalizationSettingsSO settings) =>
            $"https://docs.google.com/spreadsheets/d/{settings.SheetId.Trim()}/export?format=csv&gid={settings.SheetGid.Trim()}";

        // Returns the issue report; files are still written when it has warnings.
        public static List<string> Apply(LocalizationSettingsSO settings, string csv)
        {
            var sheet = LocalizationCsv.Parse(csv, settings.SourceLanguage);
            var report = new List<string>(sheet.Issues);
            if (sheet.Languages.Count == 0) return report;

            var folder = settings.TableFolder.TrimEnd('/');
            Directory.CreateDirectory(folder);

            var written = new List<string>();
            foreach (var language in sheet.Languages)
            {
                var path = $"{folder}/{language}.json";
                File.WriteAllText(path, LocalizationTableFile.Write(sheet.Entries(language)));
                written.Add(path);
            }

            AssetDatabase.Refresh();
            foreach (var path in written) AssetDatabase.ImportAsset(path);

            LinkTables(settings, sheet.Languages, folder, report);
            report.Insert(0, $"Wrote {written.Count} table(s): {string.Join(", ", sheet.Languages)}.");
            return report;
        }

        static void LinkTables(LocalizationSettingsSO settings, IReadOnlyList<string> languages, string folder,
            List<string> report)
        {
            var serialized = new SerializedObject(settings);
            var entries = serialized.FindProperty("languages");

            foreach (var language in languages)
            {
                var table = AssetDatabase.LoadAssetAtPath<TextAsset>($"{folder}/{language}.json");
                var index = IndexOf(entries, language);
                if (index < 0)
                {
                    index = entries.arraySize;
                    entries.InsertArrayElementAtIndex(index);
                    var added = entries.GetArrayElementAtIndex(index);
                    added.FindPropertyRelative("code").stringValue = language;
                    added.FindPropertyRelative("displayName").stringValue = language;
                    added.FindPropertyRelative("fonts").objectReferenceValue = null;
                    added.FindPropertyRelative("deviceLanguages").arraySize = 0;
                    report.Add($"New language \"{language}\" added to {settings.name}: set its display name, fonts and device languages.");
                }

                entries.GetArrayElementAtIndex(index).FindPropertyRelative("table").objectReferenceValue = table;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        static int IndexOf(SerializedProperty entries, string language)
        {
            for (var i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("code").stringValue == language) return i;
            return -1;
        }
    }
}
