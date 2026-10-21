using Game.Core.Services;
using Game.Game.Data;
using Game.Game.Services;
using MoodBoost.Sdk.Tracking;

namespace Game.Game.Views.Gameplay
{
    // The revive offered by the Lose popup: price, affordability and payment live here; what a
    // revive does to the board is the game's ILevelReviveHandler. A successful revive reports
    // Resumed so streaks, tracking and play time pick the level back up.
    public sealed class LevelReviveController
    {
        readonly ILevelReviveHandler handler;
        readonly GameplayBridge bridge;
        readonly ICoinService coins;
        readonly IAudioService audio;
        readonly IHapticService haptics;
        readonly CoinSettingsSO coinSettings;
        readonly ITrackingService tracking;
        readonly GameRemoteConfig remoteConfig;
        int reviveCount;

        public LevelReviveController(
            ILevelReviveHandler handler,
            GameplayBridge bridge,
            ICoinService coins,
            IAudioService audio,
            IHapticService haptics,
            CoinSettingsSO coinSettings,
            ITrackingService tracking,
            GameRemoteConfig gameRemoteConfig)
        {
            this.handler = handler;
            this.bridge = bridge;
            this.coins = coins;
            this.audio = audio;
            this.haptics = haptics;
            this.coinSettings = coinSettings;
            this.tracking = tracking;
            remoteConfig = gameRemoteConfig;
        }

        public int CoinCost
        {
            get
            {
                var baseCost = remoteConfig != null ? remoteConfig.ReviveCoinCost :
                    coinSettings != null ? coinSettings.ReviveCost : 0;
                var step = remoteConfig != null ? remoteConfig.ReviveCostStep : baseCost;
                return baseCost + step * reviveCount;
            }
        }

        public int ReviveCount => reviveCount;
        public bool CanRevive => handler != null && handler.CanRevive;
        public bool CanAfford => coins != null && coins.Coins >= CoinCost;

        public bool TryReviveWithCoins() => TryRevive(CoinCost);
        public bool TryReviveFree() => TryRevive(0);

        bool TryRevive(int coinCost)
        {
            if (!CanRevive || coinCost > 0 && !CanAfford) return false;
            if (!handler.TryRevive()) return false;

            if (coinCost > 0 && coins.TrySpend(coinCost))
            {
                tracking.LogResourceSpend(
                    new ResourceChange(ResourceType.Currency, ResourceNames.Coin, coinCost, coins.Coins),
                    TrackingPlacements.Revive,
                    SpendReason.Use);
            }

            reviveCount++;
            bridge.ReportResumed();
            haptics?.Play(HapticCue.MediumImpact);
            audio?.Play(AudioCue.ButtonConfirm);
            return true;
        }
    }
}
