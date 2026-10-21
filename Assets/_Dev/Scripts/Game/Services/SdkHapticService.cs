using Game.Core.Services;
using MoodBoost.Sdk.Haptics;

namespace Game.Game.Services
{
    // Translates Core's HapticCue into the SDK's IHapticsService; the SDK picks the platform motor (MOST on mobile,
    // TTSDK on TikTok). The on/off flag and the strength belong to ISettingsService and are read on every call.
    //
    // Strength: iOS only has fixed presets, so stronger means a heavier preset; Android plays a waveform, so stronger
    // means longer and louder. A HapticPattern carries both, so Normal plays the plain preset and Strong/VeryStrong
    // play a pattern.
    public sealed class SdkHapticService : IHapticService
    {
        readonly ISettingsService settings;
        readonly IHapticsService haptics;

        public SdkHapticService(ISettingsService settings, IHapticsService haptics)
        {
            this.settings = settings;
            this.haptics = haptics;
        }

        public void Play(HapticCue cue)
        {
            if (!settings.HapticsEnabled) return;

            var strength = settings.HapticStrength;

            if (strength == HapticStrength.Normal)
            {
                haptics.Play(ToKind(cue));
                return;
            }

            haptics.Play(CreateStrengthPattern(cue, strength));
        }

        public void PlayWithCooldown(HapticCue cue, float cooldownSeconds)
        {
            if (!settings.HapticsEnabled) return;

            haptics.PlayWithCooldown(ToKind(ApplyStrength(cue, settings.HapticStrength)), cooldownSeconds);
        }

        // Moves a cue up to a heavier preset. Success/Warning/Failure have no heavier step and stay as they are.
        static HapticCue ApplyStrength(HapticCue cue, HapticStrength strength)
        {
            switch (strength)
            {
                case HapticStrength.Strong:
                    return cue switch
                    {
                        HapticCue.Selection => HapticCue.LightImpact,
                        HapticCue.LightImpact => HapticCue.MediumImpact,
                        HapticCue.MediumImpact => HapticCue.HeavyImpact,
                        HapticCue.RigidImpact => HapticCue.HeavyImpact,
                        HapticCue.SoftImpact => HapticCue.MediumImpact,
                        _ => cue
                    };
                case HapticStrength.VeryStrong:
                    return cue switch
                    {
                        HapticCue.Selection => HapticCue.MediumImpact,
                        HapticCue.LightImpact => HapticCue.HeavyImpact,
                        HapticCue.MediumImpact => HapticCue.HeavyImpact,
                        HapticCue.RigidImpact => HapticCue.HeavyImpact,
                        HapticCue.SoftImpact => HapticCue.HeavyImpact,
                        _ => cue
                    };
                default:
                    return cue;
            }
        }

        static HapticPattern CreateStrengthPattern(HapticCue cue, HapticStrength strength)
        {
            if (cue == HapticCue.Selection)
            {
                return strength == HapticStrength.VeryStrong
                    ? CreatePattern(HapticCue.MediumImpact, 95, 255)
                    : CreatePattern(HapticCue.LightImpact, 55, 175);
            }

            if (cue == HapticCue.RigidImpact)
            {
                return strength == HapticStrength.VeryStrong
                    ? CreatePattern(HapticCue.HeavyImpact, 170, 255, HapticCue.HeavyImpact, 65, 90)
                    : CreatePattern(HapticCue.HeavyImpact, 150, 255);
            }

            var strongerCue = ApplyStrength(cue, strength);
            return strength == HapticStrength.VeryStrong
                ? CreatePattern(strongerCue, 160, 255, HapticCue.HeavyImpact, 60, 80)
                : CreatePattern(strongerCue, 110, 230);
        }

        // Durations and delays in milliseconds, amplitude 0-255. The echo reuses the first pulse's amplitude.
        static HapticPattern CreatePattern(HapticCue primaryCue, long primaryDuration, int primaryAmplitude,
            HapticCue? secondaryCue = null, long secondaryDelay = 0, long secondaryDuration = 0)
        {
            var first = new HapticPulse(ToKind(primaryCue), 0, primaryDuration, primaryAmplitude);
            if (!secondaryCue.HasValue) return new HapticPattern(first);

            var second = new HapticPulse(ToKind(secondaryCue.Value), secondaryDelay, secondaryDuration, primaryAmplitude);
            return new HapticPattern(first, second);
        }

        static HapticKind ToKind(HapticCue cue)
        {
            switch (cue)
            {
                case HapticCue.Selection:
                    return HapticKind.Selection;
                case HapticCue.Success:
                    return HapticKind.Success;
                case HapticCue.Warning:
                    return HapticKind.Warning;
                case HapticCue.Failure:
                    return HapticKind.Failure;
                case HapticCue.LightImpact:
                    return HapticKind.LightImpact;
                case HapticCue.HeavyImpact:
                    return HapticKind.HeavyImpact;
                case HapticCue.RigidImpact:
                    return HapticKind.RigidImpact;
                case HapticCue.SoftImpact:
                    return HapticKind.SoftImpact;
                case HapticCue.MediumImpact:
                default:
                    return HapticKind.MediumImpact;
            }
        }
    }
}
