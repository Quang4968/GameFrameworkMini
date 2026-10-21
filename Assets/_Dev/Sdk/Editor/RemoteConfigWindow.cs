using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MoodBoost.Sdk.Unity;
using UnityEditor;
using UnityEngine;

namespace MoodBoost.Sdk.Editor
{
    // MoodBoost > Remote Config - shows every ConfigKey declared in the project, edits this project's defaults for them
    // and exports the template for Firebase.
    //
    // This window is the only place with the whole picture: keys declared in the SDK, keys declared in the game, which
    // names collide, which lack a description. Reading the code instead means opening every file, and a name collision
    // is something no amount of looking will catch.
    //
    // It is also the only place the Remote Config part of SdkSettings is edited - those fields are hidden from the
    // asset's Inspector. A default means nothing without its key, and only this window knows which keys exist: typed by
    // hand into the Inspector, a misspelt key name was ignored by the game and the template alike, without a word.
    public sealed class RemoteConfigWindow : EditorWindow
    {
        const string TemplateFileName = "remoteconfig.template.json";

        // Private fields of SdkSettings, reached through SerializedObject so an edit gets Undo and marks the asset dirty.
        const string DefaultsField = "remoteConfigDefaults";

        const float ValueWidth = 120f;
        const float ResetWidth = 52f;

        static readonly GUIContent ResetLabel = new GUIContent(
            "Reset", "Remove this project's value - the key runs on the default declared in code.");

        static GUIStyle wrappedTextArea;

        List<DeclaredParameter> parameters;
        HashSet<string> declaredNames;
        List<string> problems;
        List<string> warnings;

        SdkSettings settings;
        string settingsNotice;
        SerializedObject settingsObject;
        SerializedProperty defaults;

        Vector2 scroll;
        bool showWarnings;

        [MenuItem("MoodBoost/Remote Config", priority = 20)]
        static void Open()
        {
            var window = GetWindow<RemoteConfigWindow>(false, "Remote Config", true);
            window.minSize = new Vector2(600f, 360f);
            window.Reload();
        }

        void OnEnable() => Undo.undoRedoPerformed += Repaint;

        void OnDisable() => Undo.undoRedoPerformed -= Repaint;

        void OnFocus() => Reload();

        void Reload()
        {
            parameters = RemoteConfigTemplate.Collect();
            declaredNames = new HashSet<string>(parameters.Select(entry => entry.Parameter.Name), StringComparer.Ordinal);
            problems = RemoteConfigTemplate.Validate(parameters);
            warnings = RemoteConfigTemplate.Warnings(parameters);
            BindSettings();
        }

        void BindSettings()
        {
            var paths = AssetDatabase.FindAssets($"t:{nameof(SdkSettings)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();

            settings = paths.Length > 0 ? AssetDatabase.LoadAssetAtPath<SdkSettings>(paths[0]) : null;

            // Said here rather than logged: this runs on every focus, and the console would fill with the same line.
            settingsNotice = paths.Length > 1
                ? $"Found {paths.Length} SdkSettings - editing the first. Each scene may be pointing at a different " +
                  "one, keep only one:\n  " + string.Join("\n  ", paths)
                : null;

            if (settings != null && settingsObject != null && settingsObject.targetObject == settings) return;

            settingsObject?.Dispose();
            settingsObject = settings != null ? new SerializedObject(settings) : null;
            defaults = settingsObject?.FindProperty(DefaultsField);
        }

        void OnGUI()
        {
            if (parameters == null) Reload();

            // The asset was deleted while the window stayed open.
            if (settingsObject != null && settingsObject.targetObject == null) BindSettings();

            settingsObject?.Update();

            EditorGUILayout.HelpBox(
                "Every ConfigKey declared with 'public static readonly' appears here automatically - there is no " +
                "list to register in. A value changed here is saved to SdkSettings and replaces the code default " +
                "for this project only; Reset hands the key back to the code.",
                MessageType.None);

            DrawSettings();
            DrawProblems();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawTable();
            DrawUnusedDefaults();
            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        void DrawSettings()
        {
            if (settingsObject == null)
            {
                EditorGUILayout.HelpBox(
                    "No SdkSettings in the project - the values below are the code defaults and cannot be changed " +
                    "until one exists.",
                    MessageType.Warning);

                if (GUILayout.Button("Create SdkSettings", GUILayout.Width(150f)))
                {
                    SdkSettingsMenu.Open();
                    BindSettings();
                    GUIUtility.ExitGUI();
                }

                return;
            }

            if (settingsNotice != null) EditorGUILayout.HelpBox(settingsNotice, MessageType.Warning);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Settings asset", settings, typeof(SdkSettings), false);
            }
        }

        void DrawProblems()
        {
            if (problems.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "Could not export the template:\n• " + string.Join("\n• ", problems),
                    MessageType.Error);
            }

            if (warnings.Count == 0) return;

            showWarnings = EditorGUILayout.Foldout(showWarnings, $"Warnings ({warnings.Count})", true);
            if (showWarnings) EditorGUILayout.HelpBox("• " + string.Join("\n• ", warnings), MessageType.Warning);
        }

        void DrawTable()
        {
            if (parameters.Count == 0)
            {
                EditorGUILayout.LabelField("No key declared yet. See AdsRemoteConfig for an example.");
                return;
            }

            foreach (var group in parameters.GroupBy(entry => entry.Parameter.Group))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField(
                    string.IsNullOrEmpty(group.Key) ? "(ungrouped)" : group.Key,
                    EditorStyles.boldLabel);

                foreach (var entry in group) DrawRow(entry);
            }
        }

        // Everything the row draws is decided from the state BEFORE this event's edit: an edit that adds or removes the
        // project value changes the row's layout, and IMGUI only tolerates that from the next event on.
        void DrawRow(DeclaredParameter entry)
        {
            var parameter = entry.Parameter;
            var type = ClrType(parameter);
            var codeDefault = parameter.DefaultAsTemplateValue;

            var index = FindDefault(parameter.Name);
            var stored = index >= 0 ? ValueAt(index) : null;
            var storedIsValid = stored == null || IsValid(type, stored);
            var current = stored != null && storedIsValid ? stored : codeDefault;

            string edited = null;
            var reset = false;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            using (new EditorGUI.DisabledScope(settingsObject == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.SelectableLabel(parameter.Name, EditorStyles.boldLabel, GUILayout.Height(16f));

                    GUILayout.FlexibleSpace();

                    EditorGUILayout.LabelField(parameter.ValueType.ToString(), GUILayout.Width(60f));

                    if (type != typeof(string)) edited = ScalarField(type, current);

                    using (new EditorGUI.DisabledScope(stored == null))
                    {
                        reset = GUILayout.Button(ResetLabel, EditorStyles.miniButton, GUILayout.Width(ResetWidth));
                    }
                }

                if (type == typeof(string)) edited = TextField(current);

                if (!storedIsValid)
                {
                    EditorGUILayout.HelpBox(
                        $"The saved value '{stored}' is not a valid {parameter.ValueType} - the game ignores it and " +
                        "runs on the code default shown above. Set a value or press Reset.",
                        MessageType.Warning);
                }

                if (!string.IsNullOrEmpty(parameter.Description))
                {
                    EditorGUILayout.LabelField(parameter.Description, EditorStyles.wordWrappedMiniLabel);
                }

                var origin = stored != null && stored != codeDefault
                    ? $"{entry.Owner}  ·  code default: {codeDefault}"
                    : entry.Owner;

                EditorGUILayout.LabelField(origin, EditorStyles.wordWrappedMiniLabel);
            }

            if (settingsObject == null) return;

            if (reset) ResetDefault(parameter.Name);
            else if (edited != null) SetDefault(parameter.Name, edited);
        }

        // A typed field rather than a text box: at runtime a value that does not parse silently falls back to the code
        // default, so a typo would ship looking as if it had worked. Returns the new value written the way ConfigKey
        // writes it into the template, or null when nothing changed.
        static string ScalarField(Type type, string current)
        {
            EditorGUI.BeginChangeCheck();
            string result;

            if (type == typeof(bool))
            {
                bool.TryParse(current, out var value);
                result = EditorGUILayout.Toggle(value, GUILayout.Width(ValueWidth)) ? "true" : "false";
            }
            else if (type == typeof(long))
            {
                long.TryParse(current, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value);
                result = EditorGUILayout.LongField(value, GUILayout.Width(ValueWidth))
                    .ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                double.TryParse(current, NumberStyles.Float, CultureInfo.InvariantCulture, out var value);
                result = Convert.ToString(
                    EditorGUILayout.DoubleField(value, GUILayout.Width(ValueWidth)), CultureInfo.InvariantCulture);
            }

            return EditorGUI.EndChangeCheck() ? result : null;
        }

        static string TextField(string current)
        {
            if (wrappedTextArea == null) wrappedTextArea = new GUIStyle(EditorStyles.textArea) { wordWrap = true };

            EditorGUI.BeginChangeCheck();
            var result = EditorGUILayout.TextArea(current, wrappedTextArea);
            return EditorGUI.EndChangeCheck() ? result : null;
        }

        // Values left behind by a key that was renamed or deleted. Harmless at runtime, but with the list hidden from the
        // Inspector this is the only place they can still be seen and cleared.
        void DrawUnusedDefaults()
        {
            if (defaults == null) return;

            var removeAt = -1;
            var headerDrawn = false;

            for (var index = 0; index < defaults.arraySize; index++)
            {
                var key = KeyAt(index);
                if (declaredNames.Contains(key)) continue;

                if (!headerDrawn)
                {
                    EditorGUILayout.Space(6f);
                    EditorGUILayout.LabelField("Unused values", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        "Saved in SdkSettings for a key no ConfigKey declares any more.",
                        EditorStyles.wordWrappedMiniLabel);
                    headerDrawn = true;
                }

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.SelectableLabel(
                        string.IsNullOrWhiteSpace(key) ? "(empty key)" : key, EditorStyles.boldLabel,
                        GUILayout.Height(16f));
                    EditorGUILayout.SelectableLabel(ValueAt(index), GUILayout.Height(16f));

                    if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(ResetWidth + 8f)))
                    {
                        removeAt = index;
                    }
                }
            }

            if (removeAt < 0) return;

            defaults.DeleteArrayElementAtIndex(removeAt);
            settingsObject.ApplyModifiedProperties();
        }

        void DrawFooter()
        {
            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rescan", GUILayout.Width(90f))) Reload();

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(problems.Count > 0 || parameters.Count == 0))
                {
                    if (GUILayout.Button("Copy JSON", GUILayout.Width(100f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = RemoteConfigTemplate.Build(parameters, settings);
                        ShowNotification(new GUIContent("Copied"));
                    }

                    if (GUILayout.Button("Export template…", GUILayout.Width(130f))) Export();
                }
            }

            EditorGUILayout.LabelField(
                $"Publish: firebase deploy --only remoteconfig (point firebase.json at {TemplateFileName})",
                EditorStyles.miniLabel);
        }

        void Export()
        {
            // Writes to the repository root by default, next to firebase.json - not into Assets/, because this file is an
            // input to the Firebase CLI rather than a game asset.
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

            var path = EditorUtility.SaveFilePanel(
                "Export Remote Config template", projectRoot, TemplateFileName, "json");

            if (string.IsNullOrEmpty(path)) return;

            File.WriteAllText(path, RemoteConfigTemplate.Build(parameters, settings));

            SdkLog.Warn("RemoteConfig", $"Exported {parameters.Count} parameter(s) to {path}");
            EditorUtility.RevealInFinder(path);
        }

        // Searched from the end: on a repeated key the last entry wins, as in RemoteConfigDefaults.Set.
        int FindDefault(string name)
        {
            if (defaults == null) return -1;

            for (var index = defaults.arraySize - 1; index >= 0; index--)
            {
                if (KeyAt(index) == name) return index;
            }

            return -1;
        }

        string KeyAt(int index) => defaults.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue;

        string ValueAt(int index) => defaults.GetArrayElementAtIndex(index).FindPropertyRelative("value").stringValue;

        void SetDefault(string name, string value)
        {
            var index = FindDefault(name);

            if (index < 0)
            {
                // A grown array copies the previous last element into the new slot - both fields are overwritten.
                index = defaults.arraySize;
                defaults.arraySize++;
                defaults.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue = name;
            }

            defaults.GetArrayElementAtIndex(index).FindPropertyRelative("value").stringValue = value;
            settingsObject.ApplyModifiedProperties();
        }

        // Every entry for the key, not only the last - otherwise an older duplicate would quietly take over.
        void ResetDefault(string name)
        {
            for (var index = defaults.arraySize - 1; index >= 0; index--)
            {
                if (KeyAt(index) == name) defaults.DeleteArrayElementAtIndex(index);
            }

            settingsObject.ApplyModifiedProperties();
        }

        // The T of the ConfigKey<T> behind the parameter. ValueType alone cannot tell a long from a double - both are
        // Number - and a long key given "2.5" falls back to its code default at runtime.
        static Type ClrType(IConfigParameter parameter)
        {
            var type = parameter.GetType();
            return type.IsGenericType ? type.GetGenericArguments()[0] : typeof(string);
        }

        // The same parsing RemoteConfigDefaults applies at runtime, so the window flags exactly what the game ignores.
        static bool IsValid(Type type, string value)
        {
            if (type == typeof(bool)) return bool.TryParse(value, out _);

            if (type == typeof(long))
            {
                return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            }

            if (type == typeof(double))
            {
                return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
            }

            return true;
        }
    }
}
