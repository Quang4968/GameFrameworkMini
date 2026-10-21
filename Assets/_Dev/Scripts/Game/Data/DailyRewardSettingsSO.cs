using System;
using System.Globalization;
using UnityEngine;

namespace Game.Game.Data
{
    // How each daily reward item id is drawn, plus the claim animation timing. The ids are the
    // game's own (remote config daily_reward_cycle); a new id only needs a row here and a mapping in
    // the game's IDailyRewardGranter.
    [CreateAssetMenu(menuName = "Game/Daily Reward Settings", fileName = "DailyRewardSettings")]
    public sealed class DailyRewardSettingsSO : ScriptableObject
    {
        [Serializable]
        public struct ItemVisual
        {
            [Tooltip("Item id exactly as written in the remote config JSON, e.g. 'shuffle'.")]
            public string id;

            [Tooltip("Icon on the day card. Empty: the slot shows its amount label alone.")]
            public Sprite icon;

            [Tooltip("Amount label, {0} is the amount: 'x{0}' for a count, '{0}m' for minutes. " +
                "Empty: the plain number.")]
            public string amountFormat;
        }

        [Tooltip("One row per item id. An id without a row still pays out but shows as a bare number.")]
        [SerializeField] ItemVisual[] items = Array.Empty<ItemVisual>();

        [Tooltip("Seconds the claimed tick takes to stamp onto today's card.")]
        [SerializeField, Min(0f)] float stampSeconds = 0.3f;

        [Tooltip("Seconds the popup stays up after a claim so the player sees the tick land.")]
        [SerializeField, Min(0f)] float closeDelaySeconds = 0.6f;

        [Tooltip("Gift drawn on a chest day's card and on its reveal panel. Empty: a chest card shows no picture.")]
        [SerializeField] Sprite chestIcon;

        [Tooltip("Seconds the chest reveal panel takes to fade in.")]
        [SerializeField, Min(0f)] float revealFadeSeconds = 0.2f;

        [Tooltip("Seconds each revealed item takes to pop in.")]
        [SerializeField, Min(0f)] float revealItemSeconds = 0.3f;

        [Tooltip("Seconds between two revealed items starting to pop in.")]
        [SerializeField, Min(0f)] float revealItemStaggerSeconds = 0.12f;

        [Tooltip("Peak scale of the claimable day's breathing pulse; 1 turns the pulse off.")]
        [SerializeField, Min(1f)] float claimablePulseScale = 1.05f;

        [Tooltip("Seconds for one grow or one shrink of the claimable pulse; 0 turns it off.")]
        [SerializeField, Min(0f)] float claimablePulseSeconds = 0.6f;

        public float StampSeconds => stampSeconds;
        public float CloseDelaySeconds => closeDelaySeconds;
        public Sprite ChestIcon => chestIcon;
        public float RevealFadeSeconds => revealFadeSeconds;
        public float RevealItemSeconds => revealItemSeconds;
        public float RevealItemStaggerSeconds => revealItemStaggerSeconds;
        public float ClaimablePulseScale => claimablePulseScale;
        public float ClaimablePulseSeconds => claimablePulseSeconds;

        public Sprite Icon(string id) => TryGet(id, out var visual) ? visual.icon : null;

        public string AmountLabel(string id, int amount)
        {
            var format = TryGet(id, out var visual) ? visual.amountFormat : null;
            return string.IsNullOrEmpty(format)
                ? amount.ToString(CultureInfo.InvariantCulture)
                : string.Format(CultureInfo.InvariantCulture, format, amount);
        }

        bool TryGet(string id, out ItemVisual visual)
        {
            foreach (var item in items)
            {
                if (item.id != id) continue;

                visual = item;
                return true;
            }

            visual = default;
            return false;
        }
    }
}
