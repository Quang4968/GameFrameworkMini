namespace Game.Core.Domain
{
    // Một dòng trong lịch giới thiệu: cơ chế nào xuất hiện lần đầu ở màn nào.
    //
    // Đây chỉ là LỜI TUYÊN BỐ, không phải cái công tắc: nội dung màn nằm sẵn trong file dump, nên
    // config sai không làm cơ chế biến mất mà chỉ khiến popup hứa lệch. Việc soi lệch là của
    // FeatureIntroValidator trong Editor.
    public readonly struct FeatureIntroduction
    {
        public GameFeature Feature { get; }

        // Chỉ số màn, đếm từ 0 — cùng hệ với ILevelProgressService.LevelShow.
        public int LevelIndex { get; }

        public FeatureIntroduction(GameFeature feature, int levelIndex)
        {
            Feature = feature;
            LevelIndex = levelIndex;
        }
    }
}
