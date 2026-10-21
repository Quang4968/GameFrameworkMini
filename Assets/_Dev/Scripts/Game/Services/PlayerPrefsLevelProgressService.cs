using Game.Core.GameLogic;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Services
{
    // Lưu tiến độ màn vào PlayerPrefs. Con số nằm trong key là LevelShow — nó chỉ tăng, kể cả khi
    // người chơi đã vào vòng lặp; chỉ số nạp màn suy ra từ nó qua LevelLoopRules.
    public sealed class PlayerPrefsLevelProgressService : ILevelProgressService
    {
        public const string PrefsKey = "Game.CurrentLevel";

        readonly int[] levelOrder;
        readonly int loopStart;
        readonly int loopEnd;

        public PlayerPrefsLevelProgressService(int[] levelOrder, int loopStart, int loopEnd)
        {
            this.levelOrder = levelOrder ?? new int[0];
            this.loopStart = loopStart;
            this.loopEnd = loopEnd;

            // Ghi sẵn màn hiện tại lúc chạy lần đầu. Chỉ đọc bằng GetInt thì PlayerPrefs không tạo
            // key, nên nó không hiện trong cửa sổ PlayerPrefs để sửa tay mà nhảy màn.
            if (!PlayerPrefs.HasKey(PrefsKey)) SetLevelShow(LevelShow);
        }

        public int LevelShow => Mathf.Max(0, PlayerPrefs.GetInt(PrefsKey, 0));

        public int LevelLoad
        {
            get
            {
                if (levelOrder.Length == 0) return 0;
                var orderedIndex = LevelLoopRules.ResolveLoadIndex(
                    LevelShow, levelOrder.Length, loopStart, loopEnd);
                return levelOrder[orderedIndex];
            }
        }

        public void SetLevelShow(int levelShow)
        {
            PlayerPrefs.SetInt(PrefsKey, Mathf.Max(0, levelShow));
            PlayerPrefs.Save();
        }

        public void AdvanceToNextLevel() => SetLevelShow(LevelShow + 1);
    }
}
