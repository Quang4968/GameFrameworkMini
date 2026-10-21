using System;
using System.Globalization;
using Game.Core.GameLogic;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Services
{
    // active_day_n on PlayerPrefs: the install moment plus the timezone offset it happened in.
    public sealed class PlayerPrefsActiveDayService : I_ActiveDayService
    {
        public const string FirstOpenPrefsKey = "Game.FirstOpenUtcTicks";
        public const string OffsetPrefsKey = "Game.FirstOpenOffsetMinutes";
        public const string MaxSeenPrefsKey = "Game.ActiveDayMaxSeen";
        public const string EstimatedPrefsKey = "Game.ActiveDayEstimated";

        // Written by PlayerPrefsLevelProgressService the first time the game ever runs, so its presence without our
        // own key means an install that predates this system.
        const string LegacyProbeKey = "Game.CurrentLevel";

        readonly IClockService clock;

        DateTime firstOpenUtc;
        int offsetMinutes;

        public int DayIndex { get; private set; }

        public bool IsEstimated { get; private set; }

        public event Action<int> DayIndexChanged;

        public PlayerPrefsActiveDayService(IClockService clock)
        {
            this.clock = clock;

            Load();
            Refresh();
        }

        public void Refresh()
        {
            var index = ActiveDayRules.DayIndex(firstOpenUtc, offsetMinutes, clock.UtcNow);

            // Never regress: winding the device clock back would otherwise move a player from D9 to D0.
            var maxSeen = PlayerPrefs.GetInt(MaxSeenPrefsKey, 0);
            if (index < maxSeen) index = maxSeen;

            if (index != maxSeen)
            {
                PlayerPrefs.SetInt(MaxSeenPrefsKey, index);
                PlayerPrefs.Save();
            }

            if (index == DayIndex) return;

            DayIndex = index;
            DayIndexChanged?.Invoke(index);
        }

        void Load()
        {
            var storedTicks = ReadTicks(FirstOpenPrefsKey);
            if (storedTicks > 0)
            {
                firstOpenUtc = ActiveDayRules.GuardFirstOpen(new DateTime(storedTicks, DateTimeKind.Utc), clock.UtcNow);
                offsetMinutes = PlayerPrefs.GetInt(OffsetPrefsKey, 0);
                IsEstimated = PlayerPrefs.GetInt(EstimatedPrefsKey, 0) == 1;
                return;
            }

            firstOpenUtc = clock.UtcNow;

            // The timezone, not the time - the clock stays the one sanctioned source of "now".
            offsetMinutes = (int)DateTimeOffset.Now.Offset.TotalMinutes;

            // Nothing on the device records when an older install happened, so a pre-existing player is seeded from
            // today and flagged, rather than being reported as a brand new D0 and quietly polluting the cohort.
            IsEstimated = PlayerPrefs.HasKey(LegacyProbeKey);

            PlayerPrefs.SetString(FirstOpenPrefsKey, firstOpenUtc.Ticks.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetInt(OffsetPrefsKey, offsetMinutes);
            PlayerPrefs.SetInt(EstimatedPrefsKey, IsEstimated ? 1 : 0);
            PlayerPrefs.Save();
        }

        static long ReadTicks(string key)
        {
            return long.TryParse(PlayerPrefs.GetString(key, "0"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var ticks) ? ticks : 0;
        }
    }
}
