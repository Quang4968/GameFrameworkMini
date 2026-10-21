using System;
using Game.Core.Domain;
using Game.Core.Services;
using Game.Game.Data;
using Game.Game.Services;
using UnityEditor;
using UnityEngine;
using VContainer.Unity;

namespace Game.Editor.Windows
{
    // Bàn điều khiển để test tay hệ booster: xem kho và bơm/rút số lượng của từng loại.
    //
    // Lý do tồn tại giống LifeDebugWindow: hai nhánh của nút booster (còn hàng thì xáo, hết hàng
    // thì mở popup mua) chỉ khác nhau ở đúng một con số, mà chơi tay tới lúc cạn kho thì lâu.
    //
    // Lúc CHƯA chạy game thì làm việc thẳng trên PlayerPrefs. Lúc ĐANG chạy thì đi qua
    // IBoosterInventoryService của scene, vì service giữ kho trong RAM và chỉ ghi đĩa khi số lượng
    // đổi — sửa PlayerPrefs dưới chân nó sẽ bị nó ghi đè.
    //
    // Coin KHÔNG có ở đây: Game > Life Debug đã có ô đặt coin, và nút mua booster đọc đúng
    // cái ví đó.
    public sealed class BoosterDebugWindow : EditorWindow
    {
        BoosterSettingsSO settings;

        [MenuItem("Game/Booster Debug")]
        public static void Open()
        {
            GetWindow<BoosterDebugWindow>("Booster Debug").minSize = new Vector2(320f, 260f);
        }

        // Số lượng đổi được từ trong game (bấm Shuffle, mua trong popup) chứ không chỉ từ cửa sổ
        // này, nên phải vẽ lại đều thay vì đợi người dùng rê chuột.
        void OnEnable()
        {
            EditorApplication.update += Repaint;
            settings = LoadSettings();
        }

        void OnDisable() => EditorApplication.update -= Repaint;

        void OnGUI()
        {
            var inventory = RuntimeInventory();

            EditorGUILayout.LabelField("Trạng thái", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Nguồn", inventory != null
                    ? "IBoosterInventoryService (đang chạy)"
                    : "PlayerPrefs (chưa chạy game)");
                EditorGUILayout.TextField("Coin", PlayerPrefs.GetInt("Game.Coins", 0).ToString());
            }

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "Không tìm thấy BoosterSettings.asset — cửa sổ vẫn sửa được kho, chỉ là không hiện giá.",
                    MessageType.Info);
            }

            foreach (BoosterType type in Enum.GetValues(typeof(BoosterType)))
            {
                EditorGUILayout.Space();
                DrawBooster(inventory, type);
            }

            EditorGUILayout.Space();
            DrawReset(inventory);
        }

        void DrawBooster(IBoosterInventoryService inventory, BoosterType type)
        {
            var count = inventory != null
                ? inventory.GetCount(type)
                : PlayerPrefs.GetInt(KeyOf(type), 0);

            var title = settings != null ? settings.DisplayName(type) : type.ToString();
            EditorGUILayout.LabelField($"{title} — đang có {count}", EditorStyles.boldLabel);

            if (settings != null)
            {
                EditorGUILayout.LabelField(
                    $"{settings.CoinCost(type)} coin cho {settings.AmountPerPurchase(type)} cái",
                    EditorStyles.miniLabel);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("−1")) Change(inventory, type, -1);
                if (GUILayout.Button("+1")) Change(inventory, type, 1);
                if (GUILayout.Button("+10")) Change(inventory, type, 10);
                if (GUILayout.Button("Về 0")) Change(inventory, type, -count);
            }
        }

        void DrawReset(IBoosterInventoryService inventory)
        {
            EditorGUILayout.LabelField("Dọn dẹp", EditorStyles.boldLabel);

            if (!GUILayout.Button("Xoá sạch kho booster")) return;

            foreach (BoosterType type in Enum.GetValues(typeof(BoosterType))) PlayerPrefs.DeleteKey(KeyOf(type));
            PlayerPrefs.Save();

            if (inventory != null)
            {
                EditorUtility.DisplayDialog("Booster Debug",
                    "Đã xoá PlayerPrefs. Service đang chạy vẫn giữ kho cũ trong RAM — thoát Play Mode rồi " +
                    "vào lại để thấy trạng thái máy mới cài.",
                    "OK");
            }
        }

        // Interface không có Set, nên đi bằng cộng/trừ để sự kiện CountChanged vẫn bắn và nút trên
        // HUD cập nhật ngay — cùng lối với ô đặt coin của LifeDebugWindow.
        static void Change(IBoosterInventoryService inventory, BoosterType type, int delta)
        {
            if (delta == 0) return;

            if (inventory != null)
            {
                if (delta > 0) inventory.Add(type, delta);
                else inventory.TrySpend(type, -delta);
                return;
            }

            var value = Mathf.Max(0, PlayerPrefs.GetInt(KeyOf(type), 0) + delta);
            PlayerPrefs.SetInt(KeyOf(type), value);
            PlayerPrefs.Save();
        }

        static string KeyOf(BoosterType type) => PlayerPrefsBoosterInventoryService.PrefsKeyPrefix + type;

        static IBoosterInventoryService RuntimeInventory()
        {
            if (!Application.isPlaying) return null;

            foreach (var scope in FindObjectsByType<LifetimeScope>(FindObjectsSortMode.None))
            {
                if (scope.Container == null) continue;

                try
                {
                    return (IBoosterInventoryService)scope.Container.Resolve(typeof(IBoosterInventoryService), null);
                }
                catch (Exception)
                {
                    // Scope của scene này không đăng ký booster (Boot, Splash) — thử scope kế tiếp.
                }
            }

            return null;
        }

        static BoosterSettingsSO LoadSettings()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:BoosterSettingsSO"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<BoosterSettingsSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) return asset;
            }

            return null;
        }
    }
}
