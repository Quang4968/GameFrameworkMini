using Game.Game.Data;
using UnityEditor;

namespace Game.Editor.Localization
{
    // The settings asset syncs its own tables: the Sheet section sits under its fields.
    [CustomEditor(typeof(LocalizationSettingsSO))]
    public sealed class LocalizationSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            LocalizationSyncPanel.Draw((LocalizationSettingsSO)target);
        }
    }
}
