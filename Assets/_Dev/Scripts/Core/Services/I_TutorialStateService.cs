using System;
using Game.Core.Domain;

namespace Game.Core.Services
{
    // How far the player got through each tutorial. Core knows only the interface; storage is
    // Game's job, same as IBoosterInventoryService. The stage only moves FORWARD — SetStage ignores
    // a lower value, or replaying a level would hand out the free boosters a second time.
    public interface I_TutorialStateService
    {
        E_TutorialStage GetStage(E_TutorialId id);

        event Action<E_TutorialId, E_TutorialStage> StageChanged;

        void SetStage(E_TutorialId id, E_TutorialStage stage);
    }
}
