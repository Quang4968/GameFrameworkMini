using System;
using System.Collections.Generic;
using Game.Core.GameLogic.Localization;
using Game.Core.Services;
using Game.Game.Data;
using TMPro;
using UnityEngine;

namespace Game.Game.Services
{
    // Loads every shipped table and remembers the player's language. First launch follows the
    // device language when a shipped language claims it, otherwise the source language.
    public sealed class PlayerPrefsLocalizationService : ILocalizationService
    {
        public const string PrefsKey = "Game.Language";

        readonly LocalizationSettingsSO settings;
        readonly LocalizationTable table;

#if UNITY_EDITOR
        // Editor-only stand-in for the device language, set from the Debug tab of Game/Localization so a
        // first launch in another language can be tested without changing the OS language.
        public static SystemLanguage? DebugDeviceLanguage;

        public static SystemLanguage DeviceLanguage => DebugDeviceLanguage ?? Application.systemLanguage;
#else
        public static SystemLanguage DeviceLanguage => Application.systemLanguage;
#endif

        public PlayerPrefsLocalizationService(LocalizationSettingsSO settings)
        {
            this.settings = settings;
            table = new LocalizationTable(settings != null ? settings.SourceLanguage : string.Empty);
            if (settings == null) return;

            foreach (var entry in settings.Languages)
                if (entry != null && !string.IsNullOrEmpty(entry.Code) && entry.Table != null)
                    table.SetTable(entry.Code, LocalizationTableFile.Read(entry.Table));

            table.TrySetCurrent(StartLanguage(settings));
        }

        public event Action<string> LanguageChanged;

        public string CurrentLanguage => table.Current;

        public IReadOnlyList<string> Languages => table.Languages;

        public string Get(string key) => table.Get(key);

        public string Format(string key, params object[] args) => table.Format(key, args);

        public bool Has(string key) => table.Has(key);

        public string DisplayName(string language)
        {
            var entry = settings != null ? settings.Find(language) : null;
            return entry != null ? entry.DisplayName : language;
        }

        // The source language's profile names the label's slot and style; the current language's
        // profile draws that slot and style. False keeps the authored pair (no profile, unknown font).
        public bool TryResolveFont(TMP_FontAsset authoredFont, Material authoredMaterial,
            out TMP_FontAsset font, out Material material)
        {
            font = authoredFont;
            material = authoredMaterial;
            var source = FontsOf(table.SourceLanguage);
            var current = FontsOf(table.Current);
            if (source == null || current == null || source == current) return false;

            var sourceSlot = source.FindByFont(authoredFont);
            var slot = sourceSlot != null ? current.Find(sourceSlot.Id) : null;
            if (slot == null || slot.Font == null) return false;

            font = slot.Font;
            material = slot.MaterialFor(sourceSlot.StyleOf(authoredMaterial) ?? FontSlot.DefaultStyle);
            return true;
        }

        public void SetLanguage(string language)
        {
            if (!table.TrySetCurrent(language)) return;

            PlayerPrefs.SetString(PrefsKey, language);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke(language);
        }

        // The saved choice when it has a table, else the language claiming the device language, else the source.
        public static string StartLanguage(LocalizationSettingsSO settings)
        {
            if (settings == null) return string.Empty;
            var saved = settings.Find(PlayerPrefs.GetString(PrefsKey, string.Empty));
            if (saved != null && saved.Table != null) return saved.Code;
            return settings.LanguageForDevice(DeviceLanguage) ?? settings.SourceLanguage;
        }

        LanguageFontProfileSO FontsOf(string language)
        {
            var entry = settings != null ? settings.Find(language) : null;
            return entry != null ? entry.Fonts : null;
        }
    }
}
