// eval_file body, read-only: reports what makes the TikTok package big. Prints "OK" or "FIX <what> -> <how>" per
// line, then the heaviest offenders. Run it before a release build and after adding art or audio.
var gameRoot = "Assets/_Dev"; // <Root> from the game's project card: link.xml and the art and audio audited live under it
var target = UnityEditor.Build.NamedBuildTarget.WebGL;
var lines = new System.Collections.Generic.List<string>();
void Check(bool ok, string what, string fix) { lines.Add((ok ? "OK   " : "FIX  ") + what + (ok ? "" : " -> " + fix)); }

// Code size: these decide the wasm, by far the largest download and the slowest thing to compile on a phone.
var stripping = UnityEditor.PlayerSettings.GetManagedStrippingLevel(target);
Check(stripping >= UnityEditor.ManagedStrippingLevel.Medium, "managed stripping " + stripping,
    "Medium, with " + gameRoot + "/link.xml keeping the assemblies VContainer builds by reflection");
Check(System.IO.File.Exists(gameRoot + "/link.xml"), gameRoot + "/link.xml present", "restore it before stripping at Medium");
var codeGen = UnityEditor.PlayerSettings.GetIl2CppCodeGeneration(target);
Check(codeGen == UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize, "IL2CPP code generation " + codeGen, "OptimizeSize");
Check(UnityEditor.PlayerSettings.stripEngineCode, "strip engine code", "on");
Check(UnityEditor.PlayerSettings.WebGL.exceptionSupport != UnityEditor.WebGLExceptionSupport.FullWithStacktrace
    && UnityEditor.PlayerSettings.WebGL.exceptionSupport != UnityEditor.WebGLExceptionSupport.FullWithoutStacktrace,
    "exceptions " + UnityEditor.PlayerSettings.WebGL.exceptionSupport, "ExplicitlyThrownExceptionsOnly (UniTask cancellation needs it)");
lines.Add("INFO code optimization " + UnityEditor.WebGL.UserBuildSettings.codeOptimization
    + " (per machine; trigger_build.cs sets DiskSizeLTO for a release)");

// Render pipeline: post-processing data and an HDR PC asset drag in textures and shader variants the game never uses.
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets" }))
{
    var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    var so = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p));
    Check(so.FindProperty("postProcessData").objectReferenceValue == null, "no post-process data on " + p,
        "clear it (renderer inspector > Post-processing off) unless a camera renders post-processing");
}
var defaultRp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
Check(defaultRp != null && defaultRp.name.StartsWith("Mobile"), "default render pipeline " + (defaultRp ? defaultRp.name : "none"),
    "Mobile_RPAsset; desktop quality levels exclude WebGL");

// Textures: on WebGL a mipmapped NPOT sprite is not compressed and ships as raw RGBA32.
var heavy = new System.Collections.Generic.List<(long size, string line)>();
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { gameRoot }))
{
    var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    var ti = UnityEditor.AssetImporter.GetAtPath(p) as UnityEditor.TextureImporter;
    var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(p);
    if (ti == null || tex == null || p.Contains("/UI/Icon/")) continue;
    var f = tex.format.ToString();
    var compressed = f.StartsWith("ASTC") || f.StartsWith("ETC") || f.StartsWith("DXT") || f.StartsWith("BC");
    long size = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.ComputeMipmapSize(tex.width, tex.height, tex.graphicsFormat);
    if (tex.mipmapCount > 1) size = size * 4 / 3;    // the size it ships at, from format and resolution
    if (!compressed && size > 256 * 1024)
        lines.Add($"FIX  {p} {tex.width}x{tex.height} {f} mips={ti.mipmapEnabled} -> "
            + (ti.mipmapEnabled ? "turn mipmaps off (sprite)" : "set a compressed format"));
    heavy.Add((size, $"{size / 1024}KB {p} {f}"));
}

// Audio: long stereo clips at full quality dominate the sound budget.
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { gameRoot }))
{
    var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    var ai = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(p);
    var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AudioClip>(p);
    if (clip == null || clip.length < 10f) continue;
    Check(ai.forceToMono && ai.defaultSampleSettings.quality <= 0.5f,
        $"{p} {clip.length:F0}s mono={ai.forceToMono} quality={ai.defaultSampleSettings.quality}",
        "force to mono, quality 0.4 (WebGL has no per-platform audio override)");
}

heavy.Sort((a, b) => b.size.CompareTo(a.size));
lines.Add("Heaviest textures:");
for (var i = 0; i < System.Math.Min(5, heavy.Count); i++) lines.Add("     " + heavy[i].line);
return string.Join("\n", lines);
