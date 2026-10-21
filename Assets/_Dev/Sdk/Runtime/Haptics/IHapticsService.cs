namespace MoodBoost.Sdk.Haptics
{
    // What the game calls to vibrate. Whether the player turned haptics off is the game's setting, so the caller
    // checks it; this layer only decides how the current platform vibrates.
    public interface IHapticsService
    {
        void Play(HapticKind kind);

        void Play(in HapticPattern pattern);

        // Dropped when the previous cooldown tap is younger than cooldownSeconds, so a burst of events reads as
        // separate taps instead of one long buzz.
        void PlayWithCooldown(HapticKind kind, float cooldownSeconds);
    }
}
