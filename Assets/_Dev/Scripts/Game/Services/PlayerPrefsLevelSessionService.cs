using Game.Core.Domain;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Services
{
    // Per-level tracking counters on PlayerPrefs. Only the current level is kept - see I_LevelSessionService.
    public sealed class PlayerPrefsLevelSessionService : I_LevelSessionService
    {
        public const string LevelPrefsKey = "Game.LevelSessionLevel";
        public const string PlayIndexPrefsKey = "Game.LevelPlayIndex";
        public const string LoseIndexPrefsKey = "Game.LevelLoseIndex";
        public const string ExitIndexPrefsKey = "Game.LevelExitIndex";
        public const string PlayTypePrefsKey = "Game.LevelPlayType";

        public int Level { get; private set; }

        public int PlayIndex { get; private set; }

        public int LoseIndex { get; private set; }

        public int ExitIndex { get; private set; }

        public E_LevelPlayType PlayType { get; private set; }

        public PlayerPrefsLevelSessionService()
        {
            Level = PlayerPrefs.GetInt(LevelPrefsKey, -1);
            PlayIndex = Mathf.Max(0, PlayerPrefs.GetInt(PlayIndexPrefsKey, 0));
            LoseIndex = Mathf.Max(0, PlayerPrefs.GetInt(LoseIndexPrefsKey, 0));
            ExitIndex = Mathf.Max(0, PlayerPrefs.GetInt(ExitIndexPrefsKey, 0));
            PlayType = (E_LevelPlayType)Mathf.Clamp(PlayerPrefs.GetInt(PlayTypePrefsKey, 0), 0, 2);
        }

        // Written in Home or in the win/lose popup, read after the scene change - so it goes to disk immediately,
        // like every other value here.
        public void SetNextPlayType(E_LevelPlayType playType)
        {
            PlayType = playType;
            PlayerPrefs.SetInt(PlayTypePrefsKey, (int)playType);
            PlayerPrefs.Save();
        }

        public void BeginPlay(int level)
        {
            // A different level means a different play history; lose_index and exit_index describe THIS level only.
            if (level != Level)
            {
                Level = level;
                PlayIndex = 0;
                LoseIndex = 0;
                ExitIndex = 0;
            }

            PlayIndex++;
            Save();
        }

        public void RecordLoss()
        {
            LoseIndex++;
            Save();
        }

        public void RecordExit()
        {
            ExitIndex++;
            Save();
        }

        void Save()
        {
            PlayerPrefs.SetInt(LevelPrefsKey, Level);
            PlayerPrefs.SetInt(PlayIndexPrefsKey, PlayIndex);
            PlayerPrefs.SetInt(LoseIndexPrefsKey, LoseIndex);
            PlayerPrefs.SetInt(ExitIndexPrefsKey, ExitIndex);
            PlayerPrefs.Save();
        }
    }
}
