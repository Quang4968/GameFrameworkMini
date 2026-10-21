namespace MoodBoost.Sdk.Haptics
{
    // The SDK's own vocabulary for one tap of the motor, named after the iOS feedback presets every provider maps
    // from. Kept apart from any game enum: the SDK cannot know a game's cue names.
    public enum HapticKind
    {
        Selection = 0,
        Success = 1,
        Warning = 2,
        Failure = 3,
        LightImpact = 4,
        MediumImpact = 5,
        HeavyImpact = 6,
        RigidImpact = 7,
        SoftImpact = 8,
    }
}
