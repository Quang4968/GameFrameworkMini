using System;
using Game.Core.Domain;
using UnityEngine;

namespace Game.Game.Data
{
    // Thông số booster dùng CHUNG cho cả game — chỉnh ở asset là mọi scene đổi theo, không scene
    // nào được tự khai con số riêng. Cùng vai trò với LifeSettingsSO và CoinSettingsSO.
    [CreateAssetMenu(menuName = "Game/Booster Settings", fileName = "BoosterSettings")]
    public sealed class BoosterSettingsSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public BoosterType type;

            [Tooltip("Tên hiện trên nút và trên popup mua. Bỏ trống thì dùng luôn tên của enum.")]
            public string displayName;

            [Tooltip("Icon hiện trên popup mua booster. Nút HUD và tấm hướng dẫn gán art thẳng " +
                "trong prefab của chúng.")]
            public Sprite icon;

            [Tooltip("Số booster có sẵn lúc mở game lần đầu. Đổi số này KHÔNG cộng thêm cho máy đã " +
                "chơi rồi vì số lượng đã nằm trong PlayerPrefs.")]
            [Min(0)] public int startingCount;

            [Tooltip("Số coin cho một lô. 0 = tắt đường mua bằng coin của loại này.")]
            [Min(0)] public int coinCost;

            [Tooltip("Số booster nhận được mỗi lần mua.")]
            [Min(1)] public int amountPerPurchase;
        }

        [Tooltip("Mỗi loại booster một dòng. Loại không có dòng nào coi như giá 0 và kho rỗng — " +
            "nút của nó vẫn hiện nhưng không mua được.")]
        [SerializeField] Entry[] boosters = new Entry[0];

        public Entry GetEntry(BoosterType type)
        {
            foreach (var entry in boosters)
            {
                if (entry.type == type) return entry;
            }

            // Thiếu dòng thì trả về lô rỗng chứ không ném lỗi: quên khai một loại chỉ làm nút đó
            // không mua được, không làm vỡ cả scene — cùng lối xử lý với PopupCatalogSO.
            return new Entry { type = type, amountPerPurchase = 1 };
        }

        // Tên hiện ra cho người chơi. Bỏ trống trong asset thì rơi về tên enum, để nút mới thêm
        // vẫn có chữ chứ không trống trơn.
        public string DisplayName(BoosterType type)
        {
            var name = GetEntry(type).displayName;
            return string.IsNullOrWhiteSpace(name) ? type.ToString() : name;
        }

        public Sprite Icon(BoosterType type) => GetEntry(type).icon;

        public int StartingCount(BoosterType type) => GetEntry(type).startingCount;
        public int CoinCost(BoosterType type) => GetEntry(type).coinCost;
        public int AmountPerPurchase(BoosterType type) => GetEntry(type).amountPerPurchase;
    }
}
