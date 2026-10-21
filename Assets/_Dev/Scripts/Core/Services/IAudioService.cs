namespace Game.Core.Services
{
    // Phát tiếng theo ý nghĩa (AudioCue), song song với IHapticService. Âm lượng nhạc và hiệu
    // ứng nằm ở ISettingsService, implementation tự đọc từ đó nên nơi gọi chỉ cần nói muốn nghe
    // tiếng gì.
    //
    // Cố tình KHÔNG có tham số volume/pitch/cooldown: mọi thông số của từng cue là dữ liệu trên
    // asset bên Game, nhờ vậy chỉnh tiếng không phải sửa nơi gọi.
    public interface IAudioService
    {
        void Play(AudioCue cue);
    }
}
