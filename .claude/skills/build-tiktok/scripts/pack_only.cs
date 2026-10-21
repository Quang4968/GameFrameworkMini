// eval_file body: packs <OutputDir>/tt-minigame from an existing <OutputDir>/webgl build (the TTSDK window's Pack WebGL
// step) without rebuilding. Use it when the WebGL build succeeded but TTSDK refused to commit the output. Logs [PackOnly].
var asm = System.Array.Find(System.AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == "ttsdk_tools");
var bf = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
var settingsType = asm.GetType("TTSDK.Tool.StarkBuilderSettings");
var s = settingsType.GetMethod("LoadSettings").Invoke(null, new object[] { false });
var outDir = (string)settingsType.GetField("OutputDir", bf).GetValue(s);
if (!System.IO.File.Exists(outDir + "/webgl/Build/webgl.wasm")) return "no webgl build in " + outDir + ": run trigger_build.cs first";
var builder = asm.GetType("TTSDK.Tool.Builder");
var m = builder.GetMethod("PackageWebGLPackageChecked", bf);
UnityEditor.EditorApplication.delayCall += () =>
{
    try { var r = m.Invoke(null, new object[] { s, outDir, true }); UnityEngine.Debug.Log("[PackOnly] result=" + r); }
    catch (System.Exception e) { UnityEngine.Debug.LogError("[PackOnly] " + (e.InnerException ?? e)); }
};
return "pack queued out=" + outDir + " returns=" + m.ReturnType.Name;
