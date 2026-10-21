namespace MoodBoost.Sdk.Haptics
{
    // One pulse of a pattern. Kind drives preset-only platforms (iOS); duration and amplitude (0-255) drive waveform
    // platforms (Android). DelayMs is measured from the start of the pattern.
    public readonly struct HapticPulse
    {
        public readonly HapticKind Kind;
        public readonly long DelayMs;
        public readonly long DurationMs;
        public readonly int Amplitude;

        public HapticPulse(HapticKind kind, long delayMs, long durationMs, int amplitude)
        {
            Kind = kind;
            DelayMs = delayMs;
            DurationMs = durationMs;
            Amplitude = amplitude;
        }
    }
}
