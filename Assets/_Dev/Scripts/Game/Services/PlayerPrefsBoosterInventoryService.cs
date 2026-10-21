using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Services;
using Game.Game.Data;
using UnityEngine;

namespace Game.Game.Services
{
    // Lưu số booster vào PlayerPrefs, mỗi loại một key.
    //
    // Key bám theo TÊN của BoosterType chứ không phải số thứ tự: đảo chỗ trong enum sau này thì
    // kho vẫn về đúng loại, thay vì âm thầm biến shuffle thành undo. Đổi tên một giá trị enum thì
    // kho của loại đó coi như mới — chấp nhận, vì đó là lần sửa hiếm và thấy được ngay.
    //
    // Số lượng giữ trong bộ nhớ và chỉ ghi xuống lúc đổi: HUD đọc GetCount mỗi frame cũng không
    // tốn gì. Cùng khuôn với PlayerPrefsCoinService.
    public sealed class PlayerPrefsBoosterInventoryService : IBoosterInventoryService
    {
        public const string PrefsKeyPrefix = "Game.Booster.";

        readonly Dictionary<BoosterType, int> counts = new();

        public event Action<BoosterType, int> CountChanged;

        public PlayerPrefsBoosterInventoryService(BoosterSettingsSO settings)
        {
            foreach (BoosterType type in Enum.GetValues(typeof(BoosterType)))
            {
                var starting = settings != null ? settings.StartingCount(type) : 0;

                // Máy chưa chơi bao giờ thì lấy số khởi đầu; máy đã có kho thì giữ nguyên.
                // Mathf.Max chặn trường hợp key bị sửa tay thành số âm.
                counts[type] = Mathf.Max(0, PlayerPrefs.GetInt(KeyOf(type), Mathf.Max(0, starting)));
            }
        }

        public int GetCount(BoosterType type) => counts.TryGetValue(type, out var count) ? count : 0;

        public void Add(BoosterType type, int amount)
        {
            if (amount <= 0) return;

            SetCount(type, GetCount(type) + amount);
        }

        public bool TrySpend(BoosterType type, int amount)
        {
            if (amount <= 0) return false;

            var count = GetCount(type);
            if (count < amount) return false;

            SetCount(type, count - amount);
            return true;
        }

        static string KeyOf(BoosterType type) => PrefsKeyPrefix + type;

        void SetCount(BoosterType type, int value)
        {
            counts[type] = value;
            PlayerPrefs.SetInt(KeyOf(type), value);

            // Save ngay thay vì đợi Unity tự ghi lúc thoát: game mobile bị kill nền bất cứ lúc
            // nào, mất booster vừa mua là lỗi người chơi thấy liền.
            PlayerPrefs.Save();

            CountChanged?.Invoke(type, value);
        }
    }
}
