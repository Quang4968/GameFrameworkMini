using UnityEditor;
using UnityEngine;

#if UNITY_6000_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

/// <summary>
/// Adds a Time.timeScale slider + reset button to the Unity Editor main toolbar.
///
/// Unity 6.3+      : uses the official UnityEditor.Toolbars.MainToolbarElement API.
/// Older versions  : falls back to the reflection-based <see cref="ToolBarExtender"/>.
/// </summary>
#if UNITY_6000_3_OR_NEWER
[InitializeOnLoad]
public static class TimeScaleToolbar
{
    const string k_ElementPath = "Tools/TimeScale";

    // MainToolbarCustom is internal — cache reflected ctor: ctor(Func<VisualElement>).
    static readonly ConstructorInfo s_CustomCtor = FindCustomCtor();

    static ConstructorInfo FindCustomCtor()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType("UnityEditor.Toolbars.MainToolbarCustom");
            if (t != null)
                return t.GetConstructor(new[] { typeof(Func<VisualElement>) });
        }
        return null;
    }

    static MainToolbarElement CreateCustom(Func<VisualElement> factory)
    {
        if (s_CustomCtor == null)
        {
            Debug.LogWarning("[TimeScaleToolbar] MainToolbarCustom not found — skipping numeric field.");
            return null;
        }
        return (MainToolbarElement)s_CustomCtor.Invoke(new object[] { factory });
    }

    static TimeScaleToolbar()
    {
        // Custom toolbar elements are hidden by default in Unity 6.3.
        // Defer until the toolbar exists, then ask Unity to display ours.
        EditorApplication.delayCall += EnsureVisible;
    }

    static void EnsureVisible()
    {
        // MainToolbar.ShowAll is internal — call it via reflection.
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
            Debug.LogWarning("[TimeScaleToolbar] Failed to auto-show element: " + e.Message);
        }
    }

    [MainToolbarElement(k_ElementPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
    static IEnumerable<MainToolbarElement> CreateTimeScaleElements()
    {
        yield return new MainToolbarLabel(
            new MainToolbarContent("Time Scale:", "Adjust Time.timeScale"));

        yield return new MainToolbarSlider(
            new MainToolbarContent(string.Empty, "Drag to change Time.timeScale"),
            value:        Time.timeScale,
            minValue:     0f,
            maxValue:     10f,
            valueChanged: v => Time.timeScale = Mathf.Clamp(v, 0f, 10f));

        // Numeric input field — type an exact value.
        var custom = CreateCustom(() =>
        {
            var field = new FloatField
            {
                value = Time.timeScale,
                isDelayed = true,
                style =
                {
                    width = 42,
                    marginLeft = 2,
                    marginRight = 2,
                    alignSelf = Align.Center,
                },
            };
            field.RegisterValueChangedCallback(evt =>
            {
                Time.timeScale = Mathf.Clamp(evt.newValue, 0f, 100f);
                MainToolbar.Refresh(k_ElementPath);
            });
            // Keep the field in sync when other UI changes Time.timeScale.
            field.schedule.Execute(() =>
            {
                if (!Mathf.Approximately(field.value, Time.timeScale))
                    field.SetValueWithoutNotify(Time.timeScale);
            }).Every(200);
            return field;
        });
        if (custom != null) yield return custom;

        yield return new MainToolbarButton(
            new MainToolbarContent("x0.5", "Set Time.timeScale to 0.5"),
            () => SetTimeScale(0.5f));

        yield return new MainToolbarButton(
            new MainToolbarContent("x1", "Set Time.timeScale to 1.0"),
            () => SetTimeScale(1f));

        yield return new MainToolbarButton(
            new MainToolbarContent("x2", "Set Time.timeScale to 2.0"),
            () => SetTimeScale(2f));
    }

    static void SetTimeScale(float value)
    {
        Time.timeScale = value;
        MainToolbar.Refresh(k_ElementPath);
    }
}
#else
[InitializeOnLoad]
public static class TimeScaleToolbar
{
    static TimeScaleToolbar()
    {
        ToolBarExtender.OnGUILeftAction += DrawTimeScaleSlider;
    }

    private static void DrawTimeScaleSlider()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(80);
        GUILayout.Label("Time Scale:", GUILayout.Width(70));

        float newTimeScale = GUILayout.HorizontalSlider(Time.timeScale, 0f, 10f, GUILayout.Width(100));
        newTimeScale = EditorGUILayout.FloatField(newTimeScale, GUILayout.Width(50));

        if (GUI.changed)
            Time.timeScale = Mathf.Clamp(newTimeScale, 0f, 10f);

        if (GUILayout.Button("Reset", GUILayout.Width(50)))
            Time.timeScale = 1.0f;

        GUILayout.EndHorizontal();
    }
}
#endif