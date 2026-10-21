using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Services
{
    // Tutorial progress in PlayerPrefs, one key per lesson. The key follows the NAME of
    // E_TutorialId, not its ordinal, so reordering the enum cannot turn "Vacuum learned" into "Dig
    // learned" — same trade as PlayerPrefsBoosterInventoryService: renaming resets that lesson.
    public sealed class PlayerPrefsTutorialStateService : I_TutorialStateService
    {
        public const string PrefsKeyPrefix = "Game.Tutorial.";

        readonly Dictionary<E_TutorialId, E_TutorialStage> stages = new();

        public event Action<E_TutorialId, E_TutorialStage> StageChanged;

        public PlayerPrefsTutorialStateService()
        {
            foreach (E_TutorialId id in Enum.GetValues(typeof(E_TutorialId)))
            {
                var stored = PlayerPrefs.GetInt(KeyOf(id), (int)E_TutorialStage.NotStarted);
                stages[id] = Clamp(stored);
            }
        }

        public E_TutorialStage GetStage(E_TutorialId id)
            => stages.TryGetValue(id, out var stage) ? stage : E_TutorialStage.NotStarted;

        public void SetStage(E_TutorialId id, E_TutorialStage stage)
        {
            // Forward only, or replaying level 6 hands out the free boosters again.
            if (stage <= GetStage(id)) return;

            stages[id] = stage;
            PlayerPrefs.SetInt(KeyOf(id), (int)stage);

            // Written straight away: the reward was granted one line earlier, and an app killed
            // between the two would pay out twice on the next launch.
            PlayerPrefs.Save();

            StageChanged?.Invoke(id, stage);
        }

        static string KeyOf(E_TutorialId id) => PrefsKeyPrefix + id;

        // A hand-edited or future-version key must not read as a stage that does not exist.
        static E_TutorialStage Clamp(int stored)
        {
            if (stored <= (int)E_TutorialStage.NotStarted) return E_TutorialStage.NotStarted;
            return stored >= (int)E_TutorialStage.Done ? E_TutorialStage.Done : E_TutorialStage.RewardClaimed;
        }
    }
}
