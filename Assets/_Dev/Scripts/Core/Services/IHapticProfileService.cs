namespace Game.Core.Services
{
    // Cổng rung dùng cho những event bắn liên tục: nơi gọi cứ báo mỗi lần xảy ra, việc thưa ra
    // là của implementation.
    //
    // Khác IHapticService ở chỗ đó — cái kia rung ngay và luôn, cái này có quyền BỎ QUA lời gọi
    // khi profile vừa rung xong. Five hits landing in the same frame still make one
    // cú rung, và những cú sau đó giãn đều theo khoảng nghỉ khai trong asset.
    public interface IHapticProfileService
    {
        void Play(HapticProfile profile);
    }
}
