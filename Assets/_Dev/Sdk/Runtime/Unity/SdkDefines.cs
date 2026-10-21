namespace MoodBoost.Sdk.Unity
{
    // The define symbols of the SDK, kept in one place because the Editor window and the runtime code have to say the
    // same strings - one mistyped character and the window toggles one thing while the code reads another, with no
    // compile error to report it. All of them live on the WebGL target only: this SDK builds TikTok Mini Games.
    public static class SdkDefines
    {
        // TikTok Mini Game (TTSDK): the TikTok assembly compiles only with it (defineConstraints).
        public const string TikTok = "MB_TIKTOK";
        // The simulated network on a DEVICE build. It has no assembly of its own, so this symbol only SELECTS it in
        // SdkProviderCatalog; the Editor simulates either way.
        public const string FakeAds = "MB_ADS_FAKE";
        // Enables the SDK's Info logs. Off, the SdkLog.Info calls are stripped entirely from the build.
        public const string VerboseLog = "MB_SDK_LOG";
    }
}
