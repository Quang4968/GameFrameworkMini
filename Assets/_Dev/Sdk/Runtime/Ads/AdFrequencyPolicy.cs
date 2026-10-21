namespace MoodBoost.Sdk.Ads
{
    // The "is showing allowed yet" rule for interstitial and app open. Pure C# plus ISdkClock and IPlayerLevel, so it is
    // testable with fakes without opening the Editor.
    //
    // Rewarded is NEVER blocked here: it is something the player taps themselves in exchange for a reward, and
    // blocking it blocks the best-earning format while making the button inexplicably "not work".
    public sealed class AdFrequencyPolicy
    {
        readonly ISdkClock clock;
        readonly IPlayerLevel playerLevel;

        // Where InterstitialFirstCooldownSeconds counts from. AdService is built once at app launch.
        readonly double startedAt;

        // NOT readonly: when remote config changes, AdService pushes the new numbers down here.
        AdPolicySettings settings;

        // Initialized to negative infinity so the FIRST show is not blocked by the cooldown itself.
        double lastInterstitialAt = double.NegativeInfinity;
        double lastRewardedAt = double.NegativeInfinity;
        double lastAppOpenAt = double.NegativeInfinity;

        public AdFrequencyPolicy(ISdkClock clock, IPlayerLevel playerLevel, AdPolicySettings settings)
        {
            this.clock = clock;
            this.playerLevel = playerLevel;
            this.settings = settings;
            startedAt = clock.RealtimeSeconds;
        }

        // Change the rules while KEEPING the recorded timestamps. Rebuilding the object instead of calling this would
        // reset lastInterstitialAt to negative infinity - meaning every remote config change hands the player an
        // immediate interstitial regardless of having just watched one.
        public void ApplySettings(in AdPolicySettings value)
        {
            settings = value;
        }

        public bool CanShow(AdFormat format)
        {
            var now = clock.RealtimeSeconds;

            switch (format)
            {
                case AdFormat.Interstitial:
                    if (playerLevel != null
                        && playerLevel.TryGetCurrent(out var level)
                        && level < settings.InterstitialFirstLevel)
                    {
                        return false;
                    }

                    if (double.IsNegativeInfinity(lastInterstitialAt)
                        && now - startedAt < settings.InterstitialFirstCooldownSeconds)
                    {
                        return false;
                    }

                    return now - lastInterstitialAt >= settings.InterstitialCooldownSeconds
                        && now - lastRewardedAt >= settings.InterstitialCooldownAfterRewardedSeconds;

                case AdFormat.AppOpen:
                    return now - lastAppOpenAt >= settings.AppOpenCooldownSeconds;

                default:
                    return true;
            }
        }

        // Call after EVERY completed show, including when the player skips: once the screen has been taken over, the
        // cooldown has to run, otherwise skipping and tapping again serves another ad instantly.
        public void NotifyShown(AdFormat format)
        {
            var now = clock.RealtimeSeconds;

            switch (format)
            {
                case AdFormat.Interstitial:
                    lastInterstitialAt = now;
                    break;
                case AdFormat.Rewarded:
                    lastRewardedAt = now;
                    break;
                case AdFormat.AppOpen:
                    lastAppOpenAt = now;
                    break;
            }
        }
    }
}
