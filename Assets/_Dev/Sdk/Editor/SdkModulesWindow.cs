using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace MoodBoost.Sdk.Editor
{
    // MoodBoost > SDK Modules - toggles each SDK switch as a define symbol on the WebGL target.
    //
    // The "imported" label is read by looking for a type in the loaded assemblies: it says whether the underlying SDK
    // is in the project, so a symbol pointing at something uninstalled is never enabled.
    public sealed class SdkModulesWindow : EditorWindow
    {
        static readonly NamedBuildTarget Target = NamedBuildTarget.WebGL;

        [MenuItem("MoodBoost/SDK Modules", priority = 1)]
        static void Open()
        {
            var window = GetWindow<SdkModulesWindow>();
            window.titleContent = new GUIContent("SDK Modules");
            window.minSize = new Vector2(520f, 240f);
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Symbols are written to the WebGL target (TikTok Mini Game); every other target keeps no SDK symbol. " +
                "Unchecked, the TikTok assembly is not compiled and the null implementations take over.",
                MessageType.Info);

            EditorGUILayout.Space();

            foreach (var row in SdkModuleRows.All)
            {
                DrawRow(row);
            }
        }

        static void DrawRow(SdkModuleRow row)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var enabled = GetSymbols().Contains(row.Symbol);
                var imported = row.ProbeType == null || Type.GetType(row.ProbeType) != null;

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!imported))
                    {
                        var next = EditorGUILayout.ToggleLeft($"{row.Title}   ({row.Symbol})", enabled && imported);
                        if (next != enabled) SetSymbol(row.Symbol, next);
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.Label(imported ? "imported" : "not imported", EditorStyles.miniLabel);
                }

                EditorGUILayout.LabelField(row.Description, EditorStyles.wordWrappedMiniLabel);
            }
        }

        static void SetSymbol(string symbol, bool enabled)
        {
            var symbols = GetSymbols();
            var changed = enabled ? symbols.Add(symbol) : symbols.Remove(symbol);
            if (!changed) return;

            PlayerSettings.SetScriptingDefineSymbols(Target, string.Join(";", symbols.OrderBy(s => s)));
            AssetDatabase.Refresh();
        }

        static HashSet<string> GetSymbols()
        {
            var raw = PlayerSettings.GetScriptingDefineSymbols(Target);
            return new HashSet<string>(
                raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
        }
    }
}
