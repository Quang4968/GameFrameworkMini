---
name: new-popup
description: Add a popup the project way - append PopupId, controller with wiring in Construct, prefab copied and made Addressable through the Editor, catalog row through eval, gate and scope registration. Use when adding a popup, dialog or modal.
argument-hint: <Name>
---

# /new-popup $ARGUMENTS

Popups have four traps `/new-view` cannot know: the enum is index-stored, the catalog is edited
through the Editor, the prefab must be Addressable, and `OnEnable` fires on prewarm. Read
`docs/popups.md` (`limit: 20`) first. Templates in `${CLAUDE_SKILL_DIR}/templates/`.

## 1. Plan, stop

List: the new `PopupId` member, the controller file
(`Scripts/Game/Views/Popup/Popup<Name>Controller.cs`), the source prefab to copy the root from
(`Prefabs/UIPopup/PopupTest.prefab`, the base's bare popup: `PopupView` and an empty `Panel`; or
another popup of this game, never its content), the UI spec from `/ui-from-demo` steps 1–6 (demo, asset list, layout, text and button
tables), the gate if logic opens it, the scopes that host a `PopupHost` where it can open. Stop for
the go-ahead.

## 2. Enum

Append `<Name>` to `PopupId` **after the last implicit member**, keeping any explicit sentinel
(`Test = 100`) last. Never insert in the middle: `PopupCatalogSO` stores the id by index.

## 3. Controller

From `PopupController.cs.tmpl`:

- all wiring in `[Inject] Construct` (runs once, after `resolver.Instantiate`), unsubscribed in
  `OnDestroy`;
- work that needs the popup on screen listens to `PopupView.Opened`, not `OnEnable` (prewarm and
  `SetActive` also fire `OnEnable`);
- motion on `MotionScheduler.UpdateIgnoreTimeScale` — a popup freezes `Time.timeScale`;
- dependencies declared here, never added to `PopupHost`;
- buttons that open or close popups do not call `ButtonSounds`; `PopupService` plays the cue.

## 4. Prefab

Through `unity command eval`, never by editing YAML:

1. `AssetDatabase.CopyAsset(source, "<Prefabs>/UIPopup/<Name>.prefab")`, then
   `AssetDatabase.ImportAsset(path, ForceUpdate | ForceSynchronousImport)`;
2. `PrefabUtility.LoadPrefabContents` → add `Popup<Name>Controller` to the root → set the `Panel`
   `Image` to the game's popup frame (the first popup takes it from its demo, Sliced, white; later
   ones reuse it, and a demo drawing another frame is raised in the spec) → delete the source's content
   children and build the content from the approved spec (`/ui-from-demo` step 7) → wire references
   → `SaveAsPrefabAsset` → `UnloadPrefabContents` → `ImportAsset` again with
   the same options. Grep the `.prefab` for the change, and grep again after the next Play mode: a
   stale in-memory copy otherwise overwrites the file on entering Play, silently;
3. make it Addressable in the sibling popups' group, with an address in their pattern:
   `AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group)`.

## 5. Catalog

Run `templates/add-catalog-entry.eval.cs` (replace the placeholders): it grows the `popups` array
of the catalog asset with a `SerializedObject`, sets the id and the `AssetReference` GUID, applies
and saves. Never hand-edit the `.asset`.

## 6. Opener and registration

- Opened from logic → one gate class (`<Name>Gate`, pattern: the existing gates in
  `Views/Popup/`) registered by reference with a null guard; opened from a button → the button
  calls `IPopupService.OpenAsync(PopupId.<Name>, ct)` through a small component.
- Register the controller's dependencies in every scope whose `PopupHost` can show this popup.

## 7. Verify

`/verify`, then `/ui-from-demo` steps 8–9 (side-by-side compare with the demo, report with the
image). Add a bullet to `docs/popups.md` only for a new invariant (the lookup row already covers
`Views/Popup/`).
