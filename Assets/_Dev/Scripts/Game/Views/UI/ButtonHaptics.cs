using Game.Core.Services;

namespace Game.Game.Views.UI
{
    // Cú rung chung cho MỌI nút UI trong game. Cue để một chỗ duy nhất ở đây: nút nằm rải khắp
    // scene lẫn popup, mỗi nơi tự chọn cue thì sớm muộn cũng lệch nhau — giống lý do
    // ButtonPressEffect giữ nhịp nhún ngay trong code.
    //
    // Selection is the lightest tap: a button only needs a click, not a punch. A player who wants more raises Haptic
    // Strength in Settings, which SdkHapticService applies.
    //
    // Là extension nên gọi được cả khi service chưa được inject (scene test, popup mở tay) —
    // không phải rắc null-check ra từng nút.
    public static class ButtonHaptics
    {
        public static void PlayButtonTap(this IHapticService haptics)
        {
            haptics?.Play(HapticCue.Selection);
        }
    }
}
