using System;

namespace Game.Core.Services
{
    // Số tim (lượt chơi) của người chơi. Core chỉ biết interface; lưu ở đâu (PlayerPrefs, file,
    // server) là việc của Game — y hệt ICoinService và ILevelProgressService.
    //
    // Số tim luôn nằm trong [0, MaxLives]: Add kẹp trần thay vì cộng lố, TrySpend trả false khi
    // không đủ thay vì trừ xuống âm. Nhờ vậy chỗ gọi không phải tự kiểm tra trước.
    public interface ILifeService
    {
        int Lives { get; }

        int MaxLives { get; }

        // Thời gian còn lại tới lúc được cộng tim tiếp theo; TimeSpan.Zero khi đầy tim hoặc hồi
        // tim đang tắt. Cố tình là KHOẢNG THỜI GIAN chứ không phải mốc UTC: view chỉ việc format
        // ra mm:ss, không phải trừ cho đồng hồ máy — mà đồng hồ máy thì người chơi vặn được.
        TimeSpan TimeUntilNextLife { get; }

        // Bắn ra số tim MỚI sau mỗi lần đổi — view chỉ cần nghe sự kiện này để hiển thị, không
        // phải poll mỗi frame.
        event Action<int> LivesChanged;

        // Zero when no unlimited window runs. While one does, entering a level costs no life.
        TimeSpan UnlimitedTimeLeft { get; }

        // True when a window starts, false when it runs out; extending a running window stays silent.
        event Action<bool> UnlimitedChanged;

        void Add(int amount);

        // Stacks onto a running window and counts down in real time, app closed or not (UnlimitedLifeRules).
        void AddUnlimited(TimeSpan duration);

        bool TrySpend(int amount);

        // Tính lại tim đã hồi theo thời gian thật trôi qua kể từ lần đọc gần nhất. Service không tự
        // poll nền — chỗ nào cần số mới nhất (mở scene, mở popup hết tim, mỗi giây lúc popup đang
        // đếm ngược) tự gọi hàm này trước khi đọc Lives/TimeUntilNextLife.
        void Refresh();
    }
}
