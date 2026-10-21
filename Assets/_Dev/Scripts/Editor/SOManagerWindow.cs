using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Window quản lý tất cả ScriptableObject trong project.
/// Cho phép kiểm tra, tạo mới asset còn thiếu, và config trực tiếp.
/// Menu: Tools/SO Manager
/// </summary>
public class SOManagerWindow : EditorWindow
{
    // -- Data --
    List<SOEntry> allEntries = new();
    Dictionary<string, List<SOEntry>> groupedEntries = new();
    Dictionary<string, bool> groupFoldouts = new();

    // -- UI State --
    Vector2 scrollPos;
    Vector2 editorScrollPos;
    string searchFilter = "";
    bool showOnlyMissing;
    bool showThirdParty;
    int totalTypes;
    int totalAssets;
    int missingCount;
    int thirdPartyCount;

    // -- Inline Editor --
    Editor cachedEditor;
    SOEntry selectedEntry;
    int selectedAssetIndex;

    // -- Default save paths --
    static readonly Dictionary<string, string> CategoryPaths = new()
    {
        { "Default", "Assets/SO"},
        { "Third Party", "Assets/Resources" },
        { "Other",    "Assets/Resources" },
    };

    // -- Styles (lazy init) --
    static GUIStyle headerStyle;
    static GUIStyle statusOK;
    static GUIStyle statusMissing;
    static GUIStyle selectedRowStyle;

    // =========================================================
    [MenuItem("Tools/SO Manager")]
    public static void Open()
    {
        var win = GetWindow<SOManagerWindow>("SO Manager");
        win.minSize = new Vector2(480, 400);
        win.Refresh();
    }

    void OnEnable() => Refresh();

    void OnFocus() => Refresh();

    // =========================================================
    // SCAN
    // =========================================================
    void Refresh()
    {
        // Preserve selection across rebuild
        Type previousSelectedType = selectedEntry?.type;

        allEntries.Clear();
        groupedEntries.Clear();

        // Lấy tất cả concrete SO types trong project
        var soTypes = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
            .Where(t => !t.IsAbstract
                        && !t.IsGenericType
                        && t.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false).Length > 0)
            .OrderBy(t => t.Name)
            .ToList();

        foreach (var type in soTypes)
        {
            var entry = new SOEntry
            {
                type = type,
                typeName = type.Name,
                category = DetectCategory(type),
            };

            // Tìm tất cả asset instance của type này
            string[] guids = AssetDatabase.FindAssets($"t:{type.Name}");
            entry.assets = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Where(p =>
                {
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(p);
                    return asset != null && asset.GetType() == type;
                })
                .ToList();

            // Lấy CreateAssetMenu attribute
            var attr = (CreateAssetMenuAttribute)type
                .GetCustomAttributes(typeof(CreateAssetMenuAttribute), false)
                .FirstOrDefault();
            entry.menuFileName = attr?.fileName ?? type.Name;
            entry.menuPath = attr?.menuName ?? "";

            allEntries.Add(entry);

            // Grouping
            if (!groupedEntries.ContainsKey(entry.category))
                groupedEntries[entry.category] = new List<SOEntry>();
            groupedEntries[entry.category].Add(entry);

            // Đảm bảo foldout state tồn tại
            if (!groupFoldouts.ContainsKey(entry.category))
                groupFoldouts[entry.category] = true;
        }

        // Disambiguate display names for types that share the same short name (e.g. different namespaces)
        var duplicateNames = allEntries
            .GroupBy(e => e.type.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        foreach (var entry in allEntries)
        {
            if (duplicateNames.Contains(entry.type.Name))
            {
                string ns = entry.type.Namespace;
                entry.typeName = string.IsNullOrEmpty(ns)
                    ? $"{entry.type.Name} (global)"
                    : $"{entry.type.Name} ({ns})";
            }
        }

        // Stats
        totalTypes = allEntries.Count(e => e.category != "Third Party");
        totalAssets = allEntries.Sum(e => e.assets.Count);
        missingCount = allEntries.Count(e => e.assets.Count == 0 && e.category != "Third Party");
        thirdPartyCount = allEntries.Count(e => e.category == "Third Party");

        // Restore selection
        if (previousSelectedType != null)
        {
            selectedEntry = allEntries.FirstOrDefault(e => e.type == previousSelectedType);
            if (selectedEntry != null)
                RebuildCachedEditor();
            else
                DestroyCachedEditor();
        }
    }

    // =========================================================
    // DRAW
    // =========================================================
    void OnGUI()
    {
        InitStyles();

        DrawToolbar();
        DrawStats();

        EditorGUILayout.Space(4);

        // Split view: left = list, right = inline editor
        bool hasRightPanel = selectedEntry != null;
        float rightWidth = hasRightPanel ? Mathf.Max(position.width * 0.45f, 300f) : 0f;
        float leftWidth = position.width - rightWidth - 8f;

        EditorGUILayout.BeginHorizontal();
        {
            // LEFT: SO list
            EditorGUILayout.BeginVertical(GUILayout.Width(leftWidth), GUILayout.ExpandHeight(true));
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            DrawSOList();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // RIGHT: Inline config
            if (hasRightPanel)
            {
                EditorGUILayout.BeginVertical("box", GUILayout.Width(rightWidth), GUILayout.ExpandHeight(true));
                editorScrollPos = EditorGUILayout.BeginScrollView(editorScrollPos);
                DrawInlineEditor();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // -- Toolbar --
    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // Search
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));
        if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(20)))
            searchFilter = "";

        GUILayout.FlexibleSpace();

        // Filter toggles
        showOnlyMissing = GUILayout.Toggle(showOnlyMissing, "Missing Only", EditorStyles.toolbarButton);
        showThirdParty = GUILayout.Toggle(showThirdParty, "3rd Party", EditorStyles.toolbarButton);

        // Refresh
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            Refresh();

        // Create All Missing
        GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
        if (GUILayout.Button("Create All Missing", EditorStyles.toolbarButton))
            CreateAllMissing();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    // -- Stats bar --
    void DrawStats()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Label($"Types: {totalTypes}", EditorStyles.miniLabel);
        GUILayout.Label($"Assets: {totalAssets}", EditorStyles.miniLabel);

        GUI.contentColor = missingCount > 0 ? new Color(1f, 0.4f, 0.4f) : Color.green;
        GUILayout.Label($"Missing: {missingCount}", EditorStyles.miniLabel);
        GUI.contentColor = Color.white;

        if (showThirdParty)
        {
            GUI.contentColor = new Color(0.6f, 0.7f, 1f);
            GUILayout.Label($"3rd Party: {thirdPartyCount}", EditorStyles.miniLabel);
            GUI.contentColor = Color.white;
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    // -- SO List grouped --
    void DrawSOList()
    {
        // Sắp xếp category theo thứ tự ưu tiên
        string[] order = CategoryPaths.Keys.ToArray();
        var sortedKeys = groupedEntries.Keys
            .OrderBy(k => Array.IndexOf(order, k) is int idx && idx >= 0 ? idx : 99)
            .ToList();

        foreach (string category in sortedKeys)
        {
            var entries = groupedEntries[category];
            var filtered = FilterEntries(entries);
            if (filtered.Count == 0) continue;

            int catMissing = filtered.Count(e => e.assets.Count == 0);
            string label = catMissing > 0
                ? $"{category}  ({filtered.Count})  -- {catMissing} missing"
                : $"{category}  ({filtered.Count})";

            groupFoldouts[category] = EditorGUILayout.Foldout(groupFoldouts[category], label, true, headerStyle);

            if (!groupFoldouts[category]) continue;

            EditorGUI.indentLevel++;
            foreach (var entry in filtered)
                DrawSOEntry(entry);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);
        }
    }

    // -- Single SO row --
    void DrawSOEntry(SOEntry entry)
    {
        bool isSelected = selectedEntry == entry;
        bool hasMissing = entry.assets.Count == 0;

        // Highlight selected
        if (isSelected)
        {
            EditorGUILayout.BeginHorizontal(selectedRowStyle);
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
        }

        // Status icon
        var iconContent = hasMissing
            ? EditorGUIUtility.IconContent("console.warnicon.sml")
            : EditorGUIUtility.IconContent("TestPassed");
        GUILayout.Label(iconContent, GUILayout.Width(20), GUILayout.Height(18));

        // Type name — click to select
        if (GUILayout.Button(entry.typeName, EditorStyles.label, GUILayout.MinWidth(160)))
        {
            selectedEntry = entry;
            RebuildCachedEditor();
        }

        GUILayout.FlexibleSpace();

        // Asset count
        GUILayout.Label(hasMissing ? "No asset" : $"{entry.assets.Count} asset(s)",
            hasMissing ? statusMissing : statusOK,
            GUILayout.Width(80));

        // Actions
        if (hasMissing)
        {
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("Create", GUILayout.Width(50)))
                CreateAsset(entry);
            GUI.backgroundColor = Color.white;
        }
        else
        {
            if (GUILayout.Button("Ping", GUILayout.Width(50)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.assets[0]);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
            }
        }

        GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
        if (GUILayout.Button("Add", GUILayout.Width(44)))
            AddIndexedAsset(entry);
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    // -- Inline Editor (right panel) --
    void DrawInlineEditor()
    {
        // Header with close button
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(selectedEntry.typeName, EditorStyles.boldLabel);
        GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
        if (GUILayout.Button("Add New", GUILayout.Width(70)))
        {
            AddIndexedAsset(selectedEntry);
            GUI.backgroundColor = Color.white;
            GUIUtility.ExitGUI();
            return;
        }
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            selectedEntry = null;
            DestroyCachedEditor();
            GUIUtility.ExitGUI();
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (selectedEntry.assets.Count == 0)
        {
            EditorGUILayout.HelpBox("No asset found.", MessageType.Warning);
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("Create Asset", GUILayout.Height(28)))
            {
                CreateAsset(selectedEntry);
                RebuildCachedEditor();
            }
            GUI.backgroundColor = Color.white;
            return;
        }

        // Asset selector (dropdown for multi-asset)
        if (selectedEntry.assets.Count > 1)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Asset:", GUILayout.Width(40));
            string[] rawNames = selectedEntry.assets
                .Select(p => System.IO.Path.GetFileNameWithoutExtension(p))
                .ToArray();
            // Disambiguate assets that share the same filename by appending their parent folder
            var nameCount = rawNames.GroupBy(n => n).ToDictionary(g => g.Key, g => g.Count());
            string[] assetNames = selectedEntry.assets
                .Select((p, i) =>
                {
                    string name = rawNames[i];
                    if (nameCount[name] > 1)
                    {
                        string folder = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(p));
                        return $"{name} ({folder})";
                    }
                    return name;
                })
                .ToArray();
            int newIndex = EditorGUILayout.Popup(selectedAssetIndex, assetNames);
            if (newIndex != selectedAssetIndex)
            {
                selectedAssetIndex = newIndex;
                RebuildCachedEditor();
            }
            if (GUILayout.Button("Ping", GUILayout.Width(40)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(selectedEntry.assets[selectedAssetIndex]);
                if (obj != null) EditorGUIUtility.PingObject(obj);
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(selectedEntry.assets[0], EditorStyles.miniLabel);
            if (GUILayout.Button("Ping", GUILayout.Width(40)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(selectedEntry.assets[0]);
                if (obj != null) EditorGUIUtility.PingObject(obj);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);

        // Draw full inspector - editable
        if (cachedEditor != null)
        {
            EditorGUIUtility.wideMode = true;
            cachedEditor.serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            cachedEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                cachedEditor.serializedObject.ApplyModifiedProperties();
                if (cachedEditor.target != null)
                    EditorUtility.SetDirty(cachedEditor.target);
                Repaint();
            }
        }
    }

    // =========================================================
    // CREATE ASSET
    // =========================================================
    void CreateAsset(SOEntry entry)
    {
        string folder = GetSavePath(entry.category);
        EnsureFolder(folder);

        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{entry.menuFileName}.asset");
        CreateAssetAtPath(entry, path);
    }

    void AddIndexedAsset(SOEntry entry)
    {
        string folder = GetSavePath(entry.category);
        EnsureFolder(folder);

        string path = GetNextIndexedAssetPath(folder, entry.type.Name);
        CreateAssetAtPath(entry, path);
    }

    void CreateAssetAtPath(SOEntry entry, string path)
    {
        var instance = CreateInstance(entry.type);
        AssetDatabase.CreateAsset(instance, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(instance);
        Selection.activeObject = instance;

        Debug.Log($"[SO Manager] Created: {path}");
        Refresh();
        SelectAsset(entry.type, path);
    }

    string GetNextIndexedAssetPath(string folder, string className)
    {
        int index = 1;
        string path;
        do
        {
            path = $"{folder}/{className}{index}.asset";
            index++;
        }
        while (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null);

        return path;
    }

    void SelectAsset(Type type, string path)
    {
        selectedEntry = allEntries.FirstOrDefault(e => e.type == type);
        if (selectedEntry == null)
        {
            DestroyCachedEditor();
            return;
        }

        selectedAssetIndex = Mathf.Max(0, selectedEntry.assets.IndexOf(path));
        RebuildCachedEditor();
    }

    void CreateAllMissing()
    {
        var missing = allEntries.Where(e => e.assets.Count == 0).ToList();
        if (missing.Count == 0)
        {
            EditorUtility.DisplayDialog("SO Manager", "No missing SO found!", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("SO Manager",
                $"Will create {missing.Count} ScriptableObject asset(s):\n\n" +
                string.Join("\n", missing.Select(e => $"  - {e.typeName}")) +
                "\n\nContinue?", "Create", "Cancel"))
            return;

        foreach (var entry in missing)
        {
            string folder = GetSavePath(entry.category);
            EnsureFolder(folder);

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{entry.menuFileName}.asset");
            var instance = CreateInstance(entry.type);
            AssetDatabase.CreateAsset(instance, path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SO Manager] Created {missing.Count} SO assets.");
        Refresh();
    }

    // =========================================================
    // HELPERS
    // =========================================================
    string DetectCategory(Type type)
    {
        if (IsThirdParty(type))
            return "Third Party";
        return "Default";
    }

    bool IsThirdParty(Type type)
    {
        string ns = type.Namespace ?? "";
        string asm = type.Assembly.GetName().Name;
        return ns.StartsWith("DG.") || ns.StartsWith("Cysharp.")
            || asm.StartsWith("DOTween") || asm.StartsWith("UniTask")
            || asm.StartsWith("Unity.") || asm.StartsWith("UnityEngine.")
            || asm.StartsWith("UnityEditor.")
            || ns.StartsWith("TMPro") || asm.StartsWith("com.")
            || ns.StartsWith("Spine") || asm.StartsWith("spine")
            || ns.StartsWith("MaxSdk") || ns.StartsWith("Google")
            || ns.StartsWith("Firebase") || ns.StartsWith("Lofelt")
            || ns.StartsWith("NiceVibrations") || ns.StartsWith("MoreMountains");
    }

    string GetSavePath(string category)
    {
        return CategoryPaths.TryGetValue(category, out string path)
            ? path
            : CategoryPaths["Other"];
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts = path.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    List<SOEntry> FilterEntries(List<SOEntry> entries)
    {
        IEnumerable<SOEntry> result = entries;

        // Ẩn third-party khi toggle tắt
        if (!showThirdParty)
            result = result.Where(e => e.category != "Third Party");

        if (showOnlyMissing)
            result = result.Where(e => e.assets.Count == 0);

        if (!string.IsNullOrEmpty(searchFilter))
            result = result.Where(e =>
                e.typeName.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);

        return result.ToList();
    }

    void RebuildCachedEditor()
    {
        DestroyCachedEditor();
        editorScrollPos = Vector2.zero;

        if (selectedEntry == null || selectedEntry.assets.Count == 0) return;

        selectedAssetIndex = Mathf.Clamp(selectedAssetIndex, 0, selectedEntry.assets.Count - 1);
        var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(selectedEntry.assets[selectedAssetIndex]);
        if (obj != null)
            cachedEditor = Editor.CreateEditor(obj);
    }

    void DestroyCachedEditor()
    {
        if (cachedEditor != null)
            DestroyImmediate(cachedEditor);
        cachedEditor = null;
    }

    void OnDisable()
    {
        DestroyCachedEditor();
    }

    // -- Styles --
    static void InitStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            margin = new RectOffset(4, 4, 4, 2),
        };

        statusOK = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.3f, 0.8f, 0.3f) },
            alignment = TextAnchor.MiddleRight,
        };

        statusMissing = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(1f, 0.4f, 0.4f) },
            alignment = TextAnchor.MiddleRight,
        };

        selectedRowStyle = new GUIStyle()
        {
            normal = { background = MakeTex(2, 2, new Color(0.24f, 0.37f, 0.59f, 0.6f)) },
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
        };
    }

    static Texture2D MakeTex(int w, int h, Color col)
    {
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var tex = new Texture2D(w, h);
        tex.SetPixels(pix);
        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }

    // =========================================================
    // DATA CLASS
    // =========================================================
    class SOEntry
    {
        public Type type;
        public string typeName;
        public string category;
        public string menuFileName;
        public string menuPath;
        public List<string> assets = new();
    }
}
