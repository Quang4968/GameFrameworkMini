using Game.Core.Services;
using Game.Game.Data;

namespace Game.Game.Services
{
    // The coin reward of the win popup: remote config first, CoinSettings as the fallback, plus
    // the difficulty bonus of the level just played when the catalog knows one.
    public sealed class WinRewardSettings
    {
        readonly GameRemoteConfig _remoteConfig;
        readonly ILevelCatalog _levels;
        readonly ILevelProgressService _progress;
        readonly CoinSettingsSO _fallback;

        public WinRewardSettings(
            GameRemoteConfig remoteConfig,
            ILevelCatalog levels,
            ILevelProgressService progress,
            CoinSettingsSO fallback)
        {
            _remoteConfig = remoteConfig;
            _levels = levels;
            _progress = progress;
            _fallback = fallback;
        }

        public bool RewardedEnabled => _remoteConfig == null || _remoteConfig.WinRewardedEnabled;

        public int Multiplier => _remoteConfig != null ? _remoteConfig.WinRewardMultiplier : 2;

        public int Coins
        {
            get
            {
                var coins = _remoteConfig != null
                    ? _remoteConfig.CoinsPerWin
                    : _fallback != null ? _fallback.CoinsPerWin : 0;

                if (_remoteConfig == null || _levels == null || _progress == null) return coins;
                return coins + _remoteConfig.DifficultyBonusCoins(_levels.GetDifficulty(_progress.LevelLoad));
            }
        }
    }
}
