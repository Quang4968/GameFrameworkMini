using System;
using Game.Core.Domain;
using Game.Core.GameLogic;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Services
{
    // Win/lose streak on PlayerPrefs, plus the "a level is being played right now" flag that makes abandoning a level
    // count as the loss it is.
    public sealed class PlayerPrefsLevelStreakService : I_LevelStreakService
    {
        public const string WinPrefsKey = "Game.WinStreak";
        public const string LosePrefsKey = "Game.LoseStreak";
        public const string InProgressPrefsKey = "Game.LevelInProgress";

        // RAM only, on purpose: if the app dies with the lose popup open the loss stands, exactly as the life spent
        // on entering the level is not refunded.
        LevelStreak? pendingUndo;

        public LevelStreak Streak { get; private set; }

        public event Action<LevelStreak> StreakChanged;

        public PlayerPrefsLevelStreakService()
        {
            Streak = new LevelStreak(PlayerPrefs.GetInt(WinPrefsKey, 0), PlayerPrefs.GetInt(LosePrefsKey, 0));

            // The flag survived the process, so the previous session was killed mid-level. That is an abandoned level
            // and counts as a loss; committing it here means the first service built in the new session catches it.
            if (PlayerPrefs.GetInt(InProgressPrefsKey, 0) == 1) RecordLoss();
        }

        public void BeginLevel()
        {
            pendingUndo = null;
            SetInProgress(true);
        }

        public void RecordWin()
        {
            pendingUndo = null;
            SetInProgress(false);
            SetStreak(LevelStreakRules.OnWin(Streak));
        }

        public void RecordLoss()
        {
            pendingUndo = Streak;
            SetInProgress(false);
            SetStreak(LevelStreakRules.OnLose(Streak));
        }

        public void CancelLoss()
        {
            if (pendingUndo == null) return;

            var restored = pendingUndo.Value;
            pendingUndo = null;

            // The level is live again, so it is once more one the player can abandon.
            SetInProgress(true);
            SetStreak(restored);
        }

        public void AbandonLevel()
        {
            if (PlayerPrefs.GetInt(InProgressPrefsKey, 0) != 1) return;

            RecordLoss();
        }

        void SetStreak(LevelStreak value)
        {
            Streak = value;
            PlayerPrefs.SetInt(WinPrefsKey, value.Win);
            PlayerPrefs.SetInt(LosePrefsKey, value.Lose);
            PlayerPrefs.Save();
            StreakChanged?.Invoke(value);
        }

        void SetInProgress(bool inProgress)
        {
            PlayerPrefs.SetInt(InProgressPrefsKey, inProgress ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
