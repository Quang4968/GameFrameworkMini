namespace Game.Game.Views.Popup
{
    // Component trên prefab popup cần trả lại trạng thái đúng lúc popup BẮT ĐẦU đóng.
    // Object chỉ tắt sau animation đóng nên OnDisable là quá muộn: PopupService đã tắt lớp mờ
    // xong xuôi rồi mới tới lượt nó chạy.
    public interface IPopupClosing
    {
        void OnPopupClosing();
    }
}
