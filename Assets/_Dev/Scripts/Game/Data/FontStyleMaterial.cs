using System;
using UnityEngine;

namespace Game.Game.Data
{
    // One material preset of a slot, named by style so every language's profile lines up with the
    // source language's ("Outline-Orange-Shadow" in en and pt-BR draw the same look).
    [Serializable]
    public sealed class FontStyleMaterial
    {
        [Tooltip("Style name shared by every language's profile, e.g. \"Outline-Orange\"; \"Default\" is the font's own material.")]
        [SerializeField] string style;

        [Tooltip("Preset on this slot's font atlas. Empty: labels of this style fall back to the font's default material.")]
        [SerializeField] Material material;

        public FontStyleMaterial(string style, Material material)
        {
            this.style = style;
            this.material = material;
        }

        public string Style => style;
        public Material Material => material;
    }
}
