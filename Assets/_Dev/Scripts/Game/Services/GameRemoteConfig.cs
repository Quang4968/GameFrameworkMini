using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.GameLogic;
using MoodBoost.Sdk;
using UnityEngine;

namespace Game.Game.Services
{
    // Typed view over remote config for the framework's own parameters. Every accessor clamps or
    // falls back, so a bad value on the dashboard never reaches a service as a negative number.
    // A game adds its own keys in its own class and reads them through Int/Bool/Float here.
    public sealed class GameRemoteConfig
    {
        [Serializable] sealed class IntArray { public int[] values; }
        [Serializable] sealed class StringArray { public string[] values; }
        [Serializable] sealed class DifficultyBonus { public int hard; public int very_hard; }

        readonly IRemoteConfigService _remote;

        public GameRemoteConfig(IRemoteConfigService remoteConfig) => _remote = remoteConfig;

        public int MaxLives => PositiveInt(RemoteConfigKeys.MaxLives);
        public int LifeRefillMinutes => PositiveInt(RemoteConfigKeys.LifeRefillMinutes);
        public int LifeRefillCoinCost => PositiveInt(RemoteConfigKeys.LifeRefillCoinCost);
        public int LivesPerAd => PositiveInt(RemoteConfigKeys.LivesPerAd);
        public int CoinsPerWin => PositiveInt(RemoteConfigKeys.CoinsPerWin);
        public int WinRewardMultiplier => PositiveInt(RemoteConfigKeys.WinRewardMultiplier);
        public int StartingCoins => PositiveInt(RemoteConfigKeys.StartingCoins);
        public int ReviveCoinCost => PositiveInt(RemoteConfigKeys.ReviveCoinCost);
        public int ReviveCostStep => PositiveInt(RemoteConfigKeys.ReviveCostStep);
        public int BoosterAmountPerPurchase => PositiveInt(RemoteConfigKeys.BoosterAmountPerPurchase);
        public float TutorialHintDelaySeconds => Float(RemoteConfigKeys.HintDelaySeconds);
        public bool ExtraLifeRewardedEnabled => Bool(RemoteConfigKeys.ExtraLifeRewardedEnabled);
        public bool WinRewardedEnabled => Bool(RemoteConfigKeys.WinRewardedEnabled);
        public bool ReviveRewardedEnabled => Bool(RemoteConfigKeys.ReviveRewardedEnabled);
        public bool SkipInterstitialAfterLoss => Bool(RemoteConfigKeys.SkipInterstitialAfterLoss);

        public int TutorialUnlockLevel(E_TutorialId id, int fallback)
            => Int(RemoteConfigKeys.TutorialUnlockLevel(id.ToString(), fallback));

        public int TutorialGiftAmount(int fallback) => PositiveInt(RemoteConfigKeys.UnlockGiftAmount);

        public int FeatureIntroLevel(GameFeature feature, int fallback)
            => Int(RemoteConfigKeys.FeatureIntroLevel(feature.ToString(), fallback));

        public int DifficultyBonusCoins(E_LevelDifficulty difficulty)
        {
            if (!TryJson(RemoteConfigKeys.DifficultyCoinBonus, out DifficultyBonus bonus)) return 0;
            return difficulty == E_LevelDifficulty.VeryHard ? bonus.very_hard :
                difficulty == E_LevelDifficulty.Hard ? bonus.hard : 0;
        }

        // Keyed by the lower-case booster name; "others" covers every booster without its own row.
        public int BoosterCoinPrice(BoosterType type, int fallback)
        {
            var key = RemoteConfigKeys.BoosterCoinCost;
            var json = _remote != null ? _remote.Get(key) : key.Default;
            if (!FlatJson.TryParseInts(json, out var costs) && !FlatJson.TryParseInts(key.Default, out costs))
                return fallback;

            if (!costs.TryGetValue(ResourceNames.Of(type), out var value) || value <= 0)
                costs.TryGetValue("others", out value);
            return value > 0 ? value : fallback;
        }

        public int[] LevelOrder(int databaseCount)
        {
            var fallback = new int[Mathf.Max(0, databaseCount)];
            for (var i = 0; i < fallback.Length; i++) fallback[i] = i;

            if (!TryIntArray(RemoteConfigKeys.LevelOrder, out var source)) return fallback;

            var result = new List<int>(source.Length);
            var seen = new HashSet<int>();
            foreach (var oneBasedId in source)
            {
                var index = oneBasedId - 1;
                if (index < 0 || index >= databaseCount || !seen.Add(index)) continue;
                result.Add(index);
            }

            return result.Count > 0 ? result.ToArray() : fallback;
        }

        public int LoopStartIndex(int orderedLevelCount)
            => Mathf.Clamp(Int(RemoteConfigKeys.LevelLoopStart) - 1, 0, Mathf.Max(0, orderedLevelCount - 1));

        public int LoopEndIndex(int orderedLevelCount)
            => Mathf.Clamp(Int(RemoteConfigKeys.LevelLoopEnd) - 1, 0, Mathf.Max(0, orderedLevelCount - 1));

        public int Int(in ConfigKey<long> key)
        {
            var value = _remote != null ? _remote.Get(key) : key.Default;
            return value < int.MinValue ? int.MinValue : value > int.MaxValue ? int.MaxValue : (int)value;
        }

        public int PositiveInt(in ConfigKey<long> key)
        {
            var value = _remote != null ? _remote.Get(key) : key.Default;
            return value <= 0 ? 0 : value > int.MaxValue ? int.MaxValue : (int)value;
        }

        public float Float(in ConfigKey<double> key)
        {
            var value = _remote != null ? _remote.Get(key) : key.Default;
            return (float)value;
        }

        public bool Bool(in ConfigKey<bool> key) => _remote != null ? _remote.Get(key) : key.Default;

        public bool TryIntArray(in ConfigKey<string> key, out int[] values)
        {
            var json = _remote != null ? _remote.Get(key) : key.Default;
            return ParseIntArray(json, out values) || ParseIntArray(key.Default, out values);
        }

        public bool TryStringArray(in ConfigKey<string> key, out string[] values)
        {
            var json = _remote != null ? _remote.Get(key) : key.Default;
            return ParseStringArray(json, out values) || ParseStringArray(key.Default, out values);
        }

        public bool TryJson<T>(in ConfigKey<string> key, out T value) where T : class
        {
            var json = _remote != null ? _remote.Get(key) : key.Default;
            return ParseJson(json, out value) || ParseJson(key.Default, out value);
        }

        static bool ParseIntArray(string json, out int[] values)
        {
            values = null;
            try
            {
                values = JsonUtility.FromJson<IntArray>("{\"values\":" + json + "}")?.values;
                return values != null;
            }
            catch (Exception) { return false; }
        }

        static bool ParseStringArray(string json, out string[] values)
        {
            values = null;
            try
            {
                values = JsonUtility.FromJson<StringArray>("{\"values\":" + json + "}")?.values;
                return values != null;
            }
            catch (Exception) { return false; }
        }

        static bool ParseJson<T>(string json, out T value) where T : class
        {
            value = null;
            try
            {
                value = JsonUtility.FromJson<T>(json);
                return value != null;
            }
            catch (Exception) { return false; }
        }
    }
}
