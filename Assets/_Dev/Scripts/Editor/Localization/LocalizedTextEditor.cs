using System.Collections.Generic;
using System.Linq;
using Game.Game.Data;
using Game.Game.Views.Localization;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Localization
{
    // Key field with a picker built from the source table, a missing-key warning and the value in
    // every shipped language.
    [CustomEditor(typeof(LocalizedText))]
    [CanEditMultipleObjects]
    public sealed class LocalizedTextEditor : UnityEditor.Editor
    {
        LocalizationSettingsSO settings;
        Dictionary<string, Dictionary<string, string>> tables;

        void OnEnable() => Reload();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var key = serializedObject.FindProperty("key");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(key);
                if (GUILayout.Button("Pick", EditorStyles.miniButton, GUILayout.Width(40f))) ShowPicker(key);
            }

            serializedObject.ApplyModifiedProperties();

            if (settings == null)
            {
                EditorGUILayout.HelpBox("No LocalizationSettings asset in the project.", MessageType.Info);
                return;
            }

            if (key.hasMultipleDifferentValues) return;
            if (string.IsNullOrEmpty(key.stringValue))
            {
                EditorGUILayout.HelpBox("No key: only the font follows the language.", MessageType.None);
                return;
            }

            var source = Table(settings.SourceLanguage);
            if (!source.ContainsKey(key.stringValue))
                EditorGUILayout.HelpBox($"\"{key.stringValue}\" is not in the {settings.SourceLanguage} table; add it to the sheet and sync.", MessageType.Error);

            foreach (var entry in settings.Languages)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Code)) continue;
                Table(entry.Code).TryGetValue(key.stringValue, out var value);
                EditorGUILayout.LabelField(entry.Code, string.IsNullOrEmpty(value) ? "—" : value, EditorStyles.wordWrappedLabel);
            }

            if (GUILayout.Button("Reload Tables", EditorStyles.miniButton)) Reload();
        }

        void Reload()
        {
            settings = LocalizationEditorAssets.FindSettings();
            tables = new Dictionary<string, Dictionary<string, string>>();
        }

        Dictionary<string, string> Table(string language)
        {
            if (!tables.TryGetValue(language, out var table))
                tables[language] = table = LocalizationEditorAssets.ReadTable(settings, language);
            return table;
        }

        // "popup_settings.title" shows as popup_settings > title.
        void ShowPicker(SerializedProperty key)
        {
            if (settings == null) return;
            var menu = new GenericMenu();
            var path = key.propertyPath;
            foreach (var pair in Table(settings.SourceLanguage).OrderBy(p => p.Key))
            {
                var picked = pair.Key;
                menu.AddItem(new GUIContent(picked.Replace('.', '/')), picked == key.stringValue, () =>
                {
                    serializedObject.Update();
                    serializedObject.FindProperty(path).stringValue = picked;
                    serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
    }
}
