namespace Game.Core.Services
{
    // Tiến độ màn, trên HAI trục không được lẫn:
    //
    // - LevelShow: số màn đã đi qua, đếm từ 0 và KHÔNG bao giờ lặp lại. Đây là trục của UI ("Level
    //   N"), của lịch tutorial và của lịch giới thiệu cơ chế.
    // - LevelLoad: chỉ số màn thật trong LevelDatabase, đã gói vào vòng lặp cuối game. Đây là trục
    //   to load data; every level view of the scene reads the same index.
    //
    // Core chỉ biết interface; việc lưu vào đâu (PlayerPrefs, file, server) là của Game.
    public interface ILevelProgressService
    {
        int LevelShow { get; }

        int LevelLoad { get; }

        void SetLevelShow(int levelShow);

        void AdvanceToNextLevel();
    }
}
