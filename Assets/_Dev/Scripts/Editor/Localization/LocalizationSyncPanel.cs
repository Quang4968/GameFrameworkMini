using System.Collections.Generic;
using System.IO;
using Game.Game.Data;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Editor.Localization
{
    // The Sheet section of the settings inspector, shared by the asset's own inspector and the
    // Localization window. Pulls the public CSV export (sheet shared "Anyone with the link: Viewer").
    public static class LocalizationSyncPanel
    {
        static UnityWebRequest request;
        static List<string> report = new List<string>();

        public static void Draw(LocalizationSettingsSO settings)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Google Sheet", EditorStyles.boldLabel);

            var hasSheet = !string.IsNullOrWhiteSpace(settings.SheetId);
            EditorGUILayout.LabelField(hasSheet ? LocalizationSync.ExportUrl(settings) : "Set the Sheet ID above to sync.",
                EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUI.DisabledScope(request != null))
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!hasSheet))
                    if (GUILayout.Button(request != null ? "Downloading..." : "Sync From Google Sheet", GUILayout.Height(26f)))
                        Download(settings);
                if (GUILayout.Button("Import CSV...", GUILayout.Height(26f), GUILayout.Width(110f))) ImportFile(settings);
            }

            foreach (var line in report) EditorGUILayout.HelpBox(line, MessageType.None);
        }

        static void Download(LocalizationSettingsSO settings)
        {
            report = new List<string> { "Downloading..." };
            request = UnityWebRequest.Get(LocalizationSync.ExportUrl(settings));
            request.SendWebRequest().completed += _ => OnDownloaded(settings);
        }

        static void OnDownloaded(LocalizationSettingsSO settings)
        {
            if (request == null) return;

            // A private sheet answers with the Google sign-in page instead of CSV.
            var text = request.downloadHandler?.text ?? string.Empty;
            if (request.result != UnityWebRequest.Result.Success)
                report = new List<string> { $"Download failed: {request.error}" };
            else if (text.TrimStart().StartsWith("<"))
                report = new List<string> { "Got a web page instead of CSV: share the sheet as \"Anyone with the link: Viewer\" and check the gid." };
            else if (settings != null)
                report = LocalizationSync.Apply(settings, text);

            request.Dispose();
            request = null;
            InternalEditorUtility.RepaintAllViews();
        }

        static void ImportFile(LocalizationSettingsSO settings)
        {
            var path = EditorUtility.OpenFilePanel("Localization CSV", "", "csv");
            if (string.IsNullOrEmpty(path)) return;
            report = LocalizationSync.Apply(settings, File.ReadAllText(path));
        }
    }
}
