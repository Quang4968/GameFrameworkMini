using System;
using Game.Core.Services;

namespace Game.Game.Services
{
    // Bọc đồng hồ thật của máy sau interface — cùng lối tách Core/Game với mọi service khác.
    public sealed class SystemClockService : IClockService
    {
#if UNITY_EDITOR
        // Cửa tua giờ CHỈ có trong Editor, phục vụ công cụ test hệ tim (LifeDebugWindow): chờ thật
        // 30 phút mỗi lần kiểm tra một mốc hồi tim là không làm nổi.
        //
        // Nằm sau #if nên build thật không có dòng nào của nó — không có đường nào để lọt ra ngoài
        // thành lỗ tua giờ. Cũng vì thế nó là static: công cụ Editor không đi qua DI được.
        public static TimeSpan DebugOffset = TimeSpan.Zero;

        public DateTime UtcNow => DateTime.UtcNow + DebugOffset;
#else
        public DateTime UtcNow => DateTime.UtcNow;
#endif
    }
}
