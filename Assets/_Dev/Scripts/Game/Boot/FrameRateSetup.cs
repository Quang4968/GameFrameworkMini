using UnityEngine;

namespace Game.Game.Boot
{
    // On mobile Unity reads targetFrameRate -1 as 30 FPS, so 60 has to be asked for.
    // 60 and not 90/120: this genre shows nothing extra for the frames, and the heat
    // they cost makes the device throttle back under 60 within a few minutes.
    public static class FrameRateSetup
    {
        const int TargetFrameRate = 60;

        // Before the splash screen, not a boot step: Splash runs ahead of the Boot
        // scene and would otherwise play its animation at 30.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void Apply()
        {
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
