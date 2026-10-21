using UnityEngine;

namespace MoodBoost.Sdk.Unity
{
    // The SDK's real clock. Uses realtimeSinceStartup rather than Time.time: ads show while the game already has
    // Time.timeScale = 0 (the win/lose popup), and Time.time stands still then - the cooldown would never elapse.
    public sealed class RealtimeSdkClock : ISdkClock
    {
        public double RealtimeSeconds => Time.realtimeSinceStartupAsDouble;
    }
}
