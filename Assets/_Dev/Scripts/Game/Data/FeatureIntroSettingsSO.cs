using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.GameLogic;
using Game.Game.Services;
using UnityEngine;

namespace Game.Game.Data
{
    // Cơ chế nào được giới thiệu ở màn nào, cộng phần hiển thị (icon, tên) của nó.
    //
    // Nội dung màn nằm sẵn trong file dump nên asset này KHÔNG bật/tắt được cơ chế, nó chỉ nói
    // "màn này là màn ra mắt". Số mặc định lấy từ chính dữ liệu: bốn màn dạy cơ chế của game gốc
    // là HiddenBox (8), ConnectedBox (13), IcedBlasterBox (30), KeyLockBox (40). Chạy
    // Game > Validate Feature Introductions sau mỗi lần sửa để biết config còn khớp data không.
    [CreateAssetMenu(menuName = "Game/Feature Intro Settings", fileName = "FeatureIntroSettings")]
    public sealed class FeatureIntroSettingsSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public GameFeature feature;

            [Tooltip("SỐ MÀN người chơi nhìn thấy (màn đầu tiên là 1), không phải chỉ số đếm từ 0.")]
            [Min(1)] public int introLevel;

            [Tooltip("Icon hiện trên popup thắng. Bỏ trống thì ô icon tự ẩn.")]
            public Sprite icon;

            [Tooltip("Tên hiện trên popup thắng. Bỏ trống thì dùng luôn tên của enum.")]
            public string displayName;

            [Tooltip("Mô tả ngắn hiện trong popup New Feature.")]
            [TextArea(2, 3)] public string description;
        }

        [Tooltip("Mỗi cơ chế một dòng. Cơ chế không có dòng nào thì không bao giờ được khoe.")]
        [SerializeField] Entry[] features = new Entry[0];

        // Lịch đã dọn sạch cho Core dùng. Dựng mới mỗi lần gọi nên view phải giữ lại kết quả,
        // đừng gọi trong Update.
        public FeatureIntroductionSchedule BuildSchedule(GameRemoteConfig remoteConfig = null)
        {
            var introductions = new List<FeatureIntroduction>();

            foreach (var entry in features)
            {
                // Asset ghi bằng số màn, Core đếm từ 0 — quy đổi ĐÚNG MỘT CHỖ là đây.
                var introLevel = remoteConfig != null
                    ? remoteConfig.FeatureIntroLevel(entry.feature, entry.introLevel)
                    : entry.introLevel;
                introductions.Add(new FeatureIntroduction(entry.feature, introLevel - 1));
            }

            return new FeatureIntroductionSchedule(introductions);
        }

        public bool TryGetEntry(GameFeature feature, out Entry entry)
        {
            foreach (var candidate in features)
            {
                if (candidate.feature != feature) continue;

                entry = candidate;
                return true;
            }

            entry = default;
            return false;
        }

        // Tên để hiện lên UI. Quên điền thì rơi về tên enum chứ không để trống trơn — thiếu chữ
        // vẫn đọc được, còn ô rỗng thì trông như UI hỏng.
        public string GetDisplayName(GameFeature feature)
        {
            if (TryGetEntry(feature, out var entry) && !string.IsNullOrWhiteSpace(entry.displayName))
            {
                return entry.displayName;
            }

            return feature.ToString();
        }

        public Sprite GetIcon(GameFeature feature)
            => TryGetEntry(feature, out var entry) ? entry.icon : null;

        public string GetDescription(GameFeature feature)
            => TryGetEntry(feature, out var entry) ? entry.description : string.Empty;
    }
}
