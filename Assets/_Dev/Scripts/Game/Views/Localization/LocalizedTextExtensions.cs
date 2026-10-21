using TMPro;

namespace Game.Game.Views.Localization
{
    public static class LocalizedTextExtensions
    {
        // Shows the key's value through the label's LocalizedText (added when missing), so code-written
        // text also follows a language switch. A value that is not a key yet shows as written.
        public static void Localize(this TMP_Text label, string key, params object[] args)
        {
            if (label == null) return;
            var localized = label.GetComponent<LocalizedText>();
            if (localized == null) localized = label.gameObject.AddComponent<LocalizedText>();
            localized.SetKey(key, args);
        }
    }
}
