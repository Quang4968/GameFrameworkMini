using static Game.Core.Tests.Architecture.ArchitectureConfig;

namespace Game.Core.Tests.Architecture
{
    // The conventions a compiler cannot catch. Each Fix names the replacement, so a failure says what to write instead.
    public static class SourceRules
    {
        public static readonly SourceRule[] All =
        {
            new SourceRule("coroutine",
                @"\bIEnumerator\b|\bStartCoroutine\s*\(|\byield\s+return\b",
                "Use async UniTask with a CancellationToken instead of a coroutine.",
                RuntimeFolders),

            new SourceRule("tween-library",
                @"\bDG\.Tweening\b|\bLeanTween\b|\biTween\b",
                "Use LitMotion (LMotion.Create(...).BindTo...(...).AddTo(gameObject)).",
                RuntimeFolders),

            new SourceRule("poolboss-static",
                @"\bPoolBoss\.",
                "Inject IObjectPoolService; only PoolBossObjectPoolService talks to Pool Boss.",
                RuntimeFolders, PoolServiceFile),

            new SourceRule("singleton",
                @"\bstatic\s+[\w<>?]+\s+Instance\b",
                "Register the type in a LifetimeScope and inject it instead of a static Instance.",
                RuntimeFolders),

            new SourceRule("async-void",
                @"\basync\s+void\b",
                "Use async UniTaskVoid and call it with .Forget(), or return UniTask.",
                RuntimeFolders),

            new SourceRule("find-object",
                @"\b(?:FindObjectOfType|FindFirstObjectByType|FindAnyObjectByType|FindObjectsOfType|FindObjectsByType)\b|\bGameObject\.Find\s*\(",
                "Inject the dependency or take it as a [SerializeField] reference.",
                RuntimeFolders),

            new SourceRule("raw-instantiate",
                @"(?:^|[^\w.])(?:UnityEngine\.)?(?:Object\.|GameObject\.)?Instantiate\s*[<(]",
                "Spawn short-lived objects via IObjectPoolService; build prefabs holding [Inject] with resolver.Instantiate.",
                RuntimeFolders),

            new SourceRule("system-clock",
                @"\bDateTime(?:Offset)?\.(?:UtcNow|Now|Today)\b",
                "Inject IClockService; SystemClockService is the only reader of the system clock.",
                RuntimeFolders, ClockServiceFile),

            new SourceRule("core-unity-api",
                @"\b(?:MonoBehaviour|GameObject|Transform|ScriptableObject|PlayerPrefs|Debug|Time|Application|Resources|Physics|Input)\s*\.|:\s*(?:MonoBehaviour|ScriptableObject)\b|\busing\s+(?:VContainer|LitMotion|Cysharp|R3|DarkTonic|MoodBoost|UnityEngine\.)",
                "Core is pure C#: move Unity, DI, tween, R3 and SDK code to Game and talk to it through a Core/Services interface.",
                CoreFolders),

            new SourceRule("vendor-sdk-namespace",
                @"\busing\s+(?:Unity\.Services\.LevelPlay|Firebase|UnityEngine\.Purchasing|Tenjin|AdjustSdk|GoogleMobileAds|TTSDK|Solo\.MOST_IN_ONE|MoodBoost\.Sdk\.(?:TikTok|Ads\.Mediation|Ads\.Providers|Tracking\.Providers|Haptics\.Providers))\b",
                "Game code talks to the SDK facade (IAdService, ITrackingService, IHapticsService...), never to a vendor.",
                RuntimeFolders),

            new SourceRule("interface-prefix",
                @"\binterface\s+I_\w+",
                "Name interfaces IFoo, without an underscore.",
                AllFolders),

            new SourceRule("enum-prefix",
                @"\benum\s+E_\w+",
                "Name enums in plain PascalCase, without an E_ prefix.",
                AllFolders),

            new SourceRule("underscore-field",
                @"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|private|protected|internal|readonly|static|const|volatile)\s+)*(?!return\b|await\b|yield\b|throw\b|else\b|case\b|goto\b|in\b|out\b|ref\b)[\w<>\[\],.?]+\s+_[a-zA-Z]\w*\s*(?:=|;)",
                "Name fields in camelCase without a leading underscore.",
                AllFolders),
        };
    }
}
