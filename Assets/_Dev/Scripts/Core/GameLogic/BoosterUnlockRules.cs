using Game.Core.Domain;

namespace Game.Core.GameLogic
{
    // Whether a booster button is usable yet. Derived from the tutorial schedule alone, so there is
    // no second copy of the unlock levels and nothing extra to persist: reaching the level IS the
    // unlock, and a wiped save re-locks exactly as it should.
    public static class BoosterUnlockRules
    {
        // A booster with no tutorial row is unlocked from level 1 — silence in the config must not
        // hide a button forever.
        public static bool IsUnlocked(TutorialSchedule schedule, BoosterType booster, int currentLevelIndex)
        {
            if (schedule == null) return true;

            return !schedule.TryGetForBooster(booster, out var tutorial)
                || currentLevelIndex >= tutorial.LevelIndex;
        }

        // Whether the booster bar is worth showing at all: before the first unlock level every
        // button would be a padlock, so the whole row stays hidden instead. Derived from the same
        // schedule as IsUnlocked, so moving the first lesson moves the row with it.
        public static bool HasAnyUnlocked(TutorialSchedule schedule, int currentLevelIndex)
        {
            if (schedule == null) return true;

            var gated = false;

            for (int i = 0; i < schedule.Count; i++)
            {
                var tutorial = schedule[i];

                // A lesson that unlocks nothing says nothing about the row — counting it would
                // show five padlocks from the level it runs on.
                if (!tutorial.UnlocksBooster) continue;

                if (currentLevelIndex >= tutorial.LevelIndex) return true;

                gated = true;
            }

            return !gated;
        }

        // Level number to print on a locked button ("Unlock at Level 6"), counted from 1 the way
        // the player reads it. False = nothing to promise, the button is already usable.
        public static bool TryGetUnlockLevelNumber(TutorialSchedule schedule, BoosterType booster,
            out int levelNumber)
        {
            levelNumber = 0;

            if (schedule == null || !schedule.TryGetForBooster(booster, out var tutorial)) return false;

            levelNumber = tutorial.LevelIndex + 1;
            return true;
        }
    }
}
