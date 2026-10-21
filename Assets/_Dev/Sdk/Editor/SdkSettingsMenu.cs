using System.Linq;
using MoodBoost.Sdk.Unity;
using UnityEditor;
using UnityEngine;

namespace MoodBoost.Sdk.Editor
{
    // MoodBoost > SDK Settings - selects the configuration asset, creating it if absent.
    //
    // It exists because this asset lives in its own folder outside the SDK cluster, apart from the game's other data, so
    // it is not where people are used to looking. A menu click beats remembering the path.
    public static class SdkSettingsMenu
    {
        // The target folder is OUTSIDE the SDK folder on purpose: the SDK is copied into every new game, while this
        // asset holds ad unit ids belonging to ONE game. Kept inside, the next game would inherit the previous one's ids.
        const string Folder = "Assets/SDKSetting";

        const string AssetPath = Folder + "/SdkSettings.asset";

        [MenuItem("MoodBoost/SDK Settings", priority = 0)]
        internal static void Open()
        {
            var settings = Find();

            if (settings == null)
            {
                settings = Create();
                Debug.Log($"[SDK] No SdkSettings existed, created one at {AssetPath}. " +
                    "Remember to drag it into the sdkSettings field of the LifetimeScope in EVERY scene.");
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        internal static SdkSettings Find()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(SdkSettings)}");
            if (guids.Length == 0) return null;

            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();

            // Two or more assets is a silent trap: each scene may point at a different one, and the only symptom is ads
            // running in one scene and off in another.
            if (paths.Length > 1)
            {
                Debug.LogWarning($"[SDK] Found {paths.Length} SdkSettings in the project - each scene may be " +
                    $"pointing at a different one. Keep only one:\n  {string.Join("\n  ", paths)}");
            }

            return AssetDatabase.LoadAssetAtPath<SdkSettings>(paths[0]);
        }

        static SdkSettings Create()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "SDKSetting");

            var settings = ScriptableObject.CreateInstance<SdkSettings>();
            AssetDatabase.CreateAsset(settings, AssetDatabase.GenerateUniqueAssetPath(AssetPath));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return settings;
        }
    }
}
