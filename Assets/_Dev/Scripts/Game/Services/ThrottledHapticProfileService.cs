using System.Collections.Generic;
using Game.Core.Services;
using Game.Game.Data;
using UnityEngine;

namespace Game.Game.Services
{
    // Cổng duy nhất cho mọi cú rung của gameplay: tra cường độ trong asset, giữ mốc thời gian
    // riêng cho từng profile, rồi mới đẩy xuống IHapticService.
    //
    // Why it keeps its own clock instead of IHapticService.PlayWithCooldown: that cooldown is ONE shared timer in
    // the SDK's HapticsService - it cannot be split per profile or tuned from the asset.
    public sealed class ThrottledHapticProfileService : IHapticProfileService
    {
        readonly IHapticService haptics;
        readonly HapticProfileSettingsSO settings;

        readonly Dictionary<HapticProfile, float> nextAllowedTimes = new Dictionary<HapticProfile, float>();

        public ThrottledHapticProfileService(IHapticService haptics, HapticProfileSettingsSO settings)
        {
            this.haptics = haptics;
            this.settings = settings;
        }

        public void Play(HapticProfile profile)
        {
            if (haptics == null || settings == null) return;
            if (!settings.TryGet(profile, out var entry) || !entry.Enabled) return;

            // unscaledTime chứ không time: rung là phản hồi cho ngón tay, không được đứng lại
            // theo timeScale khi popup dừng game.
            float now = Time.unscaledTime;
            if (nextAllowedTimes.TryGetValue(profile, out float nextAllowed) && now < nextAllowed) return;

            nextAllowedTimes[profile] = now + entry.MinInterval;
            haptics.Play(entry.Cue);
        }
    }
}
