using UnityEngine;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEditor;
using System;

#if !UNITY_6000_3_OR_NEWER
[InitializeOnLoad]
#endif
public class ToolBarExtender
{
    public static Action OnGUILeftAction, OnGUIRightAction;

    const string k_LeftContainerName  = "ToolbarExtender_Left";
    const string k_RightContainerName = "ToolbarExtender_Right";

    static readonly Type m_toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");

    // null  = not yet found
    // != null & attached = working
    // != null & failed = m_rootMissing flag is set
    static ScriptableObject m_currentToolbar;
    static bool m_rootFieldMissing;

    static ToolBarExtender()
    {
        EditorApplication.update += OnUpdate;
    }

    private static void OnUpdate()
    {
        // Already confirmed missing API — nothing we can do.
        if (m_rootFieldMissing)
            return;

        // Toolbar ScriptableObject is destroyed on layout changes; null means we must re-attach.
        if (m_currentToolbar != null)
            return;

        var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
        if (toolbars.Length == 0)
            return;

        var toolbar = (ScriptableObject)toolbars[0];

        var rootField = toolbar.GetType()
            .GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);

        if (rootField == null)
        {
            m_rootFieldMissing = true;
            Debug.LogError("[ToolBarExtender] 'm_Root' field not found on UnityEditor.Toolbar. " +
                           "The internal Unity API may have changed.");
            return;
        }

        var mRoot = rootField.GetValue(toolbar) as VisualElement;
        if (mRoot == null)
            // Toolbar exists but UIElements root not ready yet — retry next frame.
            return;

        // Zones may not yet be in the visual tree on the first few updates.
        // Probe both zones; if either is missing, retry next frame without locking in.
        var leftZone  = mRoot.Q("ToolbarZoneLeftAlign");
        var rightZone = mRoot.Q("ToolbarZoneRightAlign");
        if (leftZone == null || rightZone == null)
            return;

        // Lock in only AFTER everything is ready.
        m_currentToolbar = toolbar;

        AttachContainer(leftZone,  k_LeftContainerName,  OnGUILeft);
        AttachContainer(rightZone, k_RightContainerName, OnGUIRight);

        Debug.Log($"[ToolBarExtender] Attached. Left subscribers={OnGUILeftAction?.GetInvocationList().Length ?? 0}, " +
                  $"Right subscribers={OnGUIRightAction?.GetInvocationList().Length ?? 0}");
    }

    static void AttachContainer(VisualElement zone, string containerName, Action cb)
    {
        // Guard: don't add a second container if this zone was already set up.
        if (zone.Q(containerName) != null)
            return;

        var parent = new VisualElement
        {
            name = containerName,
            style =
            {
                flexGrow      = 1,
                flexDirection = FlexDirection.Row,
                height        = 22,
                alignItems    = Align.Center,
            }
        };

        var container = new IMGUIContainer();
        container.style.flexGrow = 1;
        container.onGUIHandler += () => cb?.Invoke();

        parent.Add(container);
        zone.Add(parent);
    }

    static void OnGUILeft()  => OnGUILeftAction?.Invoke();
    static void OnGUIRight() => OnGUIRightAction?.Invoke();
}

