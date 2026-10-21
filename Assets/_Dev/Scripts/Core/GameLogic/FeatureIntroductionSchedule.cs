using System.Collections.Generic;
using Game.Core.Domain;

namespace Game.Core.GameLogic
{
    // Lịch giới thiệu cơ chế, đã dọn sạch: sắp theo màn, mỗi cơ chế đúng một mốc.
    //
    // Dựng một lần rồi hỏi nhiều lần — asset config có thể ghi lộn xộn hay trùng lặp, nhưng từ
    // đây trở đi phần còn lại của game chỉ thấy một danh sách đã đúng.
    public sealed class FeatureIntroductionSchedule
    {
        public static readonly FeatureIntroductionSchedule Empty = new(null);

        readonly FeatureIntroduction[] introductions;

        public FeatureIntroductionSchedule(IEnumerable<FeatureIntroduction> source)
        {
            var kept = new List<FeatureIntroduction>();

            if (source != null)
            {
                foreach (var introduction in source)
                {
                    if (introduction.LevelIndex < 0) continue;

                    // Khai một cơ chế hai lần thì GIỮ MÀN NHỎ NHẤT: lần đầu người chơi gặp nó mới
                    // là lần đáng giới thiệu, mốc sau chỉ là dòng thừa trong asset.
                    var duplicate = -1;
                    for (int i = 0; i < kept.Count; i++)
                    {
                        if (kept[i].Feature != introduction.Feature) continue;
                        duplicate = i;
                        break;
                    }

                    if (duplicate < 0)
                    {
                        kept.Add(introduction);
                        continue;
                    }

                    if (introduction.LevelIndex < kept[duplicate].LevelIndex) kept[duplicate] = introduction;
                }
            }

            kept.Sort((a, b) => a.LevelIndex.CompareTo(b.LevelIndex));
            introductions = kept.ToArray();
        }

        public int Count => introductions.Length;

        public FeatureIntroduction this[int index] => introductions[index];

        public bool TryGetIntroductionAt(int levelIndex, out FeatureIntroduction introduction)
        {
            foreach (var candidate in introductions)
            {
                if (candidate.LevelIndex < levelIndex) continue;
                if (candidate.LevelIndex > levelIndex) break;

                introduction = candidate;
                return true;
            }

            introduction = default;
            return false;
        }

        // Cơ chế kế tiếp tính từ màn người chơi SẮP VÀO, kèm tiến độ của chặng đang chạy.
        //
        // Trả false khi đã giới thiệu hết — view lấy đó làm tín hiệu tự ẩn, chứ không có mốc giả
        // nào để vẽ tiếp.
        public bool TryGetUpcoming(int nextLevelIndex, out FeatureIntroductionProgress progress)
        {
            for (int i = 0; i < introductions.Length; i++)
            {
                var target = introductions[i];
                if (target.LevelIndex < nextLevelIndex) continue;

                // Mốc đầu chặng là màn giới thiệu liền trước; cơ chế đầu tiên thì chặng chạy từ
                // màn 0 vì trước nó không có gì cả.
                var from = i > 0 ? introductions[i - 1].LevelIndex : 0;
                var span = target.LevelIndex - from;

                // Hai mốc dính nhau (hoặc mốc đầu ngay tại màn 0) thì không có chặng nào để chạy —
                // thanh đầy luôn thay vì chia cho 0.
                var normalized = span <= 0 ? 1f : Clamp01((float)(nextLevelIndex - from) / span);

                progress = new FeatureIntroductionProgress(
                    target.Feature, from, target.LevelIndex, nextLevelIndex,
                    normalized, target.LevelIndex - nextLevelIndex);
                return true;
            }

            progress = default;
            return false;
        }

        static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }
    }
}
