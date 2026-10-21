// eval_file body: applies the build flags, saves StarkBuilderSettings, then queues the TTSDK build window's own
// Build button on delayCall (the build outlives eval's 5 s main-thread cap). Edit the two flags before running.
// A release build also drops the debug symbols and links with LTO; codeOptimization lives in Library/ (per machine),
// so it is set here on every build rather than trusted from the project.
var devBuild = true;
var sdkLog = true;
var release = !devBuild && !sdkLog;

var asm = System.Array.Find(System.AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == "ttsdk_tools");
if (asm == null) return "ttsdk_tools not loaded: is Assets/Plugins/com.tiktok.minigame imported?";

var bf = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var settingsType = asm.GetType("TTSDK.Tool.StarkBuilderSettings");
var s = settingsType.GetMethod("LoadSettings").Invoke(null, new object[] { false });
if (s == null) return "no StarkBuilderSettings: open TikTokGame/Build Minigame once and enter the appid";

var appId = (string)settingsType.GetField("_appId", bf).GetValue(s);
if (string.IsNullOrEmpty(appId)) return "appid empty: paste it in TikTokGame/Build Minigame first";

settingsType.GetField("isDevBuild", bf).SetValue(s, devBuild);
settingsType.GetField("enableSDKLog", bf).SetValue(s, sdkLog);
settingsType.GetField("symbolMode", bf).SetValue(s, release ? UnityEditor.WebGLDebugSymbolMode.Off : UnityEditor.WebGLDebugSymbolMode.External);
UnityEditor.WebGL.UserBuildSettings.codeOptimization = release
    ? UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO
    : UnityEditor.WebGL.WasmCodeOptimization.BuildTimes;
settingsType.GetMethod("Save").Invoke(s, null);
UnityEditor.AssetDatabase.SaveAssets();

// TTSDK refuses to commit when <OutputDir>/webgl already exists (the WebGL build recreates it, so its identity
// changes mid-transaction): clear the previous output and its backups first. Only these two names, only outside the project.
var outDir = (string)settingsType.GetField("OutputDir", bf).GetValue(s);
var projectDir = System.IO.Path.GetFullPath(System.IO.Directory.GetCurrentDirectory());
if (string.IsNullOrEmpty(outDir) || !System.IO.Path.IsPathRooted(outDir)
    || System.IO.Path.GetFullPath(outDir).StartsWith(projectDir))
    return "OutputDir must be an absolute path outside the project: " + outDir;
var cleared = new System.Collections.Generic.List<string>();
if (System.IO.Directory.Exists(outDir))
{
    foreach (var dir in System.IO.Directory.GetDirectories(outDir))
    {
        var name = System.IO.Path.GetFileName(dir);
        if (name != "webgl" && !name.StartsWith("webgl.ttsdk-backup-")) continue;
        System.IO.Directory.Delete(dir, true);
        cleared.Add(name);
    }
}

var windowType = asm.GetType("TTSDK.Tool.StarkSDKToolWindow");
UnityEditor.EditorApplication.delayCall += () =>
{
    var window = UnityEditor.EditorWindow.GetWindow(windowType);
    windowType.GetMethod("TriggerBuild", bf).Invoke(window, new object[] { sdkLog });
};

return "queued appid=" + appId
    + " framework=" + settingsType.GetField("framework", bf).GetValue(s)
    + " publish=" + settingsType.GetField("_publishType", bf).GetValue(s)
    + " dev=" + devBuild + " sdkLog=" + sdkLog
    + " symbols=" + settingsType.GetField("symbolMode", bf).GetValue(s)
    + " codeOpt=" + UnityEditor.WebGL.UserBuildSettings.codeOptimization
    + " out=" + outDir
    + " cleared=[" + string.Join(",", cleared) + "]";
