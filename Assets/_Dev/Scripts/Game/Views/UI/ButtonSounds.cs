using Game.Core.Services;

namespace Game.Game.Views.UI
{
    // Tiếng bấm chung cho MỌI nút UI trong game, song song với ButtonHaptics: cùng một cú chạm
    // thì rung và tiếng phải đi cùng nhau, nên hai extension nằm cạnh nhau và cùng tên hàm.
    //
    // Cue để một chỗ duy nhất ở đây vì nút nằm rải khắp scene lẫn popup, mỗi nơi tự chọn cue thì
    // sớm muộn cũng lệch nhau.
    //
    // Nút mà việc của nó LÀ mở hoặc đóng popup thì không gọi hàm này: PopupService đã bắn
    // PopupOpen/PopupClose cho cú đó rồi, gọi thêm là hai tiếng chồng lên nhau trong một cú chạm.
    //
    // Là extension nên gọi được cả khi service chưa được inject (scene test, popup mở tay) —
    // không phải rắc null-check ra từng nút.
    public static class ButtonSounds
    {
        public static void PlayButtonTap(this IAudioService audio)
        {
            audio?.Play(AudioCue.ButtonClick);
        }

        // Nút Play trên Home — cú bấm mở màn, nên có tiếng riêng thay vì tiếng bấm chung.
        public static void PlayPlayButton(this IAudioService audio)
        {
            audio?.Play(AudioCue.ButtonPlay);
        }

        // Nút xác nhận CHÍNH của một popup (OK / nhận thưởng / mua). Nút phụ và nút đóng vẫn dùng
        // PlayButtonTap, nếu không cả popup nghe như chỉ có một loại nút.
        public static void PlayConfirmButton(this IAudioService audio)
        {
            audio?.Play(AudioCue.ButtonConfirm);
        }
    }
}
