using MoodBoost.Sdk.Unity;

namespace MoodBoost.Sdk.Editor
{
    // The rows of MoodBoost > SDK Modules. Every symbol is written to the WebGL target only.
    sealed class SdkModuleRow
    {
        public string Title;
        public string Symbol;
        public string Description;

        // The type standing in for the underlying SDK. Not found means that package has not been imported.
        public string ProbeType;
    }

    static class SdkModuleRows
    {
        public static readonly SdkModuleRow[] All =
        {
            new SdkModuleRow
            {
                Title = "TikTok Mini Game (TTSDK)",
                Symbol = SdkDefines.TikTok,
                Description = "Rewarded + interstitial, tracking and haptics inside TikTok. Install TTSDK through " +
                    "MoodBoost > TikTok Mini Game > Install TTSDK, then fill the TikTok block in SdkSettings.",
                ProbeType = "TTSDK.TT, ttsdk-editor",
            },
            new SdkModuleRow
            {
                Title = "Simulated ad network",
                Symbol = SdkDefines.FakeAds,
                Description = "Fake fill, fake latency and a placeholder panel. The Editor uses it either way, so this " +
                    "is the switch for a DEVICE build - it overrides TTSDK ads, TURN OFF before a release build.",
            },
            new SdkModuleRow
            {
                Title = "Verbose SDK logging",
                Symbol = SdkDefines.VerboseLog,
                Description = "Enables SdkLog.Info. Off, those calls are stripped entirely from the build.",
            },
        };
    }
}
