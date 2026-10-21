using System;
using System.Text;
using Game.Core.Domain;
using Game.Core.Domain.DailyReward;
using Game.Core.Domain.Shop;

namespace Game.Core.GameLogic
{
    // Daily reward item ids mapped onto the shop's reward lines, so a daily gift pays out through
    // GrantShopRewardsUseCase exactly like a bought pack. Any id that is not a currency names a
    // booster in snake_case ("extra_slot" -> BoosterType.ExtraSlot): a game gets its boosters for free.
    public static class DailyRewardPayoutRules
    {
        public const string Coins = "coins";
        public const string Lives = "lives";
        public const string UnlimitedLivesMinutes = "unlimited_lives_minutes";

        public static bool TryToShopReward(in DailyRewardItem item, out ShopReward reward)
        {
            reward = default;
            if (item.Amount <= 0 || string.IsNullOrEmpty(item.Id)) return false;

            switch (item.Id)
            {
                case Coins: reward = new ShopReward(E_ShopRewardKind.Coins, item.Amount); return true;
                case Lives: reward = new ShopReward(E_ShopRewardKind.Lives, item.Amount); return true;
                case UnlimitedLivesMinutes: reward = new ShopReward(E_ShopRewardKind.UnlimitedLives, item.Amount); return true;
            }

            if (!TryParseBooster(item.Id, out var booster)) return false;

            reward = new ShopReward(E_ShopRewardKind.Booster, item.Amount, booster);
            return true;
        }

        public static string IdOf(BoosterType booster)
        {
            var name = booster.ToString();
            var id = new StringBuilder(name.Length + 4);
            for (var i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i])) id.Append('_');
                id.Append(char.ToLowerInvariant(name[i]));
            }

            return id.ToString();
        }

        static bool TryParseBooster(string id, out BoosterType booster)
        {
            var name = new StringBuilder(id.Length);
            var upper = true;
            foreach (var c in id)
            {
                if (c == '_') { upper = true; continue; }
                name.Append(upper ? char.ToUpperInvariant(c) : c);
                upper = false;
            }

            // IsDefined as well: Enum.TryParse also accepts "3", which names no booster.
            return Enum.TryParse(name.ToString(), false, out booster) && Enum.IsDefined(typeof(BoosterType), booster);
        }
    }
}
