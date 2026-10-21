using UnityEngine;

namespace Game.Game.Services
{
    // Cổng duy nhất của game tới hệ pool.
    //
    // Interface ở Game chứ không phải Core: nó nói bằng GameObject/Transform, mà Core không được
    // biết mấy kiểu đó.
    //
    // Cái đổi được nhờ có nó: không còn chỗ nào trong game gọi thẳng static của Pool Boss, nên
    // đổi hệ pool hay tắt pool trong một scene chỉ là sửa một dòng ở LifetimeScope.
    public interface IObjectPoolService
    {
        // Pool đã dựng xong và dùng được chưa.
        bool IsReady { get; }

        // Prefab này đã được khai trong catalog chưa. Tách khỏi Spawn để caller phân biệt "quên
        // khai" — lỗi cấu hình, đáng cảnh báo — với "pool cạn", chuyện bình thường lúc cao điểm.
        bool Contains(GameObject prefab);

        // Null = không lấy được: chưa sẵn sàng, chưa khai, hoặc cạn mà không cho tạo thêm. Caller
        // tự quyết bỏ qua hay Instantiate tạm.
        Transform Spawn(GameObject prefab, Vector3 position, Quaternion rotation);

        // False = instance không thuộc pool nào đang quản, caller phải tự Destroy.
        bool Despawn(Transform instance);
    }
}
