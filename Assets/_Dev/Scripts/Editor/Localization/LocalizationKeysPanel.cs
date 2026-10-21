using System.Collections.Generic;
using System.IO;
using Game.Game.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.Localization
{
    // Keys tab: puts LocalizedText on new UI and exports its keys for the sheet, and validates every
    // key, scene and font profile against the source language.
    public sealed class LocalizationKeysPanel
    {
        readonly List<string> report = new List<string>();
        Vector2 scroll;

        public void Draw(LocalizationSettingsSO settings)
        {
            EditorGUILayout.HelpBox("Extract adds a LocalizedText to every TMP label without one and keys the static ones " +
                "\"<asset>.<object>\". Paste the exported key,note,en rows into the sheet, then sync.", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Extract From Prefabs", GUILayout.Height(26f))) Extract(false);
                if (GUILayout.Button("Extract From Open Scene", GUILayout.Height(26f))) Extract(true);
            }

            if (GUILayout.Button(new GUIContent("Fit Localized Labels", "Turns on auto size for every localized label with words in the prefabs and the open scene: from its authored size down to Min Font Scale."), GUILayout.Height(26f)))
            {
                report.Clear();
                var prefabs = LocalizedTextFitter.FitAllPrefabs(settings.MinFontScale);
                var scene = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() ? LocalizedTextFitter.FitOpenScene(settings.MinFontScale) : 0;
                report.Add($"Auto size turned on for {prefabs} prefab label(s) and {scene} label(s) in the open scene.");
            }

            if (GUILayout.Button("Validate", GUILayout.Height(26f)))
            {
                report.Clear();
                var problems = LocalizationKeyValidator.Validate(settings);
                report.Add(problems.Count == 0 ? "No problems." : $"{problems.Count} problem(s):");
                report.AddRange(problems);
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var line in report) EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();
        }

        void Extract(bool openScene)
        {
            var rows = new List<LocalizationKeyExtractor.Row>();
            var review = new List<string>();
            if (openScene)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                LocalizationKeyExtractor.ExtractOpenScene(rows, review, apply: true);
            }
            else LocalizationKeyExtractor.ExtractAllPrefabs(rows, review);

            report.Clear();
            report.Add($"{rows.Count} new key(s); {review.Count} code-written label(s) left without a key:");
            report.AddRange(review);
            if (rows.Count == 0) return;

            var file = EditorUtility.SaveFilePanel("Extracted keys", "", "extracted-keys", "csv");
            if (!string.IsNullOrEmpty(file)) File.WriteAllText(file, LocalizationKeyExtractor.ToCsv(rows));
        }
    }
}
