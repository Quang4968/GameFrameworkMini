using System;

namespace Game.Core.GameLogic
{
    // Luật hồi tim theo thời gian. Hàm THUẦN: không đọc đồng hồ, không ghi PlayerPrefs — chỗ gọi
    // (PlayerPrefsLifeService) tự đo thời gian đã trôi qua rồi lưu lại kết quả.
    //
    // Đồng hồ được giữ dưới dạng THỜI GIAN CÒN LẠI chứ không phải mốc tuyệt đối: mốc tuyệt đối
    // (nextRefillUtc) sẽ sai ngay khi người chơi vặn giờ máy, còn thời gian còn lại thì chỉ phụ
    // thuộc vào lượng thời gian LifeClockGuard chịu công nhận.
    //
    // remaining = TimeSpan.Zero nghĩa là "không có đồng hồ nào đang chạy" (đầy tim, hoặc hồi tim
    // đang tắt). Đồng hồ chỉ bắt đầu chạy đúng lúc tim rơi xuống dưới trần — gọi Apply lúc đang
    // đầy tim không tự khởi động đếm ngược, tránh việc tim vừa mất 1 đã gần hồi lại ngay vì đồng
    // hồ chạy sẵn từ trước.
    public static class LifeRegenRules
    {
        public static (int lives, TimeSpan remaining) Apply(
            int lives, int maxLives, TimeSpan remaining, TimeSpan elapsed, TimeSpan refillInterval)
        {
            lives = Math.Min(lives, maxLives);

            if (lives >= maxLives || refillInterval <= TimeSpan.Zero)
                return (lives, TimeSpan.Zero);

            // Thiếu tim mà đồng hồ chưa chạy: khởi động NGAY BÂY GIỜ, tức là trọn một khoảng nữa
            // mới có tim — không tính ngược lại quãng thời gian trước lúc mất tim.
            if (remaining <= TimeSpan.Zero) remaining = refillInterval;

            if (elapsed > TimeSpan.Zero) remaining -= elapsed;

            // Vòng lặp thay vì trừ một lần: máy tắt qua nhiều khoảng refillInterval thì hồi đúng
            // số tim đã trôi qua, không chỉ đúng 1 tim dù offline bao lâu.
            while (remaining <= TimeSpan.Zero && lives < maxLives)
            {
                lives++;
                remaining += refillInterval;
            }

            return lives >= maxLives ? (lives, TimeSpan.Zero) : (lives, remaining);
        }

        // Time until lives reach the cap; Zero when already full or regen is off. `remaining` is TimeUntilNextLife.
        public static TimeSpan TimeUntilFull(int lives, int maxLives, TimeSpan remaining, TimeSpan refillInterval)
        {
            if (lives >= maxLives || refillInterval <= TimeSpan.Zero) return TimeSpan.Zero;

            var missing = maxLives - Math.Max(lives, 0);
            var next = remaining > TimeSpan.Zero ? remaining : refillInterval;

            return next + TimeSpan.FromTicks(refillInterval.Ticks * (missing - 1));
        }
    }
}
