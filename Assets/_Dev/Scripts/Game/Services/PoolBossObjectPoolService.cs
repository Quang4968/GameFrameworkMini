using System;
using DarkTonic.PoolBoss;
using Game.Game.Data;
using UnityEngine;

namespace Game.Game.Services
{
    // Pool Boss vốn là singleton đặt sẵn trong scene: mọi thứ đi qua static PoolBoss.X và cần
    // đúng một object PoolBoss có mặt từ đầu. Service này gói trọn chỗ đó — nó TỰ dựng host khi
    // container tạo ra nó và tự huỷ host khi scope chết.
    //
    // Đổi lại: scene không mang theo object cấu hình nào, danh sách pool nằm ở asset catalog, và
    // đây là file duy nhất trong game biết tên DarkTonic.
    public sealed class PoolBossObjectPoolService : IObjectPoolService, IDisposable
    {
        const string HostName = "PoolBoss (runtime)";

        readonly PoolBoss host;
        readonly bool initialized;

        bool disposed;

        public PoolBossObjectPoolService(PoolCatalogSO catalog)
        {
            var go = new GameObject(HostName);

            // Tắt TRƯỚC khi AddComponent. AddComponent chạy Awake ngay lập tức, mà Awake của Pool
            // Boss tự gọi Initialize khi autoCreatePools còn bật — lúc đó danh sách còn rỗng và
            // Pool Boss kẹt vĩnh viễn ở trạng thái "đã init nhưng chưa sẵn sàng".
            go.SetActive(false);

            host = go.AddComponent<PoolBoss>();
            host.autoCreatePools = false;
            host.framesForInit = 1;
            host.poolItems.Clear();

            var itemCount = Fill(host, catalog);

            go.SetActive(true);

            // Pool Boss chỉ bật cờ sẵn sàng sau khi preload đủ số item đã khai; danh sách rỗng thì
            // cờ đó không bao giờ lên. Không có gì để pool thì đừng init, để caller tự xoay.
            if (itemCount == 0)
            {
                Debug.LogWarning(
                    $"[{nameof(PoolBossObjectPoolService)}] Catalog rỗng nên không pool gì cả. " +
                    "Mọi thứ sẽ rơi về Instantiate/Destroy — gán prefab vào PoolCatalog để hết rác.");
                return;
            }

            PoolBoss.Initialize();
            initialized = true;
        }

        public bool IsReady => !disposed && initialized && host != null && PoolBoss.IsReady;

        public bool Contains(GameObject prefab)
        {
            // Phải chặn ở đây: PrefabIsInPool tự log error khi pool chưa sẵn sàng, mà hàm này bị
            // gọi mỗi nhát chém.
            if (!IsReady || prefab == null) return false;

            return PoolBoss.PrefabIsInPool(prefab.transform);
        }

        public Transform Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!Contains(prefab)) return null;

            // Cạn pool và không được phép tạo thêm: Pool Boss đã tự log, trả null cho caller quyết.
            return PoolBoss.SpawnInPool(prefab.transform, position, rotation);
        }

        public bool Despawn(Transform instance)
        {
            if (!IsReady || instance == null) return false;

            return PoolBoss.Despawn(instance);
        }

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            // Huỷ host cũng là cách reset cờ init của Pool Boss (nó làm trong OnDestroy), nhờ vậy
            // scene sau dựng service mới không bị nó kêu "Initialize được gọi nhiều hơn một lần".
            if (host != null) UnityEngine.Object.Destroy(host.gameObject);
        }

        static int Fill(PoolBoss target, PoolCatalogSO catalog)
        {
            if (catalog == null) return 0;

            var entries = catalog.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Prefab == null) continue;

                target.poolItems.Add(new PoolBossItem
                {
                    prefabTransform = entry.Prefab.transform,
                    instancesToPreload = Mathf.Max(1, entry.Preload),
                    allowInstantiateMore = entry.AllowInstantiateMore,
                    itemHardLimit = Mathf.Max(entry.HardLimit, Mathf.Max(1, entry.Preload)),
                    categoryName = PoolBoss.NoCategory,
                    isExpanded = true
                });
            }

            return target.poolItems.Count;
        }
    }
}
