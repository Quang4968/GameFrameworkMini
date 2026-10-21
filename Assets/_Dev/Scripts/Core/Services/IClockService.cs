using System;

namespace Game.Core.Services
{
    // Đồng hồ thật của máy. Core chỉ biết interface — luật hồi tim (LifeRegenRules) nhờ vậy test
    // được bằng NUnit thuần với giờ giả, không phải mở Editor hay chờ thời gian thật trôi qua.
    public interface IClockService
    {
        DateTime UtcNow { get; }
    }
}
