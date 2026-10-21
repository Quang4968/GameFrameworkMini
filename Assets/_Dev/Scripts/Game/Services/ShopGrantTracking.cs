using System.Collections.Generic;
using MoodBoost.Sdk.Tracking;
using Game.Core.Domain.Shop;

namespace Game.Game.Services
{
    // What GrantShopRewardsUseCase handed over, as resource_earn lines. Shared by every payout through
    // that use case (a bought pack, a daily gift), so they all report the same names and balances.
    public static class ShopGrantTracking
    {
        public static List<ResourceChange> ToChanges(IReadOnlyList<ShopGrant> granted)
        {
            var changes = new List<ResourceChange>(granted?.Count ?? 0);
            if (granted == null) return changes;

            foreach (var grant in granted)
            {
                if (grant.Amount <= 0) continue;

                changes.Add(ToChange(grant));
            }

            return changes;
        }

        static ResourceChange ToChange(in ShopGrant grant)
        {
            switch (grant.Kind)
            {
                case E_ShopRewardKind.Coins:
                    return new ResourceChange(ResourceType.Currency, ResourceNames.Coin, grant.Amount, grant.Balance);

                case E_ShopRewardKind.Lives:
                    return new ResourceChange(ResourceType.Item, ResourceNames.Life, grant.Amount, grant.Balance);

                case E_ShopRewardKind.UnlimitedLives:
                    return new ResourceChange(
                        ResourceType.Item, ResourceNames.UnlimitedLife, grant.Amount, grant.Balance);

                default:
                    return new ResourceChange(
                        ResourceType.Booster, ResourceNames.Of(grant.Booster), grant.Amount, grant.Balance);
            }
        }
    }
}
