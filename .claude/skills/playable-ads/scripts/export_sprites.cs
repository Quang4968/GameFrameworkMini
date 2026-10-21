// eval_file body: writes single sprites cut from sliced sheets as PNGs for a playable. Input is
// Temp/playable-export.txt written by `npm run pull` (Playables/_engine/tools/pull-assets.mjs): first line the
// output folder, then one `name|Assets/path.png|SpriteName` per line. Blits through a RenderTexture, so the
// texture needs no Read/Write flag and the import settings stay untouched. Prints one OK/FIX line per sprite.
var listPath = "Temp/playable-export.txt";
if (!System.IO.File.Exists(listPath)) return "FIX no " + listPath + " -> run npm run pull -- <playable> first";
var lines = System.IO.File.ReadAllLines(listPath);
var outDir = lines[0].Trim();
System.IO.Directory.CreateDirectory(outDir);
var report = new System.Text.StringBuilder();
for (var i = 1; i < lines.Length; i++)
{
    var parts = lines[i].Split('|');
    if (parts.Length < 3) continue;
    var name = parts[0].Trim();
    var path = parts[1].Trim();
    var spriteName = parts[2].Trim();
    UnityEngine.Sprite sprite = null;
    foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
    {
        var s = asset as UnityEngine.Sprite;
        if (s != null && s.name == spriteName) { sprite = s; break; }
    }
    if (sprite == null) { report.AppendLine("FIX  " + name + ": no sprite '" + spriteName + "' in " + path); continue; }

    UnityEngine.Rect rect;
    try { rect = sprite.textureRect; }
    catch (System.Exception) { report.AppendLine("FIX  " + name + ": tight-packed sprite, set Mesh Type Full Rect or export the source PNG"); continue; }

    var source = sprite.texture;
    var rt = UnityEngine.RenderTexture.GetTemporary(source.width, source.height, 0, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
    UnityEngine.Graphics.Blit(source, rt);
    var previous = UnityEngine.RenderTexture.active;
    UnityEngine.RenderTexture.active = rt;
    var cut = new UnityEngine.Texture2D((int)rect.width, (int)rect.height, UnityEngine.TextureFormat.RGBA32, false);
    cut.ReadPixels(new UnityEngine.Rect(rect.x, rect.y, rect.width, rect.height), 0, 0);
    cut.Apply();
    UnityEngine.RenderTexture.active = previous;
    UnityEngine.RenderTexture.ReleaseTemporary(rt);

    System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, name + ".png"), UnityEngine.ImageConversion.EncodeToPNG(cut));
    UnityEngine.Object.DestroyImmediate(cut);
    report.AppendLine("OK   " + name + " <- " + path + "#" + spriteName + " (" + (int)rect.width + "x" + (int)rect.height + ")");
}
return report.ToString();
