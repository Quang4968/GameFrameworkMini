using Game.Game.Services;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Localization
{
    // Keeps the simulated device language across domain reloads. Kept apart from the window and its
    // panels: Unity builds those while deserializing, where EditorPrefs may not be called.
    [InitializeOnLoad]
    public static class LocalizationDebugPrefs
    {
        const string DevicePrefsKey = "Game.Localization.DebugDeviceLanguage";

        static LocalizationDebugPrefs() => EditorApplication.delayCall += () => PlayerPrefsLocalizationService.DebugDeviceLanguage = Load();

        public static void SetSimulated(SystemLanguage? language)
        {
            PlayerPrefsLocalizationService.DebugDeviceLanguage = language;
            if (language.HasValue) EditorPrefs.SetInt(DevicePrefsKey, (int)language.Value);
            else EditorPrefs.DeleteKey(DevicePrefsKey);
        }

        static SystemLanguage? Load()
        {
            var value = EditorPrefs.GetInt(DevicePrefsKey, -1);
            return value < 0 ? (SystemLanguage?)null : (SystemLanguage)value;
        }
    }
}
