using System;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Services
{
    // Lưu số dư coin vào PlayerPrefs.
    //
    // Số dư được giữ trong bộ nhớ và chỉ ghi xuống PlayerPrefs lúc đổi: HUD đọc Coins mỗi frame
    // cũng không tốn gì.
    public sealed class PlayerPrefsCoinService : ICoinService
    {
        public const string PrefsKey = "Game.Coins";

        public int Coins { get; private set; }

        public event Action<int> CoinsChanged;

        public PlayerPrefsCoinService(int startingCoins = 0)
        {
            // Máy chưa chơi bao giờ thì lấy số coin khởi đầu; máy đã có số dư thì giữ nguyên.
            // Mathf.Max chặn trường hợp key bị sửa tay thành số âm.
            Coins = Mathf.Max(0, PlayerPrefs.GetInt(PrefsKey, Mathf.Max(0, startingCoins)));
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;

            SetCoins(Coins + amount);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return false;
            if (Coins < amount) return false;

            SetCoins(Coins - amount);
            return true;
        }

        void SetCoins(int value)
        {
            Coins = value;
            PlayerPrefs.SetInt(PrefsKey, value);

            // Save ngay thay vì đợi Unity tự ghi lúc thoát: game mobile bị kill nền bất cứ lúc
            // nào, mất coin vừa thưởng là lỗi người chơi thấy liền.
            PlayerPrefs.Save();

            CoinsChanged?.Invoke(value);
        }
    }
}
