using UnityEngine;

namespace Game.Game.Animation
{
    // Đặt lên prefab hiệu ứng dùng chung với Pool Boss.
    //
    // Object lấy ra từ pool KHÔNG phải object mới: nó chỉ được bật lại. ParticleSystem giữ nguyên
    // các hạt còn sống từ lần dùng trước, nên bật lại ở toạ độ mới sẽ loé ra một nhúm hạt cũ ở sai
    // chỗ. Với hiệu ứng looping thì chắc chắn dính, vì lúc trả về pool luôn còn hạt đang bay.
    //
    // Play(true) một mình không đủ — nó không xoá hạt cũ, phải Clear trước.
    //
    // Pool Boss gửi OnSpawned/OnDespawned bằng BroadcastMessage nên hai hàm dưới cố ý để private
    // và không có tham số; đổi tên là mất liên lạc, không có lỗi biên dịch nào báo.
    public sealed class PooledParticleRestarter : MonoBehaviour
    {
        ParticleSystem[] systems;

        void Awake()
        {
            // Gồm cả object đang tắt: prefab hiệu ứng hay tắt sẵn vài nhánh biến thể.
            systems = GetComponentsInChildren<ParticleSystem>(true);
        }

        void OnSpawned()
        {
            if (systems == null) return;

            foreach (var system in systems)
            {
                if (system == null) continue;

                system.Clear(true);
                system.Play(true);
            }
        }

        void OnDespawned()
        {
            if (systems == null) return;

            foreach (var system in systems)
            {
                if (system == null) continue;

                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
