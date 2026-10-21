# Unity Editor CLI

The running Editor is driven through the `unity` CLI and the `com.unity.pipeline` package. This is
the only channel: do not use the `mcp__unityMCP__*` tools; anything they offer is reachable here
through a dedicated command or `eval`. The rules are in `.claude/rules/unity-editor-cli.md`; this file
holds the flows, the `eval` recipes and the reasons behind each gotcha.

## Summary

- The Editor serves the pipeline on the first free port in `127.0.0.1:7800-7849` and auto-starts
  with the project; `unity command` finds it through `Library/Pipeline/.unity-pipeline-port`, so run
  from the project root or pass `--project-path`. No port file = no Editor: say so, never fake a result.
- `recompile` triggers a domain reload, which **discards unsaved scene edits**: save first.
- `eval` compiles a **method body** (no `using`, fully-qualified names) on the **main thread with a
  5 s cap** that `--timeout` does not lift: chunk work per call.
- Screenshots always take `--save_path`; the PNG lands under `Assets/` and is removed with
  `AssetDatabase.DeleteAsset` in `eval` (which also drops the `.meta`). Overlay UI needs
  `--source screen`, Play mode, and the project card's portrait size.
- Play mode dirties the TMP font atlas and spends PlayerPrefs state: revert and restore afterwards.
- `com.unity.pipeline` is experimental; the version is whatever `Packages/manifest.json` says.

## Discovery

```bash
unity command                    # every command with its parameters
unity command --query material   # filter the listing
unity command editor_status      # ready / compiling / playMode before a batch
```

Output is one long JSON line. Filter it in the shell before reading a large result
(`get_scene_hierarchy` has no pagination): `unity command get_scene_hierarchy | cut -f3 | python3 -c '...'`.

## Flows

Tests — the whole result comes back in the response; `test_status` reports `no_tests` afterwards:

```bash
unity command run_tests --mode EditMode
```

C# changes — edit with the file tools, save any open scene, then:

```bash
unity command recompile
unity command recompile_status        # poll until "compiling": false
unity command console --level error
```

Screenshots — `source=camera` (default) omits Screen Space-Overlay UI; `source=screen` includes it
but requires Play mode and the Game view at the real resolution or the canvas comes out squashed:

```bash
unity command capture_game_view --save_path "Temp/shot.png" --width 540 --height 960
unity command capture_game_view --source screen --save_path "Temp/ui.png" --width 1080 --height 1920
unity command eval --code 'return UnityEditor.AssetDatabase.DeleteAsset("Assets/Temp/ui.png");'
```

Long jobs — `build`, `bake_lighting`, `bake_navmesh`, `bake_occlusion_culling`, `audit` return at
once; add `--detach` and poll `<name>_status`.

Scenes, Play mode and menu items have dedicated commands; prefer them over `eval`:

```bash
unity command save_scene                                  # before anything that reloads the domain
unity command open_scene --path "Assets/<Root>/Scenes/MainGame.unity"
unity command editor_play                                 # poll editor_status until playMode = playing
unity command editor_stop
unity command menu --path "<Game>/Validate Level Data"    # any MenuItem
unity command eval_file --file /path/to/script.cs         # a long eval body kept in a file
unity command console --level error
```

## `eval` recipes

Save every open scene before anything that reloads the domain:

```csharp
return UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
```

Open a scene, **then** load assets — `OpenScene` unloads what an earlier line of the same call loaded:

```csharp
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/<Root>/Scenes/MainGame.unity");
var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>("Assets/<Root>/Data/X.asset");
return settings != null;
```

Reimport a prefab before editing it — the in-memory copy can be stale, and the stale copy wins on
the next domain reload:

```csharp
UnityEditor.AssetDatabase.ImportAsset("Assets/<Root>/Prefabs/Foo.prefab", UnityEditor.ImportAssetOptions.ForceUpdate);
var root = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/<Root>/Prefabs/Foo.prefab");
// edit root...
UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, "Assets/<Root>/Prefabs/Foo.prefab");
UnityEditor.PrefabUtility.UnloadPrefabContents(root);
return "saved";
```

Create a ScriptableObject asset (Unity writes the `.meta`; never write one by hand):

```csharp
var so = UnityEngine.ScriptableObject.CreateInstance("<Game>.Game.Data.FooSettingsSO");
UnityEditor.AssetDatabase.CreateAsset(so, "Assets/<Root>/Data/FooSettings.asset");
UnityEditor.AssetDatabase.SaveAssets();
return UnityEditor.AssetDatabase.GetAssetPath(so);
```

Rename or move an asset (keeps the GUID and the `.meta`):

```csharp
return UnityEditor.AssetDatabase.MoveAsset("Assets/<Root>/Scripts/Game/Services/I_Foo.cs", "Assets/<Root>/Scripts/Game/Services/IFoo.cs");
```

Snapshot and restore PlayerPrefs around a play-test (keys from the project card):

```csharp
return string.Join(",", new[] { "<Game>.Lives", "<Game>.Coins" }.Select(k => k + "=" + UnityEngine.PlayerPrefs.GetInt(k, -1)));
```

```csharp
UnityEngine.PlayerPrefs.SetInt("<Game>.Lives", 5); UnityEngine.PlayerPrefs.Save(); return "restored";
```

Compile-check code for another platform or a doc sketch without switching the build target. The
builder must use engine modules (LitMotion and R3 were compiled against them) and reference the
game's non-Unity dependencies; the result lands in a `.log` next to the DLL a few seconds later:

```csharp
var dir = "/absolute/path/to/scratch/"; var outDll = dir + "Check.dll";
var builder = new UnityEditor.Compilation.AssemblyBuilder(outDll, System.IO.Directory.GetFiles(dir, "*.cs"));
builder.buildTarget = UnityEditor.BuildTarget.iOS;   // or EditorUserBuildSettings.activeBuildTarget
builder.buildTargetGroup = UnityEditor.BuildPipeline.GetBuildTargetGroup(builder.buildTarget);
builder.referencesOptions = UnityEditor.Compilation.ReferencesOptions.UseEngineModules;
var refs = new System.Collections.Generic.List<string>();
foreach (var a in UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Player))
    if (a.name == "<Game>.Game") { foreach (var c in a.compiledAssemblyReferences) { var n = System.IO.Path.GetFileName(c); if (!n.StartsWith("UnityEngine") && !n.StartsWith("UnityEditor")) refs.Add(c); } foreach (var r in a.assemblyReferences) refs.Add(r.outputPath); }
builder.additionalReferences = refs.ToArray();
var log = new System.Text.StringBuilder();
builder.buildFinished += (path, msgs) => { foreach (var m in msgs) if (m.type == UnityEditor.Compilation.CompilerMessageType.Error) log.AppendLine(m.message); System.IO.File.WriteAllText(outDll + ".log", log.Length == 0 ? "OK" : log.ToString()); };
return builder.Build() ? "started" : "not started";
```

## Gotchas and why

| Symptom | Cause | Do |
| --- | --- | --- |
| Scene edits vanished after `recompile` | domain reload reverts unsaved scenes | `SaveOpenScenes()` first |
| `eval` fails with `Main thread operation timed out after 5000ms` | hard 5 s cap; Editor was busy | chunk the work; check `editor_status`; retry |
| `eval` cannot find a type | no `using` in a method body | fully-qualify |
| Asset loaded in `eval` is null after `OpenScene` | opening a scene unloads loaded assets | open first, load after |
| Prefab edit lost on next reload | stale AssetDatabase copy overwrote the file | `ImportAsset(..., ForceUpdate)` before editing |
| `* SDF.asset` modified after Play | TMP rewrote the dynamic atlas | `git checkout -- <atlas>` before commit |
| Lives / boosters / level changed after Play | play-testing spends PlayerPrefs | snapshot before, restore after (or the debug windows) |
| Overlay UI missing or squashed in a capture | `--source camera`, or wrong size | `--source screen` in Play mode at the card's size |
| Base64 blob in the response | no `--save_path` | always pass `--save_path` |
| `#if UNITY_IOS` code never compiled | Editor compiles the active target only | `AssemblyBuilder` with `buildTarget = iOS` |
| Another session's edits disappeared | shared working tree and Editor | re-read before editing; stage only your paths |
