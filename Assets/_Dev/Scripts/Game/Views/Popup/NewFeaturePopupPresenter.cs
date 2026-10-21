using Game.Core.GameLogic;
using Game.Core.Services;
using Game.Game.Data;
using Game.Game.Services;
using VContainer.Unity;

namespace Game.Game.Views.Popup
{
    public sealed class NewFeaturePopupPresenter : IStartable
    {
        readonly ILevelProgressService levelProgress;
        readonly IFeatureIntroductionStateService state;
        readonly NewFeatureGate gate;
        readonly FeatureIntroductionSchedule schedule;

        public NewFeaturePopupPresenter(ILevelProgressService progress,
            IFeatureIntroductionStateService introductionState, NewFeatureGate featureGate,
            FeatureIntroSettingsSO settings, GameRemoteConfig remoteConfig)
        {
            levelProgress = progress;
            state = introductionState;
            gate = featureGate;
            schedule = settings != null
                ? settings.BuildSchedule(remoteConfig)
                : FeatureIntroductionSchedule.Empty;
        }

        public void Start()
        {
            if (!schedule.TryGetIntroductionAt(levelProgress.LevelShow, out var introduction)) return;
            if (state.HasShown(introduction.Feature)) return;

            gate.Open(introduction.Feature);
        }
    }
}
