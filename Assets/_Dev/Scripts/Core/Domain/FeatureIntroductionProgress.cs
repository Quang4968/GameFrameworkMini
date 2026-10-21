namespace Game.Core.Domain
{
    // Tiến độ tới cơ chế kế tiếp, đã tính xong để view chỉ việc vẽ.
    //
    // Thanh chạy từ MỐC TRƯỚC tới MỐC SAU chứ không phải từ màn 0: mỗi cơ chế là một chặng đầy đủ
    // 0 -> 100%, nên chặng nào cũng thấy nhích. Tính từ màn 0 thì chặng thứ tư đã đầy 90% ngay lúc
    // vừa mở, mỗi màn nhích không nổi một pixel.
    public readonly struct FeatureIntroductionProgress
    {
        public GameFeature Feature { get; }

        // Mốc đầu chặng: màn giới thiệu TRƯỚC đó, hoặc 0 nếu đây là cơ chế đầu tiên.
        public int FromLevelIndex { get; }

        // Màn cơ chế sẽ xuất hiện.
        public int TargetLevelIndex { get; }

        // Màn người chơi sắp vào. Lúc popup thắng đang mở thì đây là màn vừa thắng + 1.
        public int NextLevelIndex { get; }

        public float Normalized01 { get; }

        // Số màn còn phải thắng nữa. 0 = màn sắp vào CHÍNH LÀ màn có cơ chế.
        public int LevelsRemaining { get; }

        public bool IsReadyNow => LevelsRemaining <= 0;

        public FeatureIntroductionProgress(GameFeature feature, int fromLevelIndex, int targetLevelIndex,
            int nextLevelIndex, float normalized01, int levelsRemaining)
        {
            Feature = feature;
            FromLevelIndex = fromLevelIndex;
            TargetLevelIndex = targetLevelIndex;
            NextLevelIndex = nextLevelIndex;
            Normalized01 = normalized01;
            LevelsRemaining = levelsRemaining;
        }
    }
}
