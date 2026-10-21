using System.Linq;
using Game.Game.Data;
using Game.Game.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.Localization
{
    // Debug tab: tests the first launch in any language without touching the OS language.
    public sealed class LocalizationDebugPanel
    {
        const string SplashScene = "Assets/_Dev/Scenes/Splash.unity";

        public void Draw(LocalizationSettingsSO settings)
        {
            DrawDevice(settings);
            EditorGUILayout.Space();

            var saved = PlayerPrefs.GetString(PlayerPrefsLocalizationService.PrefsKey, string.Empty);
            EditorGUILayout.LabelField("Saved choice", string.IsNullOrEmpty(saved) ? "none (first launch)" : saved);
            var start = PlayerPrefsLocalizationService.StartLanguage(settings);
            EditorGUILayout.LabelField("Next launch opens in", $"{start}  ({DisplayName(settings, start)})", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button(new GUIContent("Reset To First Launch", "Deletes the saved language so the next launch follows the device language."), GUILayout.Height(26f)))
                    ClearSaved();
                if (GUILayout.Button(new GUIContent("Reset + Play From Splash", "Deletes the saved language and enters Play mode from the Splash scene."), GUILayout.Height(26f)))
                    PlayFirstLaunch();
            }

            if (Application.isPlaying) DrawLive(settings);
        }

        // Only the shipped languages are offered; picking one simulates its first device language.
        static void DrawDevice(LocalizationSettingsSO settings)
        {
            EditorGUILayout.LabelField("Device language", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Real (OS)", Application.systemLanguage.ToString());

            var entries = settings.Languages.Where(e => e != null && e.Table != null && e.DeviceLanguages.Length > 0).ToArray();
            var simulated = PlayerPrefsLocalizationService.DebugDeviceLanguage;
            var index = simulated.HasValue ? System.Array.FindIndex(entries, e => e.MatchesDevice(simulated.Value)) : -1;

            EditorGUI.BeginChangeCheck();
            var on = EditorGUILayout.Toggle(new GUIContent("Simulate", "Editor only: the game reads this instead of the OS language. Builds always use the real device language."), simulated.HasValue);
            using (new EditorGUI.DisabledScope(!on || entries.Length == 0))
                index = EditorGUILayout.Popup("Simulated", Mathf.Max(index, 0), entries.Select(e => $"{e.DisplayName} ({e.Code})").ToArray());
            if (EditorGUI.EndChangeCheck())
                LocalizationDebugPrefs.SetSimulated(on && entries.Length > 0 ? entries[index].DeviceLanguages[0] : (SystemLanguage?)null);

            var unreachable = settings.Languages.Where(e => e != null && !string.IsNullOrEmpty(e.Code) && (e.Table == null || e.DeviceLanguages.Length == 0)).Select(e => e.Code).ToArray();
            if (unreachable.Length > 0)
                EditorGUILayout.HelpBox($"No table or device language, so a first launch never opens in: {string.Join(", ", unreachable)}.", MessageType.None);
        }

        static void DrawLive(LocalizationSettingsSO settings)
        {
            var live = LocalizationProvider.Current;
            if (live == null) return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Playing in", live.CurrentLanguage, EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
                foreach (var language in live.Languages)
                    using (new EditorGUI.DisabledScope(language == live.CurrentLanguage))
                        if (GUILayout.Button(DisplayName(settings, language))) live.SetLanguage(language);
        }

        static void ClearSaved()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsLocalizationService.PrefsKey);
            PlayerPrefs.Save();
        }

        static void PlayFirstLaunch()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ClearSaved();
            EditorSceneManager.OpenScene(SplashScene);
            EditorApplication.isPlaying = true;
        }

        static string DisplayName(LocalizationSettingsSO settings, string language)
        {
            var entry = settings.Find(language);
            return entry != null ? entry.DisplayName : language;
        }
    }
}
