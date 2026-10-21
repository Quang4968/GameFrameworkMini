using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_6000_3_OR_NEWER
using System;
using System.Reflection;
using UnityEditor.Toolbars;
#endif

#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif

/// <summary>
/// Dropdown trên main toolbar để chuyển nhanh giữa các scene của build.
///
/// Unity 6.3+     : dùng API chính thức UnityEditor.Toolbars.MainToolbarElement.
/// Bản cũ hơn     : fallback sang <see cref="ToolBarExtender"/> (reflection).
///
/// Nguồn scene: build profile đang active nếu profile đó override scene list,
/// nếu không thì lấy scene list chung trong Build Settings.
/// Giữ Alt khi bấm dropdown để mở scene ở chế độ additive.
/// </summary>
[InitializeOnLoad]
public static class SceneSwitcherToolbar
{
    struct SceneEntry
    {
        public string Path;
        public string Name;
        public bool   Enabled;
    }

    // Alt state phải bắt lúc bấm nút; callback của GenericMenu chạy sau khi Event đã mất.
    static bool s_OpenAdditive;

#if UNITY_6000_3_OR_NEWER
    const string k_ElementPath = "Tools/SceneSwitcher";

    static SceneSwitcherToolbar()
    {
        // Custom toolbar element bị ẩn mặc định ở Unity 6.3 — đợi toolbar dựng xong rồi bảo Unity hiện nó.
        EditorApplication.delayCall += EnsureVisible;

        EditorSceneManager.sceneOpened          += (_, _) => MainToolbar.Refresh(k_ElementPath);
        EditorSceneManager.sceneClosed          += _      => MainToolbar.Refresh(k_ElementPath);
        EditorSceneManager.activeSceneChangedInEditMode += (_, _) => MainToolbar.Refresh(k_ElementPath);
        EditorApplication.playModeStateChanged  += _      => MainToolbar.Refresh(k_ElementPath);
    }

    static void EnsureVisible()
    {
        // MainToolbar.ShowAll là internal — gọi qua reflection.
        try
        {
            var m = typeof(MainToolbar).GetMethod("ShowAll",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                null, new[] { typeof(string) }, null);
            m?.Invoke(null, new object[] { k_ElementPath });
            MainToolbar.Refresh(k_ElementPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SceneSwitcherToolbar] Failed to auto-show element: " + e.Message);
        }
    }

    [MainToolbarElement(k_ElementPath, defaultDockPosition = MainToolbarDockPosition.Left)]
    static IEnumerable<MainToolbarElement> CreateSceneDropdown()
    {
        var content = new MainToolbarContent(
            CurrentSceneLabel(),
            "Chuyển scene trong build list (giữ Alt để mở additive)");

        yield return new MainToolbarDropdown(content, rect =>
        {
            if (EditorApplication.isPlaying)
            {
                var menu = new GenericMenu();
                menu.AddDisabledItem(new GUIContent("Không đổi scene khi đang Play"));
                menu.DropDown(rect);
                return;
            }

            s_OpenAdditive = Event.current != null && Event.current.alt;
            BuildMenu().DropDown(rect);
        });
    }
#else
    static SceneSwitcherToolbar()
    {
        // Truy cập ToolBarExtender cũng chính là thứ kích hoạt static ctor của nó.
        ToolBarExtender.OnGUILeftAction += OnToolbarGUI;
    }

    static void OnToolbarGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(4);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
        {
            var label = new GUIContent(
                CurrentSceneLabel(),
                EditorApplication.isPlaying
                    ? "Không đổi scene khi đang Play"
                    : "Chuyển scene trong build list (giữ Alt để mở additive)");

            var rect = GUILayoutUtility.GetRect(label, EditorStyles.toolbarDropDown, GUILayout.Width(170));
            if (EditorGUI.DropdownButton(rect, label, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                s_OpenAdditive = Event.current != null && Event.current.alt;
                BuildMenu().DropDown(rect);
            }
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
#endif

    static string CurrentSceneLabel()
    {
        var scene = SceneManager.GetActiveScene();
        var name  = string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name;
        return SceneManager.loadedSceneCount > 1 ? $"{name} (+{SceneManager.loadedSceneCount - 1})" : name;
    }

    static GenericMenu BuildMenu()
    {
        var menu   = new GenericMenu();
        var scenes = GetBuildScenes(out var sourceLabel);

        menu.AddDisabledItem(new GUIContent(sourceLabel));
        menu.AddSeparator(string.Empty);

        if (scenes.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No scenes in build list"));
        }
        else
        {
            var activePath = SceneManager.GetActiveScene().path;
            var index      = 0;

            foreach (var entry in scenes)
            {
                var path = entry.Path;
                var item = entry.Enabled
                    ? new GUIContent($"{index++}  {entry.Name}")
                    : new GUIContent($"Disabled/{entry.Name}");

                menu.AddItem(item, path == activePath, () => OpenScene(path));
            }
        }

        menu.AddSeparator(string.Empty);
        menu.AddItem(new GUIContent("Build Profiles..."), false,
            () => EditorApplication.ExecuteMenuItem("File/Build Profiles"));

        return menu;
    }

    static List<SceneEntry> GetBuildScenes(out string sourceLabel)
    {
#if UNITY_6000_0_OR_NEWER
        var profile = BuildProfile.GetActiveBuildProfile();
        if (profile != null && profile.overrideGlobalScenes && profile.scenes != null && profile.scenes.Length > 0)
        {
            sourceLabel = $"Profile: {profile.name}";
            return ToEntries(profile.scenes);
        }
#endif
        sourceLabel = "Build Settings (shared scene list)";
        return ToEntries(EditorBuildSettings.scenes);
    }

    static List<SceneEntry> ToEntries(EditorBuildSettingsScene[] scenes)
    {
        var result = new List<SceneEntry>(scenes.Length);

        foreach (var scene in scenes)
        {
            if (string.IsNullOrEmpty(scene.path))
                continue;

            result.Add(new SceneEntry
            {
                Path    = scene.path,
                Name    = System.IO.Path.GetFileNameWithoutExtension(scene.path),
                Enabled = scene.enabled,
            });
        }

        return result;
    }

    static void OpenScene(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning($"[SceneSwitcherToolbar] Scene not found: {path}");
            return;
        }

        if (s_OpenAdditive)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }
}
