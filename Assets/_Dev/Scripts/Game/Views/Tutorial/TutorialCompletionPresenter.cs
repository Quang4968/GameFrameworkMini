using System;
using MoodBoost.Sdk.Tracking;
using Game.Core.Domain;
using Game.Core.Services;
using VContainer.Unity;

namespace Game.Game.Views.Tutorial
{
    // tutorial_complete_<lesson> for each booster lesson, once per install. Level 1 uses level_achieved_1 instead.
    // Hangs off the stored stage because every completion path writes Done and SetStage never repeats a change.
    public sealed class TutorialCompletionPresenter : IStartable, IDisposable
    {
        readonly I_TutorialStateService state;
        readonly IAttributionService attribution;

        public TutorialCompletionPresenter(I_TutorialStateService state, IAttributionService attribution)
        {
            this.state = state;
            this.attribution = attribution;
        }

        public void Start() => state.StageChanged += OnStageChanged;

        public void Dispose() => state.StageChanged -= OnStageChanged;

        // Done, not RewardClaimed: a player can take the gift and quit before doing what the lesson teaches.
        void OnStageChanged(E_TutorialId id, E_TutorialStage stage)
        {
            if (stage != E_TutorialStage.Done
                || !MmpEvents.TryTutorialComplete(id.ToString(), out var eventName)) return;

            attribution.LogEvent(eventName);
        }
    }
}
