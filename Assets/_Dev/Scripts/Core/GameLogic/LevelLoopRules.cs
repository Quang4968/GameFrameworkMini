namespace Game.Core.GameLogic
{
    // Đổi số màn người chơi thấy (LevelShow, tăng mãi) sang chỉ số nạp dữ liệu trong LevelDatabase
    // (LevelLoad).
    //
    // Đoạn [loopStart, loopEnd] chạy vòng vô hạn: qua khỏi loopEnd thì quay về loopStart. Các màn
    // trước loopStart chỉ chơi đúng một lần.
    public static class LevelLoopRules
    {
        // loopEnd < loopStart (mặc định -1) là TẮT vòng lặp — khi đó gói theo cả database như cũ.
        public static int ResolveLoadIndex(int levelShow, int levelCount, int loopStart, int loopEnd)
        {
            if (levelCount <= 0) return 0;
            if (levelShow < 0) levelShow = 0;

            // Khai lệch (âm, hoặc trỏ ra ngoài database) thì bỏ vòng lặp thay vì để repository ném lỗi.
            if (loopStart < 0 || loopEnd < loopStart || loopEnd >= levelCount) return levelShow % levelCount;

            if (levelShow <= loopEnd) return levelShow;

            var loopLength = loopEnd - loopStart + 1;
            return loopStart + (levelShow - loopStart) % loopLength;
        }
    }
}
