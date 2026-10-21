namespace Game.Core.Services
{
    // Phát rung theo ý nghĩa (HapticCue). Cờ bật/tắt và mức rung nằm ở ISettingsService,
    // implementation tự đọc từ đó nên nơi gọi chỉ cần nói muốn rung kiểu gì.
    public interface IHapticService
    {
        void Play(HapticCue cue);

        // Bỏ qua nếu vừa rung xong trong khoảng cooldownSeconds. Dùng cho event bắn liên tục
        // (kéo piece, phá khối) để máy không rung dính thành một khối.
        void PlayWithCooldown(HapticCue cue, float cooldownSeconds);
    }
}
