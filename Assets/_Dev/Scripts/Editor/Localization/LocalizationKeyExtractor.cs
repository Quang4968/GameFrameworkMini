using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Game.Views.Localization;
using Game.Game.Views.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.Localization
{
    // Puts a LocalizedText on every TMP label and keys the static ones "<asset>.<object>". Labels
    // code writes into (referenced by another component) or that mirror another label get no key:
    // they only follow the font. Run twice: pass 1 keys each prefab's own labels, pass 2 keys the
    // text overrides nested instances put on top of them.
    public static class LocalizationKeyExtractor
    {
        public const string PrefabRoot = "Assets/_Dev/Prefabs";

        // Two letters at least: a lone "X" on a close button is an icon, not a word.
        static readonly Regex Word = new Regex(@"\p{L}.*\p{L}", RegexOptions.Compiled | RegexOptions.Singleline);
        static readonly Regex NonWord = new Regex(@"[^a-z0-9]+", RegexOptions.Compiled);
        static readonly Regex CamelHump = new Regex(@"([a-z0-9])([A-Z])", RegexOptions.Compiled);
        static readonly HashSet<string> GenericNames = new HashSet<string>
            { "label", "front", "back", "loading", "text", "button_child", "content", "shadow", "caption_text" };

        public struct Row
        {
            public string Key;
            public string Note;
            public string English;
        }

        // Both passes over every prefab: own labels first, then the overrides nested instances add.
        public static void ExtractAllPrefabs(List<Row> rows, List<string> review)
        {
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }).Select(AssetDatabase.GUIDToAssetPath).ToList();
            foreach (var overrides in new[] { false, true })
                foreach (var path in paths) ExtractPrefab(path, overrides, rows, review, apply: true);
        }

        // apply: false only reports.
        public static void ExtractPrefab(string path, bool overrides, List<Row> rows, List<string> review, bool apply)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var prefix = Snake(Path.GetFileNameWithoutExtension(path));
                if (Process(new[] { root }, prefix, Path.GetFileName(path), overrides, rows, review, apply))
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // The open scene, both passes at once (its nested prefabs are processed already).
        public static void ExtractOpenScene(List<Row> rows, List<string> review, bool apply)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var prefix = Snake(scene.name);
            var changed = Process(roots, prefix, scene.name, false, rows, review, apply);
            changed |= Process(roots, prefix, scene.name, true, rows, review, apply);
            if (changed) EditorSceneManager.SaveScene(scene);
        }

        static bool Process(GameObject[] roots, string prefix, string note, bool overrides, List<Row> rows,
            List<string> review, bool apply)
        {
            var codeDriven = ReferencedLabels(roots);
            var usedKeys = new HashSet<string>(rows.Select(r => r.Key));
            foreach (var existing in roots.SelectMany(r => r.GetComponentsInChildren<LocalizedText>(true)))
                if (!string.IsNullOrEmpty(existing.Key)) usedKeys.Add(existing.Key);
            var changed = false;

            foreach (var label in roots.SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true)))
            {
                var instance = PrefabUtility.IsPartOfPrefabInstance(label);
                if (instance != overrides) continue;

                var localized = label.GetComponent<LocalizedText>();
                var text = label.text != null ? label.text.Trim() : string.Empty;
                var where = $"{note} / {HierarchyPath(label.transform)}";

                if (instance)
                {
                    // Only an instance whose text differs from its source, and not keyed yet, needs its own key.
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(label);
                    if (localized == null || source == null || source.text == label.text || !Word.IsMatch(text)) continue;
                    var sourceLocalized = source.GetComponent<LocalizedText>();
                    if (sourceLocalized != null && sourceLocalized.Key != localized.Key) continue;
                }
                else if (localized != null) continue;

                var key = string.Empty;
                if (codeDriven.Contains(label) && Word.IsMatch(text)) review.Add($"{where} = \"{text}\"");
                else if (label.GetComponent<TextMirror>() == null && Word.IsMatch(text))
                {
                    key = Unique($"{prefix}.{KeyName(label.transform)}", usedKeys);
                    rows.Add(new Row { Key = key, Note = where, English = text });
                }

                if (!apply || (instance && key.Length == 0)) continue;
                if (localized == null) localized = label.gameObject.AddComponent<LocalizedText>();
                if (key.Length > 0 || !instance) SetKey(localized, key);
                LocalizedTextFitter.Fit(label, MinFontScale());
                changed = true;
            }

            return changed;
        }

        static float MinFontScale()
        {
            var settings = LocalizationEditorAssets.FindSettings();
            return settings != null ? settings.MinFontScale : 0.6f;
        }

        public static void SetKey(LocalizedText localized, string key)
        {
            var serialized = new SerializedObject(localized);
            serialized.FindProperty("key").stringValue = key;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static string ToCsv(IEnumerable<Row> rows)
        {
            var lines = rows.Select(r => $"{Cell(r.Key)},{Cell(r.Note)},{Cell(r.English)}");
            return "key,note,en\n" + string.Join("\n", lines) + "\n";
        }

        public static string Cell(string value)
        {
            value ??= string.Empty;
            return value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? value : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public static string Snake(string name) =>
            NonWord.Replace(CamelHump.Replace(name, "$1_$2").ToLowerInvariant(), "_").Trim('_');

        // "ButtonClaim/ClaimText/Front" -> "claim_text": generic leaf names borrow the nearest
        // meaningful ancestor's.
        static string KeyName(Transform node)
        {
            for (var current = node; current != null; current = current.parent)
            {
                var name = Snake(current.name);
                if (!GenericNames.Contains(name)) return name;
            }
            return Snake(node.name);
        }

        // Every TMP label another component holds a reference to; a TextMirror only reads its source.
        static HashSet<TMP_Text> ReferencedLabels(GameObject[] roots)
        {
            var labels = new HashSet<TMP_Text>();
            foreach (var behaviour in roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if (behaviour == null || behaviour is TMP_Text || behaviour is LocalizedText || behaviour is TextMirror) continue;
                var property = new SerializedObject(behaviour).GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is TMP_Text label)
                        labels.Add(label);
            }
            return labels;
        }

        static string Unique(string key, HashSet<string> used)
        {
            var candidate = key;
            for (var i = 2; !used.Add(candidate); i++) candidate = $"{key}_{i}";
            return candidate;
        }

        static string HierarchyPath(Transform node)
        {
            var path = node.name;
            for (var parent = node.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
            return path;
        }
    }
}
