using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MoodBoost.Sdk.Editor
{
    // MoodBoost > TikTok Mini Game > Install TTSDK - imports the TTSDK package the SDK carries in Vendor~/.
    //
    // The package is shipped as a .unitypackage rather than as files under the SDK because its build tool hardcodes
    // Assets/Plugins/com.tiktok.minigame (version lookup and the WebGL template). The "~" keeps Unity from importing it,
    // so a mobile game never compiles TTSDK or gets its menus.
    static class TikTokMiniGameInstaller
    {
        const string InstalledManifest = "Assets/Plugins/com.tiktok.minigame/package.json";
        const string VendorFolder = "Vendor~/TikTokMiniGame";

        [MenuItem("MoodBoost/TikTok Mini Game/Install TTSDK", priority = 40)]
        static void Install()
        {
            var package = FindPackage();
            if (package == null)
            {
                EditorUtility.DisplayDialog("TTSDK", $"No .unitypackage found in {VendorFolder} of the SDK.", "OK");
                return;
            }

            if (File.Exists(InstalledManifest) && !EditorUtility.DisplayDialog("TTSDK",
                    $"TTSDK is already installed. Re-import {Path.GetFileName(package)} over it?", "Re-import", "Cancel"))
            {
                return;
            }

            AssetDatabase.importPackageCompleted += OnImported;
            AssetDatabase.importPackageCancelled += Unsubscribe;
            AssetDatabase.importPackageFailed += OnFailed;
            AssetDatabase.ImportPackage(package, false);
        }

        static void Unsubscribe(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnImported;
            AssetDatabase.importPackageCancelled -= Unsubscribe;
            AssetDatabase.importPackageFailed -= OnFailed;
        }

        static void OnFailed(string packageName, string error)
        {
            Unsubscribe(packageName);
            Debug.LogError($"[SDK/TikTok] importing {packageName} failed: {error}");
        }

        static void OnImported(string packageName)
        {
            Unsubscribe(packageName);
            Debug.Log($"[SDK/TikTok] imported {packageName}. Switch the build target to WebGL, then enable " +
                "MB_TIKTOK in MoodBoost > SDK Modules.");
            EditorApplication.ExecuteMenuItem("MoodBoost/SDK Modules");
        }

        // Resolved from this assembly's asmdef so the SDK keeps working wherever the SDK folder lives.
        static string FindPackage()
        {
            var asmdef = AssetDatabase.FindAssets("MoodBoost.Sdk.Editor t:AssemblyDefinitionAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path => Path.GetFileName(path) == "MoodBoost.Sdk.Editor.asmdef");
            if (asmdef == null) return null;

            var sdkRoot = Path.GetDirectoryName(Path.GetDirectoryName(asmdef));
            var folder = Path.Combine(sdkRoot ?? string.Empty, VendorFolder);
            if (!Directory.Exists(folder)) return null;

            return Directory.GetFiles(folder, "*.unitypackage").OrderBy(path => path).LastOrDefault();
        }
    }
}
