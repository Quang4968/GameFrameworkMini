using System;

namespace Game.Core.GameLogic
{
    // Chốt chặn giữa đồng hồ máy và luật hồi tim: game offline nên không có giờ server để đối
    // chiếu, mọi thứ phải suy ra từ chính con số người chơi có thể vặn được.
    //
    // Cách làm là giữ MỐC CAO NHẤT từng thấy: chỉ công nhận thời gian trôi về phía trước, và khi
    // giờ máy tụt lùi thì GIỮ NGUYÊN mốc cũ thay vì hạ theo. Hệ quả:
    //
    // - Vặn giờ LÙI: không hồi được giây nào, và mốc không tụt theo nên lúc chỉnh giờ về đúng vẫn
    //   không có tim miễn phí. Đây cũng là chỗ vá lỗi cũ — mốc tuyệt đối kiểu nextRefillUtc bị vặn
    //   lùi một năm là treo tim đúng một năm.
    // - Vặn giờ TỚI: có hồi tim thật, nhưng nhiều nhất là đầy bình (LifeRegenRules kẹp ở maxLives),
    //   và mốc nhảy theo lên tương lai nên sau khi chỉnh giờ về đúng thì tim ĐỨNG YÊN cho tới khi
    //   giờ thật đuổi kịp. Người chơi chỉ "vay trước" phần tim lẽ ra vẫn nhận được, không tạo thêm.
    //
    // Chặn triệt để cần giờ server; chừng nào chưa có server thì đây là mức tốt nhất mà một game
    // offline làm được, và nó không bao giờ ăn oan thời gian thật của người chơi ngay tình.
    public static class LifeClockGuard
    {
        // Trả về (thời gian được công nhận, mốc mới cần lưu lại).
        // anchorUtc = DateTime.MinValue nghĩa là chưa có mốc nào (máy mới cài) — lần đầu chỉ đặt
        // mốc chứ không công nhận giây nào, nếu không sẽ hồi tim theo cả quãng từ năm 0001.
        public static (TimeSpan elapsed, DateTime anchorUtc) Measure(DateTime anchorUtc, DateTime nowUtc)
        {
            if (anchorUtc == DateTime.MinValue) return (TimeSpan.Zero, nowUtc);

            if (nowUtc <= anchorUtc) return (TimeSpan.Zero, anchorUtc);

            return (nowUtc - anchorUtc, nowUtc);
        }
    }
}
