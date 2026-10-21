using System;
using Game.Core.Domain;

namespace Game.Core.Services
{
    // Kho booster của người chơi. Core chỉ biết interface; lưu ở đâu (PlayerPrefs, file, server)
    // là việc của Game — y hệt ICoinService và ILifeService.
    //
    // Số lượng không bao giờ âm: Add bỏ qua số <= 0, TrySpend trả false khi không đủ thay vì trừ
    // lố. Nhờ vậy chỗ gọi không phải tự kiểm tra trước.
    public interface IBoosterInventoryService
    {
        int GetCount(BoosterType type);

        // Bắn ra (loại, số lượng MỚI) sau mỗi lần đổi — view chỉ cần nghe sự kiện này để hiển
        // thị, không phải poll mỗi frame.
        event Action<BoosterType, int> CountChanged;

        void Add(BoosterType type, int amount);

        bool TrySpend(BoosterType type, int amount);
    }
}
