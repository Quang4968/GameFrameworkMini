using System.Collections.Generic;
using Game.Core.Domain;

namespace Game.Core.GameLogic
{
    // The schedule, cleaned up: sorted by level, one entry per lesson and per booster. Built once
    // and asked many times, so nothing downstream ever sees the mess a config asset may hold.
    // Same shape as FeatureIntroductionSchedule.
    public sealed class TutorialSchedule
    {
        public static readonly TutorialSchedule Empty = new(null);

        readonly TutorialDefinition[] tutorials;

        public TutorialSchedule(IEnumerable<TutorialDefinition> source)
        {
            var kept = new List<TutorialDefinition>();

            if (source != null)
            {
                foreach (var tutorial in source)
                {
                    if (tutorial.LevelIndex < 0 || tutorial.RewardAmount < 0) continue;

                    // Duplicate lesson or duplicate booster: keep the EARLIEST level. Two unlock
                    // levels for one button would leave BoosterUnlockRules with no single answer.
                    // Lessons that unlock nothing clash on their id alone — their booster field
                    // carries no meaning to collide with.
                    var duplicate = -1;
                    for (int i = 0; i < kept.Count; i++)
                    {
                        var sameBooster = kept[i].UnlocksBooster && tutorial.UnlocksBooster
                            && kept[i].Booster == tutorial.Booster;

                        if (kept[i].Id != tutorial.Id && !sameBooster) continue;
                        duplicate = i;
                        break;
                    }

                    if (duplicate < 0)
                    {
                        kept.Add(tutorial);
                        continue;
                    }

                    if (tutorial.LevelIndex < kept[duplicate].LevelIndex) kept[duplicate] = tutorial;
                }
            }

            kept.Sort((a, b) => a.LevelIndex.CompareTo(b.LevelIndex));
            tutorials = kept.ToArray();
        }

        public int Count => tutorials.Length;

        public TutorialDefinition this[int index] => tutorials[index];

        public bool TryGet(E_TutorialId id, out TutorialDefinition tutorial)
        {
            foreach (var candidate in tutorials)
            {
                if (candidate.Id != id) continue;

                tutorial = candidate;
                return true;
            }

            tutorial = default;
            return false;
        }

        // A booster with no row here is never taught and never locked — adding a sixth booster
        // must not make its button silently disappear until someone writes a tutorial for it.
        public bool TryGetForBooster(BoosterType booster, out TutorialDefinition tutorial)
        {
            foreach (var candidate in tutorials)
            {
                if (!candidate.UnlocksBooster || candidate.Booster != booster) continue;

                tutorial = candidate;
                return true;
            }

            tutorial = default;
            return false;
        }
    }
}
