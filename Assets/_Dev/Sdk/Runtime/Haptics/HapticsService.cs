namespace MoodBoost.Sdk.Haptics
{
    // Forwards to the platform provider and owns the cooldown, so every provider gets the same throttling and none
    // has to keep its own clock. Realtime, not scaled time: a popup sets timeScale to 0 and taps must still space out.
    public sealed class HapticsService : IHapticsService
    {
        readonly IHapticsProvider provider;
        readonly ISdkClock clock;

        double lastCooldownTapAt = double.NegativeInfinity;

        public HapticsService(IHapticsProvider provider, ISdkClock clock)
        {
            this.provider = provider ?? new NullHapticsProvider();
            this.clock = clock;
        }

        public void Play(HapticKind kind) => provider.Play(kind);

        public void Play(in HapticPattern pattern) => provider.Play(pattern);

        public void PlayWithCooldown(HapticKind kind, float cooldownSeconds)
        {
            var now = clock.RealtimeSeconds;
            if (now - lastCooldownTapAt < cooldownSeconds) return;

            lastCooldownTapAt = now;
            provider.Play(kind);
        }
    }
}
