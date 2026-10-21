using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Localization
{
    // Copies a font's material presets (<Font>-<Style>.mat) onto another font's atlas. SDF thickness
    // values are fractions of the atlas padding, so they are rescaled by the padding/point-size
    // ratio of the two fonts to keep the same on-screen outline and shadow.
    public static class FontMaterialCloneRules
    {
        public const string DefaultStyle = "Default";
        const float MaxRatioDrift = 0.25f;

        // Property, min, max of every SDF thickness value the ratio applies to.
        static readonly (string name, float min, float max)[] Scaled =
        {
            ("_FaceDilate", -1f, 1f), ("_OutlineWidth", 0f, 1f), ("_OutlineSoftness", 0f, 1f),
            ("_UnderlayOffsetX", -1f, 1f), ("_UnderlayOffsetY", -1f, 1f), ("_UnderlayDilate", -1f, 1f),
            ("_UnderlaySoftness", 0f, 1f), ("_GlowOffset", -1f, 1f), ("_GlowInner", 0f, 1f), ("_GlowOuter", 0f, 1f),
        };

        public struct Result
        {
            public Material Source;
            public string TargetPath;
            public bool Created;
            public List<string> Warnings;
        }

        // "LilitaOne-Regular SDF" -> "LilitaOne-Regular": the prefix every preset of the font carries.
        public static string BaseName(TMP_FontAsset font)
        {
            var name = font.name;
            return name.EndsWith(" SDF") ? name.Substring(0, name.Length - 4) : name;
        }

        // On-screen size of one SDF unit relative to the source font; 1 when both fonts were baked alike.
        public static float Ratio(TMP_FontAsset source, TMP_FontAsset target)
        {
            float Unit(TMP_FontAsset font) => font.atlasPadding / Mathf.Max(1f, font.faceInfo.pointSize);
            var targetUnit = Unit(target);
            return targetUnit > 0f ? Unit(source) / targetUnit : 1f;
        }

        public static bool RatioDrifts(float ratio) => Mathf.Abs(ratio - 1f) > MaxRatioDrift;

        // The font's default material first, then every <Base>-<Style>.mat beside the font asset.
        public static List<(Material material, string style)> Presets(TMP_FontAsset source)
        {
            var presets = new List<(Material, string)>();
            if (source == null) return presets;
            if (source.material != null) presets.Add((source.material, DefaultStyle));

            var prefix = BaseName(source) + "-";
            var folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(source))?.Replace('\\', '/');
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = Path.GetFileNameWithoutExtension(path);
                if (!name.StartsWith(prefix)) continue;
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null) presets.Add((material, name.Substring(prefix.Length)));
            }

            presets.Sort((a, b) => a.Item2 == DefaultStyle ? -1 : b.Item2 == DefaultStyle ? 1 : string.CompareOrdinal(a.Item2, b.Item2));
            return presets;
        }

        // tag keeps presets cloned from two source fonts apart ("Outline" exists for both).
        public static string TargetPath(TMP_FontAsset target, string style, string tag)
        {
            var folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(target))?.Replace('\\', '/');
            var middle = string.IsNullOrWhiteSpace(tag) ? string.Empty : tag.Trim() + "-";
            return $"{folder}/{BaseName(target)}-{middle}{style}.mat";
        }

        // An existing target is overwritten in place so its GUID, and every reference to it, survive.
        public static Result Clone(Material source, string style, string tag, TMP_FontAsset sourceFont, TMP_FontAsset target,
            float outlineScale, float faceDilateOffset)
        {
            var result = new Result { Source = source, TargetPath = TargetPath(target, style, tag), Warnings = new List<string>() };
            var ratio = Ratio(sourceFont, target);

            var material = AssetDatabase.LoadAssetAtPath<Material>(result.TargetPath);
            result.Created = material == null;
            if (material == null) material = new Material(source);
            else
            {
                material.shader = source.shader;
                material.CopyPropertiesFromMaterial(source);
            }

            material.shaderKeywords = source.shaderKeywords;
            material.name = Path.GetFileNameWithoutExtension(result.TargetPath);
            BindAtlas(material, target);

            foreach (var (name, min, max) in Scaled)
            {
                if (!material.HasProperty(name)) continue;
                var value = material.GetFloat(name) * ratio;
                if (name == "_OutlineWidth" || name == "_OutlineSoftness") value *= outlineScale;
                if (name == "_FaceDilate") value += faceDilateOffset;
                var clamped = Mathf.Clamp(value, min, max);
                if (!Mathf.Approximately(clamped, value))
                    result.Warnings.Add($"{name} {value:0.###} clamped to {clamped:0.###}");
                material.SetFloat(name, clamped);
            }

            ShaderUtilities.UpdateShaderRatios(material);
            if (result.Created) AssetDatabase.CreateAsset(material, result.TargetPath);
            else EditorUtility.SetDirty(material);
            return result;
        }

        static void BindAtlas(Material material, TMP_FontAsset target)
        {
            material.SetTexture(ShaderUtilities.ID_MainTex, target.atlasTexture);
            material.SetFloat(ShaderUtilities.ID_TextureWidth, target.atlasWidth);
            material.SetFloat(ShaderUtilities.ID_TextureHeight, target.atlasHeight);
            material.SetFloat(ShaderUtilities.ID_GradientScale, target.atlasPadding + 1);
        }
    }
}
