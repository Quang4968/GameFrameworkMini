using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Game.Data;
using Game.Game.Views.Localization;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Localization
{
    // Finds every localization key the source table lacks (such text shows its raw key in game), every
    // scene not pointing at the project's LocalizationSettings asset, and every font profile missing a
    // slot or style of the source language's profile.
    public static class LocalizationKeyValidator
    {
        static readonly string[] Scenes =
        {
            "Assets/_Dev/Scenes/Splash.unity", "Assets/_Dev/Scenes/Boot.unity",
            "Assets/_Dev/Scenes/HomeScene.unity", "Assets/_Dev/Scenes/MainGame.unity",
        };

        static readonly Regex Word = new Regex(@"\p{L}.*\p{L}", RegexOptions.Singleline);

        // Serialized string fields that hold a key rather than display text.
        static readonly HashSet<string> KeyFields = new HashSet<string>
        {
            "messageOverride", "buttonLabel", "gestureLabel", "format", "fullText", "readyCaption",
            "levelCaptionFormat", "caption", "displayName", "description", "usageMessage", "reasons", "applyLabel",
        };

        // Each problem is also logged as an error with its asset as context.
        public static List<string> Validate(LocalizationSettingsSO settings)
        {
            var problems = new List<string>();
            var source = LocalizationEditorAssets.ReadTable(settings, settings.SourceLanguage);
            CheckScenes(settings, problems);
            CheckProfiles(settings, problems);

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { LocalizationKeyExtractor.PrefabRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var label in prefab.GetComponentsInChildren<LocalizedText>(true))
                    Check(label.Key, source, settings, $"{path} / {label.name}", problems);
                foreach (var behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                    if (behaviour != null && !(behaviour is LocalizedText)) CheckFields(behaviour, source, settings, path, problems);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/_Dev/Data" }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && asset.GetType().Assembly.GetName().Name == "Game.Game" && !(asset is LocalizationSettingsSO))
                    CheckFields(asset, source, settings, AssetDatabase.GetAssetPath(asset), problems);
            }

            foreach (var problem in problems) Debug.LogError("[Localization] " + problem);
            return problems;
        }

        // A scene on another (or no) settings asset flips the language or drops localization there.
        static void CheckScenes(LocalizationSettingsSO settings, List<string> problems)
        {
            var settingsPath = AssetDatabase.GetAssetPath(settings);
            foreach (var scene in Scenes)
                if (System.Array.IndexOf(AssetDatabase.GetDependencies(scene, false), settingsPath) < 0)
                    problems.Add($"{scene} does not reference {settingsPath}: assign it on the scene's scope.");
        }

        // A missing slot keeps the authored font; a missing style drops to the font's default material.
        static void CheckProfiles(LocalizationSettingsSO settings, List<string> problems)
        {
            var source = settings.Find(settings.SourceLanguage)?.Fonts;
            if (source == null) return;
            foreach (var entry in settings.Languages)
            {
                if (entry == null || entry.Fonts == null || entry.Fonts == source) continue;
                foreach (var slot in source.Slots)
                {
                    var target = entry.Fonts.Find(slot.Id);
                    if (target == null)
                    {
                        problems.Add($"{entry.Code} font profile has no slot \"{slot.Id}\".");
                        continue;
                    }
                    foreach (var style in slot.Styles)
                        if (style != null && target.Styles.All(s => s == null || s.Style != style.Style || s.Material == null))
                            problems.Add($"{entry.Code} slot \"{slot.Id}\" has no material for style \"{style.Style}\".");
                }
            }
        }

        static void CheckFields(Object target, Dictionary<string, string> source, LocalizationSettingsSO settings, string path,
            List<string> problems)
        {
            if (target.GetType().Assembly.GetName().Name != "Game.Game") return;
            var property = new SerializedObject(target).GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.String) continue;
                var field = property.propertyPath.Contains(".Array.") && property.name == "data"
                    ? property.propertyPath.Substring(0, property.propertyPath.IndexOf(".Array.", System.StringComparison.Ordinal))
                    : property.name;
                if (!KeyFields.Contains(field.Substring(field.LastIndexOf('.') + 1))) continue;
                Check(property.stringValue, source, settings, $"{path} / {target.name}.{property.propertyPath}", problems);
            }
        }

        static void Check(string key, Dictionary<string, string> source, LocalizationSettingsSO settings, string where,
            List<string> problems)
        {
            // A number-only format ("{0}x{1}") is not text and needs no key.
            if (string.IsNullOrEmpty(key) || source.ContainsKey(key) || !Word.IsMatch(key)) return;
            problems.Add($"{where}: \"{key}\" is not in the {settings.SourceLanguage} table.");
        }
    }
}
