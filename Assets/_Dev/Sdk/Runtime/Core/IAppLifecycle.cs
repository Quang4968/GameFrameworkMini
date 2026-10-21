using System;

namespace MoodBoost.Sdk
{
    // The app lifecycle signals the SDK needs. Split into an interface because its source is a MonoBehaviour
    // (OnApplicationPause) while the ads module is plain C# and has to be testable.
    public interface IAppLifecycle
    {
        // Raised when the app returns from the background. This is where the App Open ad is triggered — not in
        // some scene's Awake, because a scene change is not a return from the background.
        event Action Resumed;

        event Action Paused;
    }
}
