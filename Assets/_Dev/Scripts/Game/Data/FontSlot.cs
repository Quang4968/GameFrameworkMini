using System;
using TMPro;
using UnityEngine;

namespace Game.Game.Data
{
    // One font role of the UI ("Main", "Display"): the font a language draws it with and that
    // font's presets by style. Profiles match slots by id, never by font.
    [Serializable]
    public sealed class FontSlot
    {
        public const string DefaultStyle = "Default";

        [Tooltip("Role shared by every language's profile, e.g. \"Main\". A slot missing in a language keeps the source font.")]
        [SerializeField] string id;

        [Tooltip("Font this language draws the role with. Empty: the slot is ignored.")]
        [SerializeField] TMP_FontAsset font;

        [Tooltip("Presets of this font by style; build them with the Fonts tab of Game/Localization.")]
        [SerializeField] FontStyleMaterial[] styles = new FontStyleMaterial[0];

        public FontSlot(string id, TMP_FontAsset font, FontStyleMaterial[] styles)
        {
            this.id = id;
            this.font = font;
            this.styles = styles;
        }

        public string Id => id;
        public TMP_FontAsset Font => font;
        public FontStyleMaterial[] Styles => styles ?? new FontStyleMaterial[0];

        // The style a material draws, or null when it is not one of this slot's presets.
        public string StyleOf(Material material)
        {
            if (material == null) return null;
            if (font != null && material == font.material) return DefaultStyle;
            foreach (var entry in Styles)
                if (entry != null && entry.Material == material) return entry.Style;
            return null;
        }

        // Falls back to the font's default material when the style has no preset here.
        public Material MaterialFor(string style)
        {
            foreach (var entry in Styles)
                if (entry != null && entry.Style == style && entry.Material != null) return entry.Material;
            return font != null ? font.material : null;
        }
    }
}
