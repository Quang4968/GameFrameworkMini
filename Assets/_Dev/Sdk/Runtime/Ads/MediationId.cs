namespace MoodBoost.Sdk.Ads
{
    // The identifier of a mediation network. An enum rather than a string because it appears in the Inspector (the
    // waterfall order is declared by hand) and in revenue data - typing "levelpay" once loses a whole reporting branch
    // with nothing flagging the mistake.
    //
    // To add a network, add a value here; do NOT change the numbers of existing values: those numbers live in already
    // serialized assets, and changing them points every existing configuration at a different network.
    public enum MediationId
    {
        None = 0,
        LevelPlay = 1,

        // 2 was AdMob, removed along with its plugin. Leave the number retired rather than reusing it: an old asset
        // still holding a 2 would silently come back as whatever network took the value over.

        // The simulated network for the Editor. When enabled it replaces every real network.
        Fake = 3,

        // TikTok Mini Game ads (TTSDK) - a WebGL-only network with no auction of its own.
        TikTok = 4,
    }
}
