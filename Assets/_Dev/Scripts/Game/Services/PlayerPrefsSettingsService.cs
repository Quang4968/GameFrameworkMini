using System;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Services
{
    // Lưu âm lượng nhạc/hiệu ứng và cờ rung vào PlayerPrefs.
    // Mặc định âm lượng đầy, rung bật, mức rung Normal.
    public sealed class PlayerPrefsSettingsService : ISettingsService
    {
        public const string MusicVolumePrefsKey = "Game.MusicVolume";
        public const string SoundVolumePrefsKey = "Game.SoundVolume";
        public const string HapticsPrefsKey = "Game.HapticsEnabled";
        public const string HapticStrengthPrefsKey = "Game.HapticStrength";

        public float MusicVolume { get; private set; }
        public float SoundVolume { get; private set; }
        public bool HapticsEnabled { get; private set; }
        public HapticStrength HapticStrength { get; private set; }

        public event Action<float> MusicVolumeChanged;
        public event Action<float> SoundVolumeChanged;
        public event Action<bool> HapticsEnabledChanged;
        public event Action<HapticStrength> HapticStrengthChanged;

        public PlayerPrefsSettingsService()
        {
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumePrefsKey, 1f));
            SoundVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SoundVolumePrefsKey, 1f));
            HapticsEnabled = PlayerPrefs.GetInt(HapticsPrefsKey, 1) != 0;
            HapticStrength = ClampStrength(PlayerPrefs.GetInt(HapticStrengthPrefsKey, (int)HapticStrength.Normal));
        }

        public void SetMusicVolume(float volume)
        {
            var clamped = Mathf.Clamp01(volume);
            if (Mathf.Approximately(MusicVolume, clamped)) return;

            MusicVolume = clamped;
            PlayerPrefs.SetFloat(MusicVolumePrefsKey, clamped);
            PlayerPrefs.Save();
            MusicVolumeChanged?.Invoke(clamped);
        }

        public void SetSoundVolume(float volume)
        {
            var clamped = Mathf.Clamp01(volume);
            if (Mathf.Approximately(SoundVolume, clamped)) return;

            SoundVolume = clamped;
            PlayerPrefs.SetFloat(SoundVolumePrefsKey, clamped);
            PlayerPrefs.Save();
            SoundVolumeChanged?.Invoke(clamped);
        }

        public void SetHapticsEnabled(bool enabled)
        {
            if (HapticsEnabled == enabled) return;

            HapticsEnabled = enabled;
            PlayerPrefs.SetInt(HapticsPrefsKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            HapticsEnabledChanged?.Invoke(enabled);
        }

        public void SetHapticStrength(HapticStrength strength)
        {
            HapticStrength clamped = ClampStrength((int)strength);
            if (HapticStrength == clamped) return;

            HapticStrength = clamped;
            PlayerPrefs.SetInt(HapticStrengthPrefsKey, (int)clamped);
            PlayerPrefs.Save();
            HapticStrengthChanged?.Invoke(clamped);
        }

        static HapticStrength ClampStrength(int value)
        {
            return (HapticStrength)Mathf.Clamp(value, (int)HapticStrength.Normal, (int)HapticStrength.VeryStrong);
        }
    }
}
