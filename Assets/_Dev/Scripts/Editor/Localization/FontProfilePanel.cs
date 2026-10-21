using System.Collections.Generic;
using System.Linq;
using Game.Game.Data;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Localization
{
    // Fonts tab: the source language's slots are read from each font's presets; another language
    // picks a font per slot and gets every style cloned onto it, written straight into its profile.
    public sealed class FontProfilePanel
    {
        readonly Dictionary<string, TMP_FontAsset> targets = new Dictionary<string, TMP_FontAsset>();
        readonly List<string> report = new List<string>();
        int languageIndex;
        float outlineScale = 1f;
        float faceDilateOffset;
        string newSlotId = "Main";
        TMP_FontAsset newSlotFont;

        public void Draw(LocalizationSettingsSO settings)
        {
            var codes = settings.Languages.Where(e => e != null && !string.IsNullOrEmpty(e.Code)).Select(e => e.Code).ToArray();
            if (codes.Length == 0)
            {
                EditorGUILayout.HelpBox("Add languages in the Settings & Sync tab first.", MessageType.Info);
                return;
            }

            languageIndex = Mathf.Clamp(EditorGUILayout.Popup("Language", languageIndex, codes), 0, codes.Length - 1);
            var language = codes[languageIndex];
            var profile = settings.Find(language)?.Fonts;
            EditorGUILayout.ObjectField("Profile", profile, typeof(LanguageFontProfileSO), false);
            EditorGUILayout.Space();

            if (language == settings.SourceLanguage) DrawSource(settings, language, profile);
            else DrawTarget(settings, language, profile, settings.Find(settings.SourceLanguage)?.Fonts);

            foreach (var line in report) EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
        }

        void DrawSource(LocalizationSettingsSO settings, string language, LanguageFontProfileSO profile)
        {
            EditorGUILayout.HelpBox("Source language: one slot per authored font; styles are read from its <Font>-<Style>.mat presets.", MessageType.None);
            foreach (var slot in profile != null ? profile.Slots : new FontSlot[0])
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(slot.Id, $"{(slot.Font != null ? slot.Font.name : "—")}  ·  {slot.Styles.Length} styles");
                    if (GUILayout.Button("Rescan", GUILayout.Width(70f)) && slot.Font != null)
                        FontProfileBuilder.SetSlot(profile, FontProfileBuilder.SourceSlot(slot.Id, slot.Font));
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                newSlotId = EditorGUILayout.TextField(newSlotId, GUILayout.Width(110f));
                newSlotFont = (TMP_FontAsset)EditorGUILayout.ObjectField(newSlotFont, typeof(TMP_FontAsset), false);
                using (new EditorGUI.DisabledScope(newSlotFont == null || string.IsNullOrWhiteSpace(newSlotId)))
                    if (GUILayout.Button("Add / Replace Slot", GUILayout.Width(130f)))
                        FontProfileBuilder.SetSlot(FontProfileBuilder.ProfileFor(settings, language), FontProfileBuilder.SourceSlot(newSlotId.Trim(), newSlotFont));
            }
        }

        void DrawTarget(LocalizationSettingsSO settings, string language, LanguageFontProfileSO profile, LanguageFontProfileSO source)
        {
            if (source == null || source.Slots.Length == 0)
            {
                EditorGUILayout.HelpBox($"Build the slots of the source language ({settings.SourceLanguage}) first.", MessageType.Info);
                return;
            }

            foreach (var slot in source.Slots)
            {
                if (!targets.ContainsKey(slot.Id)) targets[slot.Id] = profile != null ? profile.Find(slot.Id)?.Font : null;
                var done = profile != null ? profile.Find(slot.Id)?.Styles.Length ?? 0 : 0;
                targets[slot.Id] = (TMP_FontAsset)EditorGUILayout.ObjectField($"{slot.Id}  ({done}/{slot.Styles.Length})", targets[slot.Id], typeof(TMP_FontAsset), false);
                if (targets[slot.Id] != null && FontMaterialCloneRules.RatioDrifts(FontMaterialCloneRules.Ratio(slot.Font, targets[slot.Id])))
                    EditorGUILayout.HelpBox("Baked with very different padding or point size; check the result by eye.", MessageType.Warning);
            }

            outlineScale = EditorGUILayout.Slider(new GUIContent("Outline scale", "Multiplies outline width and softness on top of the padding ratio."), outlineScale, 0.25f, 2f);
            faceDilateOffset = EditorGUILayout.Slider(new GUIContent("Face dilate +", "Added to face dilate; thickens a thin target font."), faceDilateOffset, -0.5f, 0.5f);

            using (new EditorGUI.DisabledScope(source.Slots.All(s => targets[s.Id] == null)))
                if (GUILayout.Button("Create Materials + Fill Profile", GUILayout.Height(28f))) Generate(settings, language, source);
        }

        void Generate(LocalizationSettingsSO settings, string language, LanguageFontProfileSO source)
        {
            report.Clear();
            var profile = FontProfileBuilder.ProfileFor(settings, language);
            var usedTargets = new HashSet<TMP_FontAsset>();
            foreach (var slot in source.Slots)
            {
                var target = targets[slot.Id];
                if (target == null || slot.Font == null) continue;
                // A second slot cloning into the same font gets its source font's name in the file names.
                var tag = usedTargets.Add(target) ? string.Empty : FontMaterialCloneRules.BaseName(slot.Font).Split('-')[0];
                FontProfileBuilder.SetSlot(profile, FontProfileBuilder.TargetSlot(slot, target, tag, outlineScale, faceDilateOffset, report));
            }
        }
    }
}
