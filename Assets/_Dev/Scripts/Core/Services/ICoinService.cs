using System;

namespace Game.Core.Services
{
    // Ví coin của người chơi. Core chỉ biết interface; lưu ở đâu (PlayerPrefs, file, server) là
    // việc của Game — y hệt ILevelProgressService và ISettingsService.
    //
    // Số dư không bao giờ âm: Add bỏ qua số <= 0, TrySpend trả false khi không đủ tiền thay vì
    // trừ lố. Nhờ vậy chỗ gọi không phải tự kiểm tra trước.
    public interface ICoinService
    {
        int Coins { get; }

        // Bắn ra số dư MỚI sau mỗi lần đổi — view chỉ cần nghe sự kiện này để hiển thị, không
        // phải poll mỗi frame.
        event Action<int> CoinsChanged;

        void Add(int amount);

        bool TrySpend(int amount);
    }
}
