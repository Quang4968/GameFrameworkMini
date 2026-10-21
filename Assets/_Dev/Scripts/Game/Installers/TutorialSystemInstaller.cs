using Game.Core.GameLogic;
using Game.Core.Services;
using Game.Game.Data;
using Game.Game.Services;
using Game.Game.Views.Tutorial;
using VContainer;
using VContainer.Unity;

namespace Game.Game.Installers
{
    // Tutorial schedule + progress store. Needed by any scene showing booster buttons, since a
    // button reads its lock state from the schedule. Built ONCE here because BuildSchedule
    // allocates, and a button asking per frame would allocate per frame.
    public static class TutorialSystemInstaller
    {
        public static void RegisterTutorialSystem(this IContainerBuilder builder, TutorialSettingsSO settings)
        {
            // No asset = empty schedule, not null: every booster reads as unlocked and the scene
            // plays on without tutorials.
            builder.Register(resolver => settings != null
                    ? settings.BuildSchedule(resolver.Resolve<GameRemoteConfig>())
                    : TutorialSchedule.Empty,
                Lifetime.Singleton);

            builder.Register<I_TutorialStateService, PlayerPrefsTutorialStateService>(Lifetime.Singleton);

            // Next to the store it listens to, so no scope can hold one without the other.
            builder.RegisterEntryPoint<TutorialCompletionPresenter>();

            // Factory, not RegisterInstance, so an empty asset slot still resolves.
            builder.Register(_ => settings, Lifetime.Singleton);
        }
    }
}
