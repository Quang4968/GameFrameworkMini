namespace MoodBoost.Sdk.Haptics
{
    // A build with no haptics provider compiled in (or the Editor): every call is a no-op.
    public sealed class NullHapticsProvider : IHapticsProvider
    {
        public string Name => "None";

        public void Play(HapticKind kind)
        {
        }

        public void Play(in HapticPattern pattern)
        {
        }
    }
}
