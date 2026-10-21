using Game.Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Localization
{
    // The one localization window: the settings asset with its sheet sync, the per-language font
    // profiles, key extraction / validation, and first-launch language testing.
    public sealed class LocalizationWindow : EditorWindow
    {
        static readonly string[] Tabs = { "Settings & Sync", "Fonts", "Keys", "Debug" };

        // Built in OnEnable, never in field initializers: Unity constructs the window while deserializing it.
        FontProfilePanel fonts;
        LocalizationKeysPanel keys;
        LocalizationDebugPanel debug;
        LocalizationSettingsSO settings;
        UnityEditor.Editor settingsEditor;
        int tab;
        Vector2 scroll;

        [MenuItem("Game/Localization")]
        public static void Open()
        {
            GetWindow<LocalizationWindow>("Localization").minSize = new Vector2(460f, 520f);
        }

        void OnEnable()
        {
            fonts ??= new FontProfilePanel();
            keys ??= new LocalizationKeysPanel();
            debug ??= new LocalizationDebugPanel();
            if (settings == null) settings = LocalizationEditorAssets.FindSettings();
        }

        // The Debug tab shows live play state.
        void OnInspectorUpdate()
        {
            if (tab == 3) Repaint();
        }

        void OnDisable()
        {
            if (settingsEditor != null) DestroyImmediate(settingsEditor);
        }

        void OnGUI()
        {
            settings = (LocalizationSettingsSO)EditorGUILayout.ObjectField("Settings", settings, typeof(LocalizationSettingsSO), false);
            if (settings == null)
            {
                EditorGUILayout.HelpBox("Create one with Create > Game > Localization Settings.", MessageType.Info);
                return;
            }

            tab = GUILayout.Toolbar(tab, Tabs);
            EditorGUILayout.Space();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            switch (tab)
            {
                case 0:
                    UnityEditor.Editor.CreateCachedEditor(settings, null, ref settingsEditor);
                    settingsEditor.OnInspectorGUI();
                    break;
                case 1: fonts.Draw(settings); break;
                case 2: keys.Draw(settings); break;
                default: debug.Draw(settings); break;
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
