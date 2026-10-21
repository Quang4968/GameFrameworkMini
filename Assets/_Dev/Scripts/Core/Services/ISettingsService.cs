using System;

namespace Game.Core.Services
{
    // Âm lượng nhạc nền, âm lượng hiệu ứng và cờ rung của người chơi.
    // Core chỉ biết interface; lưu ở đâu (PlayerPrefs, file, server) là việc của Game.
    public interface ISettingsService
    {
        // Âm lượng nằm trong [0, 1]; 0 nghĩa là tắt hẳn, không có cờ bật/tắt riêng.
        float MusicVolume { get; }
        float SoundVolume { get; }
        bool HapticsEnabled { get; }
        HapticStrength HapticStrength { get; }

        event Action<float> MusicVolumeChanged;
        event Action<float> SoundVolumeChanged;
        event Action<bool> HapticsEnabledChanged;
        event Action<HapticStrength> HapticStrengthChanged;

        void SetMusicVolume(float volume);
        void SetSoundVolume(float volume);
        void SetHapticsEnabled(bool enabled);
        void SetHapticStrength(HapticStrength strength);
    }
}
