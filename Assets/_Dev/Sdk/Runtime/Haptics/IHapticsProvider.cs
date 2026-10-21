namespace MoodBoost.Sdk.Haptics
{
    // One platform's motor. Adding a platform = implement this in a provider assembly gated by its define, add one
    // branch in SdkProviderCatalog.CreateHaptics. Cooldowns live in HapticsService, so a provider only vibrates.
    public interface IHapticsProvider
    {
        string Name { get; }

        void Play(HapticKind kind);

        void Play(in HapticPattern pattern);
    }
}
