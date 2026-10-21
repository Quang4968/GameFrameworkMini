// unity command eval --code "$(cat add-catalog-entry.eval.cs)" after replacing the three placeholders.
// Appends one row to the popup catalog through SerializedObject so the .asset stays Editor-owned.
var catalogPath = "<CatalogAssetPath>";          // e.g. the "Pool catalogs" neighbour in Data/: PopupCatalog.asset
var prefabPath = "<PrefabAssetPath>";            // the new popup prefab (already Addressable)
var popupId = (int)System.Enum.Parse(System.Type.GetType("<Game>.Game.Views.Popup.PopupId, <Game>.Game"), "<Name>");

var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.ScriptableObject>(catalogPath);
if (catalog == null) return "catalog not found: " + catalogPath;
var guid = UnityEditor.AssetDatabase.AssetPathToGUID(prefabPath);
if (string.IsNullOrEmpty(guid)) return "prefab not found: " + prefabPath;

var so = new UnityEditor.SerializedObject(catalog);
var popups = so.FindProperty("popups");
for (var i = 0; i < popups.arraySize; i++)
    if (popups.GetArrayElementAtIndex(i).FindPropertyRelative("id").intValue == popupId) return "already listed";

popups.arraySize += 1;
var entry = popups.GetArrayElementAtIndex(popups.arraySize - 1);
entry.FindPropertyRelative("id").intValue = popupId;
entry.FindPropertyRelative("prefab").FindPropertyRelative("m_AssetGUID").stringValue = guid;
entry.FindPropertyRelative("prefab").FindPropertyRelative("m_SubObjectName").stringValue = "";
so.ApplyModifiedProperties();
UnityEditor.EditorUtility.SetDirty(catalog);
UnityEditor.AssetDatabase.SaveAssets();
return "added " + popupId + " -> " + guid;
