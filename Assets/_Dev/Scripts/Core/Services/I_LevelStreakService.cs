using System;
using Game.Core.Domain;

namespace Game.Core.Services
{
    // Keeps the consecutive win/lose run across sessions, for the win_streak_n / lose_streak_n user properties.
    //
    // CancelLoss exists because a lost level is not final: the player can revive out of the lose popup, and without an
    // undo every revived level would inflate lose_streak_n. BeginLevel marks a level as being played so that quitting
    // or being killed mid-level still counts as the loss it is.
    public interface I_LevelStreakService
    {
        LevelStreak Streak { get; }

        event Action<LevelStreak> StreakChanged;

        void BeginLevel();

        void RecordWin();

        void RecordLoss();

        void CancelLoss();

        // The player left a level without finishing it. Counts as a loss, but only if a level really was in progress -
        // calling it after a level ended normally does nothing.
        void AbandonLevel();
    }
}
