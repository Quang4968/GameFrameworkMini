using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Game.Data
{
    // Danh sách prefab được pool sẵn cho một scene. Trước đây đây là phần khai báo nằm trên object
    // PoolBoss trong scene; đưa ra asset thì scene không còn phải mang theo object cấu hình nào,
    // và hai scene dùng chung một bộ pool chỉ việc trỏ vào cùng file.
    [CreateAssetMenu(menuName = "Game/Pool Catalog", fileName = "PoolCatalog")]
    public sealed class PoolCatalogSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public GameObject Prefab;

            [Tooltip("Số bản dựng sẵn lúc vào scene. Đặt bằng số lượng cùng lúc lúc cao điểm — " +
                     "thiếu thì phải Instantiate giữa trận, thừa thì tốn bộ nhớ.")]
            [Min(1)] public int Preload;

            [Tooltip("Cạn pool thì cho tạo thêm hay bỏ qua lượt spawn. Hiệu ứng ngắn nên bật, " +
                     "vừa không mất hình vừa tự nới tới mức thật sự cần.")]
            public bool AllowInstantiateMore;

            [Tooltip("Trần tuyệt đối kể cả khi được phép tạo thêm — chốt chặn cho lỗi rò spawn.")]
            [Min(1)] public int HardLimit;
        }

        [SerializeField] Entry[] entries = Array.Empty<Entry>();

        public IReadOnlyList<Entry> Entries => entries ?? Array.Empty<Entry>();
    }
}
