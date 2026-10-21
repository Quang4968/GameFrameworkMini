---
name: playtest
description: Run a scene in Play mode through the Unity CLI, take correctly sized screenshots, and restore everything Play mode dirties (PlayerPrefs lives/boosters/progress, TMP font atlas). User-invoked.
argument-hint: [Scene] [--shot <label>]
disable-model-invocation: true
---

# /playtest $ARGUMENTS

Play mode is destructive in two ways the rules name: it spends persisted player state and it
rewrites the TMP font atlas. This flow brackets the run so the tree is clean afterwards. Scene names
and the "Play-testing consumes" keys come from the project card.

## 1. Before Play

1. `unity command editor_status` must be `ready`, play mode `stopped`.
2. `unity command save_scene` (or `eval` `EditorSceneManager.SaveOpenScenes()`): a recompile or a
   scene switch discards unsaved edits.
3. Snapshot the consumable keys with one `eval`:
   `return string.Join(",", new[]{ "<key1>", "<key2>" }.Select(k => k + "=" + UnityEngine.PlayerPrefs.GetInt(k, -1)));`
   (booster keys share a prefix; list the concrete ones the debug window shows).
4. `unity command open_scene --path "<Scenes>/<Scene>.unity"` when a scene was named; load any
   asset you need **after** this call.

## 2. Play and drive

- `unity command editor_play`, then poll `editor_status` until `playMode` is `playing`.
- Drive with `eval` in chunks under 5 s (find a component, call a public method, read state).
  Do not reach into services through reflection when a debug window or a public method exists.
- Screenshot: `unity command capture_game_view --source screen --width 1080 --height 1920
  --save_path "Temp/<label>.png"` (the card's portrait size; `--source camera` skips overlay UI).
  Read the PNG from disk with the `Read` tool, then remove it with
  `eval` `UnityEditor.AssetDatabase.DeleteAsset("Assets/Temp/<label>.png")` (drops the `.meta` too).

## 3. After Play

1. `unity command editor_stop`, poll until `stopped`.
2. Restore PlayerPrefs from the snapshot (`PlayerPrefs.SetInt` each key, then `PlayerPrefs.Save()`),
   or use the Life / Booster / Tutorial debug windows for the same keys.
3. `git status --porcelain` → `git checkout -- "<Fonts>/<font> SDF.asset"` for the dirtied atlas;
   flag anything else Play mode changed (`ProjectSettings/`, scene files) and ask before reverting.
4. Report: what was observed, screenshot findings, what was restored, anything left dirty.
