using System;
using System.Globalization;
using Game.Game.Services;
using TMPro;
using UnityEngine;

namespace Game.Game.Views.Localization
{
    // Drop on any TMP label: it shows its key's value and follows the language's font profile,
    // live on language change. Needs no injection, so it works on scene, popup and pooled objects.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [Tooltip("Key in the localization sheet. Empty: only the font follows the language (text written by code, numbers).")]
        [SerializeField] string key;

        TMP_Text label;
        TMP_FontAsset authoredFont;
        Material authoredMaterial;
        object[] args;
        bool writtenByCode;
        PlayerPrefsLocalizationService service;

        public string Key => key;

        void Awake() => Capture();

        void OnEnable()
        {
            if (LocalizationProvider.Current != null) Bind(LocalizationProvider.Current);
            else LocalizationProvider.Created += Bind;
        }

        void OnDisable()
        {
            LocalizationProvider.Created -= Bind;
            if (service != null) service.LanguageChanged -= OnLanguageChanged;
            service = null;
        }

        // Args fill the value's {0}, {1}...; they are kept so a language switch re-renders them.
        public void SetKey(string newKey, params object[] newArgs)
        {
            key = newKey;
            args = newArgs;
            writtenByCode = true;
            RefreshText();
        }

        public void SetArgs(params object[] newArgs)
        {
            args = newArgs;
            writtenByCode = true;
            RefreshText();
        }

        // For code that restyles the label (a difficulty skin): the new preset becomes the
        // authored one and is mapped through the font profile like the original.
        public void SetAuthoredMaterial(Material material)
        {
            Capture();
            if (material == null) return;
            authoredMaterial = material;
            RefreshFont();
        }

        void Capture()
        {
            if (label != null) return;
            label = GetComponent<TMP_Text>();
            authoredFont = label.font;
            authoredMaterial = label.fontSharedMaterial;
        }

        void Bind(PlayerPrefsLocalizationService created)
        {
            LocalizationProvider.Created -= Bind;
            if (service != null) service.LanguageChanged -= OnLanguageChanged;
            service = created;
            service.LanguageChanged += OnLanguageChanged;
            Refresh();
        }

        void OnLanguageChanged(string language) => Refresh();

        void Refresh()
        {
            RefreshFont();
            RefreshText();
        }

        // With localization off, authored labels keep their text and code-written ones show the
        // value they were given, formatted.
        void RefreshText()
        {
            Capture();
            if (string.IsNullOrEmpty(key))
            {
                // Code clearing a label passes an empty key; an authored font-only label is left alone.
                if (writtenByCode) label.text = string.Empty;
                return;
            }

            var active = service != null && service.Languages.Count > 0;
            if (!active && !writtenByCode) return;

            var hasArgs = args != null && args.Length > 0;
            if (active) label.text = hasArgs ? service.Format(key, args) : service.Get(key);
            else label.text = hasArgs ? FormatRaw(key, args) : key;
        }

        static string FormatRaw(string format, object[] values)
        {
            try
            {
                return string.Format(CultureInfo.InvariantCulture, format, values);
            }
            catch (FormatException)
            {
                return format;
            }
        }

        void RefreshFont()
        {
            if (service == null) return;
            Capture();

            service.TryResolveFont(authoredFont, authoredMaterial, out var font, out var material);
            label.font = font;
            label.fontSharedMaterial = material;
        }
    }
}
