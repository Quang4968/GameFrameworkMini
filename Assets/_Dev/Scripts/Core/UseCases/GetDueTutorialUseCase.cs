using Game.Core.Domain;
using Game.Core.GameLogic;
using Game.Core.Services;

namespace Game.Core.UseCases
{
    // The tutorial the player still owes on the level about to be played — called as the scene
    // opens, so LevelShow is that level. Returns the EARLIEST unfinished lesson, not this
    // level's: someone who reached level 15 with level 6 unlearned gets the backlog back one level
    // at a time instead of losing it.
    public static class GetDueTutorialUseCase
    {
        public static bool Execute(TutorialSchedule schedule, ILevelProgressService levelProgress,
            I_TutorialStateService state, out TutorialDefinition due)
        {
            due = default;

            if (schedule == null || levelProgress == null || state == null) return false;

            var currentLevel = levelProgress.LevelShow;

            for (int i = 0; i < schedule.Count; i++)
            {
                var tutorial = schedule[i];

                // Sorted by level, so the first entry past the player's level ends the search.
                if (tutorial.LevelIndex > currentLevel) return false;

                // Claimed is already terminal, not just Done: once the player has taken the gift
                // the lesson has been shown, and showing it again on the next launch reads as a
                // bug. Quitting BEFORE claiming still brings it back — nothing was handed over.
                if (state.GetStage(tutorial.Id) >= E_TutorialStage.RewardClaimed) continue;

                due = tutorial;
                return true;
            }

            return false;
        }
    }
}
