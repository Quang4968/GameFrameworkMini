using System;
using Cysharp.Threading.Tasks;
using TTSDK;

namespace MoodBoost.Sdk.Haptics.Providers
{
    // TikTok Mini Game haptics. TTSDK only has VibrateShort and a ~400 ms VibrateLong, with no kind or amplitude, so
    // every tap is VibrateShort - VibrateLong reads as an alarm, not a tap. A pattern's echo is a second short pulse
    // after its delay, which keeps the "double knock" of the strongest setting.
    public sealed class TikTokHapticsProvider : IHapticsProvider
    {
        public string Name => "TikTok";

        public void Play(HapticKind kind) => Vibrate();

        public void Play(in HapticPattern pattern)
        {
            Vibrate();
            if (pattern.HasSecond) VibrateAfterAsync(pattern.Second.DelayMs).Forget();
        }

        static async UniTaskVoid VibrateAfterAsync(long delayMs)
        {
            await UniTask.Delay(TimeSpan.FromMilliseconds(delayMs), DelayType.Realtime);
            Vibrate();
        }

        static void Vibrate() => TT.VibrateShort(new VibrateShortParam());
    }
}
