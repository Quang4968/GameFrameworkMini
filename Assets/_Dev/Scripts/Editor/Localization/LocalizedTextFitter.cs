using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Game.Views.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor.Localization
{
    // Turns on TMP auto size for every localized label with words, from its authored size down to
    // authored x minFontScale, so a longer translation shrinks instead of overflowing. Number-only
    // labels are left alone so counters do not jump in size.
    public static class LocalizedTextFitter
    {
        static readonly Regex Word = new Regex(@"\p{L}.*\p{L}", RegexOptions.Singleline);

        // Edited through SerializedObject so a nested instance records a real override.
        public static bool Fit(TMP_Text label, float minScale)
        {
            if (label == null || label.GetComponent<LocalizedText>() == null || !Word.IsMatch(label.text ?? string.Empty)) return false;

            var serialized = new SerializedObject(label);
            var auto = serialized.FindProperty("m_enableAutoSizing");
            var size = serialized.FindProperty("m_fontSizeBase");
            var max = serialized.FindProperty("m_fontSizeMax");
            var min = serialized.FindProperty("m_fontSizeMin");

            // Already auto-sized by a designer, or inherited from a fitted source with the same size.
            if (auto.boolValue && !(size.prefabOverride && !Mathf.Approximately(max.floatValue, size.floatValue))) return false;

            var authored = size.floatValue;
            auto.boolValue = true;
            max.floatValue = authored;
            min.floatValue = Mathf.Max(1f, Mathf.Round(authored * minScale));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Sources before instances: a prefab's own labels in pass one, nested instances in pass two.
        public static int FitAllPrefabs(float minScale)
        {
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { LocalizationKeyExtractor.PrefabRoot }).Select(AssetDatabase.GUIDToAssetPath).ToList();
            var count = 0;
            foreach (var instances in new[] { false, true })
                foreach (var path in paths) count += FitPrefab(path, instances, minScale);
            return count;
        }

        public static int FitPrefab(string path, bool instances, float minScale)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var count = Fit(new[] { root }, instances, minScale);
                if (count > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                return count;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static int FitOpenScene(float minScale)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var count = Fit(roots, false, minScale) + Fit(roots, true, minScale);
            if (count > 0) EditorSceneManager.SaveScene(scene);
            return count;
        }

        static int Fit(IEnumerable<GameObject> roots, bool instances, float minScale)
        {
            var count = 0;
            foreach (var label in roots.SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true)))
                if (PrefabUtility.IsPartOfPrefabInstance(label) == instances && Fit(label, minScale)) count++;
            return count;
        }
    }
}
