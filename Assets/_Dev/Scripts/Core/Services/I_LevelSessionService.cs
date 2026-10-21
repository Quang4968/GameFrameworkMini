using Game.Core.Domain;

namespace Game.Core.Services
{
    // Per-level counters the tracking plan needs on every level event: how many times this level has been played,
    // lost and abandoned.
    //
    // Only the level being played is remembered. Moving to another level resets the three counters, so the store
    // stays four PlayerPrefs keys instead of three per level for a game with hundreds of them.
    public interface I_LevelSessionService
    {
        int Level { get; }

        // Counting the play in progress, so the first play is 1.
        int PlayIndex { get; }

        // Losses and exits BEFORE the play in progress; 0 on a first play.
        int LoseIndex { get; }

        int ExitIndex { get; }

        // How the player got here. Written before the gameplay scene loads, read when it opens.
        E_LevelPlayType PlayType { get; }

        void SetNextPlayType(E_LevelPlayType playType);

        // Opens a play of this level: rolls PlayIndex, and resets everything when the level changed.
        void BeginPlay(int level);

        void RecordLoss();

        void RecordExit();
    }
}
