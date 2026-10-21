using System;
using MoodBoost.Sdk.Tracking;
using Game.Core.Domain;
using Game.Core.Services;
using Game.Game.Services;
using VContainer.Unity;

namespace Game.Game.Boot
{
    // balance_<booster>_n, one key per booster type.
    //
    // Separate from UserPropertyPublisher because IBoosterInventoryService is only registered where the booster
    // system is - registering this alongside the others would drag a scene-only dependency into every scope.
    //
    // Each type costs one of Firebase's 25 project-wide user property slots, and BoosterType is append-only, so a
    // tenth booster breaks the ceiling. See docs/tracking.md before adding one.
    public sealed class BoosterBalancePublisher : IStartable, IDisposable
    {
        readonly ITrackingService tracking;
        readonly IBoosterInventoryService inventory;

        public BoosterBalancePublisher(ITrackingService tracking, IBoosterInventoryService inventory)
        {
            this.tracking = tracking;
            this.inventory = inventory;
        }

        public void Start()
        {
            foreach (BoosterType type in Enum.GetValues(typeof(BoosterType)))
            {
                Publish(type, inventory.GetCount(type));
            }

            inventory.CountChanged += Publish;
        }

        public void Dispose()
        {
            inventory.CountChanged -= Publish;
        }

        void Publish(BoosterType type, int count)
            => tracking.SetResourceBalance(ResourceNames.Of(type), count);
    }
}
