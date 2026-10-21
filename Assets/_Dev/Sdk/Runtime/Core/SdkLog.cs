using UnityEngine;

namespace MoodBoost.Sdk
{
    // A single door for the whole SDK to log through. Why not call Debug.Log directly: ad SDK logs are very noisy
    // (a line per load/fill/impression) yet they are exactly what needs to be readable on a real device during QA.
    // So Info is stripped from the build unless MB_SDK_LOG is on, while Warn/Error always remain.
    public static class SdkLog
    {
        [System.Diagnostics.Conditional("MB_SDK_LOG")]
        public static void Info(string tag, string message)
        {
            Debug.Log($"[SDK/{tag}] {message}");
        }

        public static void Warn(string tag, string message)
        {
            Debug.LogWarning($"[SDK/{tag}] {message}");
        }

        public static void Error(string tag, string message)
        {
            Debug.LogError($"[SDK/{tag}] {message}");
        }

        public static void Exception(string tag, System.Exception error)
        {
            Debug.LogError($"[SDK/{tag}] {error}");
        }
    }
}
