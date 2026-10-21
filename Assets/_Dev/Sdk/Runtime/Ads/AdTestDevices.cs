using System;
using UnityEngine;

namespace MoodBoost.Sdk.Ads
{
    // Answers exactly one question: is the running device in the test device list.
    //
    // The list lives on Remote Config rather than in an asset, and that is the crucial point: if adding a new QA
    // person's device required a build, nobody would ever do it, and the whole team would test on live ids - fake
    // impressions counting against the real account and corrupting the data, or worse, getting the account banned by
    // the ad network.
    //
    // The device id comes from SystemInfo.deviceUniqueIdentifier: it exists on every platform, needs no permission, and
    // does not change between installs. Do NOT use the advertising id - the player can reset it, and on Android 13 and
    // above it needs its own permission declared.
    public static class AdTestDevices
    {
        const string Tag = "Ads";

        public static string CurrentDeviceId => SystemInfo.deviceUniqueIdentifier;

        // The list is a comma-separated string ("id1, id2"). Whitespace around each id is ignored, and the comparison
        // is case-insensitive: ids copied from logcat are cased differently by each device manufacturer.
        public static bool Contains(string csv, string deviceId)
        {
            if (string.IsNullOrWhiteSpace(csv) || string.IsNullOrWhiteSpace(deviceId)) return false;

            var parts = csv.Split(',');

            for (var i = 0; i < parts.Length; i++)
            {
                var id = parts[i].Trim();
                if (id.Length == 0) continue;

                if (string.Equals(id, deviceId, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        // Whether to use test ids on this device, logging enough for QA to help themselves.
        public static bool Resolve(string csv)
        {
            var deviceId = CurrentDeviceId;

            // ALWAYS printed at Info level, even when the list is empty. Without this line it becomes a catch-22: adding
            // a device to the list requires knowing its id, and the id only appears once the list is non-empty. Info is
            // stripped from release builds, so players see nothing.
            SdkLog.Info(Tag, $"this device id: {deviceId}");

            LogAdvertisingId();

            if (string.IsNullOrWhiteSpace(csv)) return false;

            var isTestDevice = Contains(csv, deviceId);

            if (isTestDevice)
            {
                SdkLog.Warn(Tag, $"TEST DEVICE ({deviceId}) - using test ad unit ids. Revenue from this " +
                    "device does not count towards the real account.");
            }
            else
            {
                SdkLog.Warn(Tag, $"The test device list is enabled but this device is not in it. " +
                    $"This device id: {deviceId} - add it to ads_test_device_ids on Remote Config if needed.");
            }

            return isTestDevice;
        }

        // Prints the advertising id (GAID) to the log, for dashboards that require exactly that id rather than the
        // device id: LevelPlay's test device list, ironSource Ad Quality, Meta's test devices...
        //
        // Do NOT use this id for ads_test_device_ids - the player can reset it at any time in Settings, and turning on
        // Limit Ad Tracking makes it all zeros. It is only for copying to a dashboard.
        //
        // The whole method is stripped from release builds along with the SdkLog.Info calls: reading the GAID is an IPC
        // round trip to Google Play services, and there is no reason to pay that on a player's device.
        [System.Diagnostics.Conditional("MB_SDK_LOG")]
        static void LogAdvertisingId()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Runs on a background thread because AdvertisingIdClient is a blocking call into Play services, and Google
            // actively THROWS when it is called from Android's main looper. A background thread that wants to talk to
            // Java has to attach to the JVM first, otherwise every JNI call returns null.
            var thread = new System.Threading.Thread(() =>
            {
                UnityEngine.AndroidJNI.AttachCurrentThread();

                try
                {
                    using (var player = new UnityEngine.AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<UnityEngine.AndroidJavaObject>("currentActivity"))
                    using (var client = new UnityEngine.AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient"))
                    using (var info = client.CallStatic<UnityEngine.AndroidJavaObject>("getAdvertisingIdInfo", activity))
                    {
                        var id = info.Call<string>("getId");
                        var limited = info.Call<bool>("isLimitAdTrackingEnabled");

                        SdkLog.Info(Tag, limited
                            ? $"advertising id (GAID): {id} - the device has Limit Ad Tracking ON, this id is meaningless."
                            : $"advertising id (GAID): {id}");
                    }
                }
                catch (Exception error)
                {
                    // The device has no Google Play services, or the AD_ID permission is missing. Not the game's fault:
                    // the id simply cannot be read, everything else works normally.
                    SdkLog.Info(Tag, $"Could not read the advertising id: {error.Message}");
                }
                finally
                {
                    UnityEngine.AndroidJNI.DetachCurrentThread();
                }
            });

            thread.Start();
#endif
        }
    }
}
