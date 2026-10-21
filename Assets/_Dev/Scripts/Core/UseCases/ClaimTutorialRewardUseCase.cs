using Game.Core.Domain;
using Game.Core.Services;

namespace Game.Core.UseCases
{
    // Hand over the free boosters, then remember it happened. Idempotent: the claim button, a
    // reopened popup and a relaunched app all land here, and only the first call may add anything.
    // Add before mark — marking first would drop the boosters if the grant threw.
    public static class ClaimTutorialRewardUseCase
    {
        public static bool Execute(TutorialDefinition tutorial, IBoosterInventoryService boosters,
            I_TutorialStateService state)
        {
            if (boosters == null || state == null) return false;

            if (state.GetStage(tutorial.Id) >= E_TutorialStage.RewardClaimed) return false;

            boosters.Add(tutorial.Booster, tutorial.RewardAmount);
            state.SetStage(tutorial.Id, E_TutorialStage.RewardClaimed);
            return true;
        }
    }
}
