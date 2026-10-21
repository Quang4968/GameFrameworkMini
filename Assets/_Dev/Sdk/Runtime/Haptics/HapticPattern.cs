namespace MoodBoost.Sdk.Haptics
{
    // A stronger-than-preset vibration: one pulse, or a pulse with an echo. Two at most because no caller needs more,
    // and a struct of two fields avoids an array allocation on every tap.
    public readonly struct HapticPattern
    {
        public readonly HapticPulse First;
        public readonly HapticPulse Second;
        public readonly bool HasSecond;

        public HapticPattern(HapticPulse first)
        {
            First = first;
            Second = default;
            HasSecond = false;
        }

        public HapticPattern(HapticPulse first, HapticPulse second)
        {
            First = first;
            Second = second;
            HasSecond = true;
        }
    }
}
