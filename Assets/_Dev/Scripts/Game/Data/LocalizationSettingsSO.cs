using UnityEngine;

namespace Game.Game.Data
{
    // Every language the game ships plus where the sheet lives. Splash, Boot, Home and MainGame must
    // point at the SAME asset or the language flips between scenes.
    [CreateAssetMenu(menuName = "Game/Localization Settings", fileName = "LocalizationSettings")]
    public sealed class LocalizationSettingsSO : ScriptableObject
    {
        [Tooltip("Language the UI is authored in and every missing translation falls back to; must match a sheet column.")]
        [SerializeField] string sourceLanguage = "en";

        [Tooltip("Shipped languages in selector order. Empty: every LocalizedText keeps its authored text.")]
        [SerializeField] LanguageEntry[] languages = new LanguageEntry[0];

        [Tooltip("Smallest auto-size font of a localized label, as a fraction of its authored size; longer translations shrink down to it, then overflow.")]
        [SerializeField, Range(0.3f, 1f)] float minFontScale = 0.6f;

        [Tooltip("Google Sheet ID (the part between /d/ and /edit in its URL). Editor sync only; the sheet must be shared \"Anyone with the link: Viewer\".")]
        [SerializeField] string sheetId;

        [Tooltip("gid of the sheet tab (the number after gid= in its URL). Editor sync only.")]
        [SerializeField] string sheetGid = "0";

        [Tooltip("Project folder the sync writes <code>.json tables into.")]
        [SerializeField] string tableFolder = "Assets/_Dev/Data/Localization";

        public string SourceLanguage => sourceLanguage;
        public LanguageEntry[] Languages => languages ?? new LanguageEntry[0];
        public float MinFontScale => minFontScale;
        public string SheetId => sheetId;
        public string SheetGid => sheetGid;
        public string TableFolder => tableFolder;

        public LanguageEntry Find(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            foreach (var entry in Languages)
                if (entry != null && entry.Code == code) return entry;
            return null;
        }

        // Null when no shipped language claims the device language.
        public string LanguageForDevice(SystemLanguage device)
        {
            foreach (var entry in Languages)
                if (entry != null && entry.Table != null && entry.MatchesDevice(device)) return entry.Code;
            return null;
        }
    }
}
