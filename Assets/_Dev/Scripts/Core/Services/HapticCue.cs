namespace Game.Core.Services
{
    // Ý nghĩa của một cú rung, không phải thông số rung thật.
    // Core chỉ nói "đây là cú chạm nhẹ / thắng màn"; dịch ra preset của từng nền tảng là việc của Game.
    public enum HapticCue
    {
        Selection,
        Success,
        Warning,
        Failure,
        LightImpact,
        MediumImpact,
        HeavyImpact,
        RigidImpact,
        SoftImpact
    }
}
