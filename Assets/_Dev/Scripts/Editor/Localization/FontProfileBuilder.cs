using System.Collections.Generic;
using Game.Game.Data;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Localization
{
    // Fills language font profiles: the source language's slots come from each font's presets on
    // disk, every other language's slot is the source slot cloned onto that language's font.
    public static class FontProfileBuilder
    {
        public static FontSlot SourceSlot(string id, TMP_FontAsset font)
        {
            var styles = new List<FontStyleMaterial>();
            foreach (var (material, style) in FontMaterialCloneRules.Presets(font)) styles.Add(new FontStyleMaterial(style, material));
            return new FontSlot(id, font, styles.ToArray());
        }

        // Re-running overwrites the same material files, so references to them survive.
        public static FontSlot TargetSlot(FontSlot source, TMP_FontAsset target, string tag, float outlineScale,
            float faceDilateOffset, List<string> report)
        {
            var styles = new List<FontStyleMaterial>();
            foreach (var entry in source.Styles)
            {
                if (entry == null || entry.Material == null) continue;
                var result = FontMaterialCloneRules.Clone(entry.Material, entry.Style, tag, source.Font, target, outlineScale, faceDilateOffset);
                foreach (var warning in result.Warnings) report.Add($"{source.Id}/{entry.Style}: {warning}");
                styles.Add(new FontStyleMaterial(entry.Style, AssetDatabase.LoadAssetAtPath<Material>(result.TargetPath)));
            }
            AssetDatabase.SaveAssets();
            report.Add($"{source.Id}: {styles.Count} style(s) on {target.name}.");
            return new FontSlot(source.Id, target, styles.ToArray());
        }

        // Replaces the slot with the same id, or appends it.
        public static void SetSlot(LanguageFontProfileSO profile, FontSlot slot)
        {
            var serialized = new SerializedObject(profile);
            var slots = serialized.FindProperty("slots");
            var index = 0;
            while (index < slots.arraySize && slots.GetArrayElementAtIndex(index).FindPropertyRelative("id").stringValue != slot.Id) index++;
            if (index == slots.arraySize) slots.InsertArrayElementAtIndex(index);

            var element = slots.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("id").stringValue = slot.Id;
            element.FindPropertyRelative("font").objectReferenceValue = slot.Font;
            var styles = element.FindPropertyRelative("styles");
            styles.arraySize = slot.Styles.Length;
            for (var i = 0; i < slot.Styles.Length; i++)
            {
                var style = styles.GetArrayElementAtIndex(i);
                style.FindPropertyRelative("style").stringValue = slot.Styles[i].Style;
                style.FindPropertyRelative("material").objectReferenceValue = slot.Styles[i].Material;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(profile);
        }

        // The language's profile, created beside the settings asset and linked when missing.
        public static LanguageFontProfileSO ProfileFor(LocalizationSettingsSO settings, string language)
        {
            var entry = settings.Find(language);
            if (entry == null) return null;
            if (entry.Fonts != null) return entry.Fonts;

            var path = $"{settings.TableFolder.TrimEnd('/')}/FontProfile_{language}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<LanguageFontProfileSO>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<LanguageFontProfileSO>();
                AssetDatabase.CreateAsset(profile, path);
            }

            var serialized = new SerializedObject(settings);
            var languages = serialized.FindProperty("languages");
            for (var i = 0; i < languages.arraySize; i++)
            {
                var element = languages.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative("code").stringValue == language)
                    element.FindPropertyRelative("fonts").objectReferenceValue = profile;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(settings);
            return profile;
        }
    }
}
