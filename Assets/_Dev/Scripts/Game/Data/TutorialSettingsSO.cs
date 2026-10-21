using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.GameLogic;
using Game.Game.Services;
using UnityEngine;

namespace Game.Game.Data
{
    // Which booster is taught at which level, plus the wording and timing of the lesson.
    //
    // Single source of truth for BOTH the lesson and the unlock — a second asset holding unlock
    // levels would drift, and the player would meet a lesson for a button they already own.
    [CreateAssetMenu(menuName = "Game/Tutorial Settings", fileName = "TutorialSettings")]
    public sealed class TutorialSettingsSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public E_TutorialId id;

            [Tooltip("Booster this lesson unlocks. Its button stays locked until the level below. " +
                "Ignored when unlocksBooster is off.")]
            public BoosterType booster;

            [Tooltip("Off for a lesson that only teaches: no button is gated on it, no gift popup " +
                "opens, and the booster field above means nothing. Keep it ON for every lesson " +
                "that hands a booster over.")]
            public bool unlocksBooster;

            [Tooltip("LEVEL NUMBER as the player reads it (the first level is 1), not the index " +
                "counted from 0.")]
            [Min(1)] public int unlockLevel;

            [Tooltip("Free boosters handed over when the unlock reward is claimed.")]
            [Min(0)] public int rewardAmount;

            [Tooltip("One short line telling the player what to do with the booster, shown on " +
                "the unlock popup. The pointing hand carries no text of its own.")]
            public string usageMessage;

            [Tooltip("Seconds of no interaction before the pointing hand appears. 0 shows it at " +
                "once. Remote config overrides this per the design sheet.")]
            [Min(0f)] public float hintDelaySeconds;
        }

        [Tooltip("One row per lesson. A booster with no row here is never taught and never locked.")]
        [SerializeField] Entry[] tutorials =
        {
            new()
            {
                id = E_TutorialId.HowToPlay, booster = default, unlocksBooster = false,
                unlockLevel = 1, rewardAmount = 0, usageMessage = string.Empty, hintDelaySeconds = 1f
            },
        };

        // Rebuilt on every call — hold on to the result instead of asking inside Update.
        public TutorialSchedule BuildSchedule(GameRemoteConfig remoteConfig = null)
        {
            var definitions = new List<TutorialDefinition>();

            foreach (var entry in tutorials)
            {
                // Asset counts levels from 1, Core from 0 — converted in this ONE place.
                var unlockLevel = remoteConfig != null
                    ? remoteConfig.TutorialUnlockLevel(entry.id, entry.unlockLevel)
                    : entry.unlockLevel;
                var rewardAmount = entry.unlocksBooster && remoteConfig != null
                    ? remoteConfig.TutorialGiftAmount(entry.rewardAmount)
                    : entry.rewardAmount;

                definitions.Add(new TutorialDefinition(
                    entry.id, unlockLevel - 1, entry.booster, rewardAmount,
                    entry.unlocksBooster));
            }

            return new TutorialSchedule(definitions);
        }

        public bool TryGetEntry(E_TutorialId id, out Entry entry)
        {
            foreach (var candidate in tutorials)
            {
                if (candidate.id != id) continue;

                entry = candidate;
                return true;
            }

            entry = default;
            return false;
        }

        public string UsageMessage(E_TutorialId id)
            => TryGetEntry(id, out var entry) ? entry.usageMessage : string.Empty;

        public float HintDelaySeconds(E_TutorialId id)
            => TryGetEntry(id, out var entry) ? entry.hintDelaySeconds : 0f;
    }
}
