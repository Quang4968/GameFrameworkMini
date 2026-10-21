using TMPro;
using UnityEngine;

namespace Game.Game.Data
{
    // The fonts one language draws each UI role with. The source language's profile tells which slot
    // and style an authored label uses; the current language's profile supplies the same slot and style.
    [CreateAssetMenu(menuName = "Game/Language Font Profile", fileName = "FontProfile")]
    public sealed class LanguageFontProfileSO : ScriptableObject
    {
        [Tooltip("Font roles of this language. Ids and styles must match the source language's profile.")]
        [SerializeField] FontSlot[] slots = new FontSlot[0];

        public FontSlot[] Slots => slots ?? new FontSlot[0];

        public FontSlot Find(string id)
        {
            foreach (var slot in Slots)
                if (slot != null && slot.Id == id) return slot;
            return null;
        }

        public FontSlot FindByFont(TMP_FontAsset font)
        {
            if (font == null) return null;
            foreach (var slot in Slots)
                if (slot != null && slot.Font == font) return slot;
            return null;
        }
    }
}
