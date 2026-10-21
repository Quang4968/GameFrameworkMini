using System;
using System.Collections.Generic;
using MoodBoost.Sdk;
using Game.Core.Domain.DailyReward;
using Game.Core.GameLogic;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Services
{
    // The daily reward cycle read from DailyRewardRemoteConfig.Cycle. Re-read on every remote config
    // activation; a value that does not parse into a valid cycle keeps the shipped default instead.
    public sealed class RemoteDailyRewardSchedule : IDailyRewardSchedule, IDisposable
    {
        [Serializable] sealed class CycleJson { public int unlock_level; public DayJson[] days; }
        [Serializable] sealed class DayJson { public bool chest; public ItemJson[] items; }
        [Serializable] sealed class ItemJson { public string id; public int amount; }

        readonly IRemoteConfigService remote;

        public IReadOnlyList<DailyRewardDay> Days { get; private set; }
        public int UnlockLevel { get; private set; }

        public RemoteDailyRewardSchedule(IRemoteConfigService remote)
        {
            this.remote = remote;
            Reload();
            if (remote != null) remote.Updated += Reload;
        }

        public void Dispose()
        {
            if (remote != null) remote.Updated -= Reload;
        }

        public static bool TryParse(string json, out IReadOnlyList<DailyRewardDay> days, out int unlockLevel)
        {
            days = null;
            unlockLevel = 0;
            if (string.IsNullOrWhiteSpace(json)) return false;

            CycleJson parsed;
            try
            {
                parsed = JsonUtility.FromJson<CycleJson>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (parsed?.days == null) return false;

            var list = new List<DailyRewardDay>(parsed.days.Length);
            foreach (var day in parsed.days)
            {
                var items = new List<DailyRewardItem>(day?.items?.Length ?? 0);
                if (day?.items != null)
                {
                    foreach (var item in day.items)
                    {
                        if (item != null) items.Add(new DailyRewardItem(item.id, item.amount));
                    }
                }

                list.Add(new DailyRewardDay(items, day != null && day.chest));
            }

            if (!DailyRewardRules.IsValidCycle(list)) return false;

            days = list;
            unlockLevel = Mathf.Max(0, parsed.unlock_level);
            return true;
        }

        void Reload()
        {
            var json = remote != null ? remote.Get(DailyRewardRemoteConfig.Cycle) : DailyRewardRemoteConfig.Cycle.Default;
            if (TryParse(json, out var days, out var unlockLevel))
            {
                Days = days;
                UnlockLevel = unlockLevel;
                return;
            }

            Debug.LogWarning($"[{nameof(RemoteDailyRewardSchedule)}] '{DailyRewardRemoteConfig.Cycle.Name}' is " +
                "not a valid cycle (whole weeks of 7, every day with items); keeping the previous one.");

            // The very first read must still end with a cycle: the shipped default always parses.
            if (Days == null && TryParse(DailyRewardRemoteConfig.Cycle.Default, out days, out unlockLevel))
            {
                Days = days;
                UnlockLevel = unlockLevel;
            }
        }
    }
}
