namespace Game.Core.Tests.Architecture
{
    // The only project-specific part of the architecture tests: porting them to another game means editing this file
    // and regenerating ArchitectureBaseline, nothing else.
    public static class ArchitectureConfig
    {
        // Relative to the project root; every path below is relative to this folder.
        public const string ScriptsRoot = "Assets/_Dev/Scripts";
        public const int MaxFileLines = 300;

        public const string CoreAssembly = "Game.Core";
        public const string GameAssembly = "Game.Game";
        public const string GameAssemblyPrefix = "Game.";
        public const string SdkAssemblyPrefix = "MoodBoost.Sdk.";

        public static readonly string[] RuntimeFolders = { "Core/", "Game/" };
        public static readonly string[] CoreFolders = { "Core/" };
        public static readonly string[] AllFolders = { "" };

        // The one file allowed to touch Pool Boss statics, and the one allowed to read the system clock.
        public const string PoolServiceFile = "Game/Services/PoolBossObjectPoolService.cs";
        public const string ClockServiceFile = "Game/Services/SystemClockService.cs";

        // Beyond the BCL, Core may reference only these (Vector2Int, Vector3Int, Vector3, Mathf).
        public static readonly string[] CoreAllowedAssemblies = { "UnityEngine.CoreModule" };

        // The SDK surface game code may reference; every other MoodBoost.Sdk.* assembly is a vendor provider.
        public static readonly string[] SdkFacadeAssemblies =
        {
            "MoodBoost.Sdk.Core", "MoodBoost.Sdk.Ads", "MoodBoost.Sdk.Tracking", "MoodBoost.Sdk.Iap",
            "MoodBoost.Sdk.Notifications", "MoodBoost.Sdk.Haptics", "MoodBoost.Sdk.Unity",
        };

        // SDK assemblies allowed to know VContainer: the installer layer and its Editor tools.
        public static readonly string[] SdkDiAssemblies = { "MoodBoost.Sdk.Unity", "MoodBoost.Sdk.Editor" };

        public static readonly string[] SdkForbiddenAssemblyPrefixes = { "VContainer", "LitMotion", "R3", "DarkTonic" };

        // Third-party SDKs only a MoodBoost provider assembly may name. ttsdk is absent on purpose: it declares a
        // global PlayerPrefs that every PlayerPrefs call binds to, so the TTSDK namespace is guarded at source level.
        public static readonly string[] VendorAssemblyPrefixes =
        {
            "Unity.LevelPlay", "Firebase", "Unity.Purchasing", "UnityEngine.Purchasing", "TenjinSDK", "AdjustSdk",
            "GoogleMobileAds", "AppLovin", "MaxSdk", "TTWebGL", "TTLitJson", "MOST_HapticFeedback",
        };
    }
}
