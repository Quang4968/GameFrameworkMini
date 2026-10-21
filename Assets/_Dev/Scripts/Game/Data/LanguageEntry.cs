using System;
using UnityEngine;

namespace Game.Game.Data
{
    // One language the game ships: its sheet column code, the table synced from that column and
    // the fonts it draws with.
    [Serializable]
    public sealed class LanguageEntry
    {
        [Tooltip("Column header in the localization sheet, e.g. \"en\" or \"pt-BR\". Empty: the entry is ignored.")]
        [SerializeField] string code;

        [Tooltip("Name shown in the Settings language selector, written in that language.")]
        [SerializeField] string displayName;

        [Tooltip("Key -> value table written by Game/Localization/Sync From Google Sheet. Empty: the language is not selectable.")]
        [SerializeField] TextAsset table;

        [Tooltip("Font and material swaps for this language. Empty: labels keep the fonts they were authored with.")]
        [SerializeField] LanguageFontProfileSO fonts;

        [Tooltip("Device languages that open the game in this language on first launch.")]
        [SerializeField] SystemLanguage[] deviceLanguages = new SystemLanguage[0];

        public string Code => code;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? code : displayName;
        public TextAsset Table => table;
        public LanguageFontProfileSO Fonts => fonts;
        public SystemLanguage[] DeviceLanguages => deviceLanguages ?? new SystemLanguage[0];

        public bool MatchesDevice(SystemLanguage language) =>
            deviceLanguages != null && Array.IndexOf(deviceLanguages, language) >= 0;
    }
}
