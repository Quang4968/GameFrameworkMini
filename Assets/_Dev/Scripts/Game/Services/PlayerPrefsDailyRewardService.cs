using System;
using Game.Core.GameLogic;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Services
{
    // Persists the daily reward progress in PlayerPrefs. State lives in RAM and is written on claim.
    public sealed class PlayerPrefsDailyRewardService : IDailyRewardService
    {
        public const string ClaimedCountPrefsKey = "Game.DailyRewardClaimed";
        public const string LastClaimDayPrefsKey = "Game.DailyRewardLastDay";

        readonly IClockService clock;

        public int ClaimedCount { get; private set; }
        public int LastClaimDay { get; private set; }

        // The device's CURRENT offset, re-read every time: the day rolls at the midnight the player sees.
        public int Today
        {
            get
            {
                var utcNow = clock.UtcNow;
                return DailyRewardRules.DayNumber(utcNow, TimeZoneInfo.Local.GetUtcOffset(utcNow));
            }
        }

        public event Action<int> ClaimedCountChanged;

        public PlayerPrefsDailyRewardService(IClockService clock)
        {
            this.clock = clock;
            ClaimedCount = Mathf.Max(0, PlayerPrefs.GetInt(ClaimedCountPrefsKey, 0));
            LastClaimDay = PlayerPrefs.GetInt(LastClaimDayPrefsKey, -1);
        }

        public void RecordClaim(int day)
        {
            ClaimedCount++;
            LastClaimDay = day;

            PlayerPrefs.SetInt(ClaimedCountPrefsKey, ClaimedCount);
            PlayerPrefs.SetInt(LastClaimDayPrefsKey, day);

            // Save now rather than at quit: a mobile app is killed in the background at any time.
            PlayerPrefs.Save();

            ClaimedCountChanged?.Invoke(ClaimedCount);
        }
    }
}
