using Game.Core.Services;
using Game.Game.Data;
using Game.Game.Services;
using Game.Game.Views.Gameplay;
using Game.Game.Views.Popup;
using VContainer;
using VContainer.Unity;

namespace Game.Game.Installers
{
    // The MainGame popup block: host, service, the gates every popup opens through, and the
    // presenters that react to the level outcome. Home registers its smaller set by hand.
    public static class PopupSystemInstaller
    {
        public static void RegisterPopupSystem(
            this IContainerBuilder builder,
            PopupHost popupHost,
            PopupTestTrigger popupTestTrigger,
            FeatureIntroSettingsSO featureIntroSettings,
            CoinSettingsSO coinSettings)
        {
            if (popupHost == null) return;

            builder.RegisterComponent<IPopupHost>(popupHost);
            builder.Register<IPopupService, PopupService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameResultPopupPresenter>();
            builder.Register(_ => featureIntroSettings, Lifetime.Singleton);

            builder.Register<IFeatureIntroductionStateService,
                PlayerPrefsFeatureIntroductionStateService>(Lifetime.Singleton);
            builder.Register<NewFeatureGate>(Lifetime.Singleton);
            builder.RegisterEntryPoint<NewFeaturePopupPresenter>();

            builder.Register<LevelReviveController>(Lifetime.Singleton)
                .WithParameter<CoinSettingsSO>(coinSettings);
            builder.Register<OutOfLivesGate>(Lifetime.Singleton);
            builder.Register<BuyBoosterGate>(Lifetime.Singleton);
            builder.Register<TutorialRewardGate>(Lifetime.Singleton);

            builder.RegisterEntryPoint<LevelPlayTimePresenter>();
            builder.RegisterEntryPoint<LevelPopupPause>();

            if (popupTestTrigger != null) builder.RegisterComponent(popupTestTrigger);
        }
    }
}
