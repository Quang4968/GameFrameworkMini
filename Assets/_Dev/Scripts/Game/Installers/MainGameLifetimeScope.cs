using Game.Core.Services;
using Game.Game.Boot;
using Game.Game.Data;
using Game.Game.Installers;
using Game.Game.SceneFlow;
using Game.Game.Services;
using Game.Game.Views.Gameplay;
using Game.Game.Views.Popup;
using MoodBoost.Sdk.Unity;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Game
{
    // The gameplay scene's container: every shared system plus the GameplayBridge the game
    // reports outcomes through. A game subclasses this and adds its board, views and bootstrap
    // in RegisterGameplay; nothing gameplay-specific lives here.
    public class MainGameLifetimeScope : LifetimeScope
    {
        [Tooltip("Level count and difficulties - the SAME asset as Boot and Home. Empty: Level 1 forever.")]
        [SerializeField] LevelCatalogSO levelCatalog;

        [Tooltip("Popup host of this scene. Empty: no Win/Lose/Settings popup opens and the level cannot end.")]
        [SerializeField] PopupHost popupHost;

        [Tooltip("Optional: a debug trigger that opens any popup by id.")]
        [SerializeField] PopupTestTrigger popupTestTrigger;

        [Tooltip("HUD prefab built into the container below at start. Empty: no HUD.")]
        [SerializeField] GameObject gameplayHudPrefab;

        [Tooltip("Where the HUD prefab is instantiated.")]
        [SerializeField] RectTransform gameplayHudContainer;

        [Tooltip("Scene transition settings. Empty: hard cut.")]
        [SerializeField] SceneTransitionSettingsSO sceneTransition;

        [Tooltip("Coin economy numbers. Empty: winning pays nothing.")]
        [SerializeField] CoinSettingsSO coinSettings;

        [Tooltip("Life economy numbers. Empty: the level costs no life and refunds none.")]
        [SerializeField] LifeSettingsSO lifeSettings;

        [Tooltip("SDK configuration - the same asset as Boot. Empty: the SDK runs in its empty mode.")]
        [SerializeField] SdkSettings sdkSettings;

        [Tooltip("Booster names, icons and prices. Empty: the buy-booster popup shows no art.")]
        [SerializeField] BoosterSettingsSO boosterSettings;

        [Tooltip("Sounds of this scene. Empty: silent.")]
        [SerializeField] AudioSettingsSO audioSettings;

        [Tooltip("Pooled prefabs of this scene. Empty: effects fall back to Instantiate with a warning.")]
        [SerializeField] PoolCatalogSO poolCatalog;

        [Tooltip("Coin flight tuning. Empty: coins are credited without the flight.")]
        [SerializeField] CoinFlightSettingsSO coinFlightSettings;

        [Tooltip("Haptic profiles for gameplay feedback. Empty: profile haptics stay silent.")]
        [SerializeField] HapticProfileSettingsSO hapticProfiles;

        [Tooltip("Lesson schedule and copy. Empty: no tutorial ever runs.")]
        [SerializeField] TutorialSettingsSO tutorialSettings;

        [Tooltip("Feature introductions on the win popup. Empty: no feature is ever introduced.")]
        [SerializeField] FeatureIntroSettingsSO featureIntroSettings;

        [Tooltip("Languages and their tables - the SAME asset in every scene. Empty: every label keeps its authored English text.")]
        [SerializeField] LocalizationSettingsSO localizationSettings;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ISceneLoader>(_ => new TransitionSceneLoader(new SceneLoader(), sceneTransition), Lifetime.Singleton);

            builder.RegisterLocalization(localizationSettings);

            // Registered BEFORE the views: they read the level index in Construct.
            builder.RegisterLevelProgress(levelCatalog);

            builder.Register<ISettingsService, PlayerPrefsSettingsService>(Lifetime.Singleton);
            builder.Register<IHapticService, SdkHapticService>(Lifetime.Singleton);

            // Gameplay haptics go through the throttle: parallel calls in one frame need one place
            // to space them out.
            builder.Register<IHapticProfileService>(
                resolver => new ThrottledHapticProfileService(resolver.Resolve<IHapticService>(), hapticProfiles),
                Lifetime.Singleton);

            // playsMusic: false keeps gameplay quiet; Home's music still fades out on entry.
            builder.Register<IAudioService>(
                resolver => new AudioService(audioSettings, resolver.Resolve<ISettingsService>(), playsMusic: false),
                Lifetime.Singleton);
            builder.RegisterBuildCallback(container => container.Resolve<IAudioService>());

            builder.Register<ICoinService>(
                resolver => new PlayerPrefsCoinService(resolver.Resolve<GameRemoteConfig>().StartingCoins), Lifetime.Singleton);

            builder.RegisterSdkSystem(sdkSettings);
            builder.RegisterLifeSystem(lifeSettings);

            // AFTER lives and level progress: the publisher reads both when the scene opens.
            builder.RegisterUserProperties();

            builder.RegisterBoosterSystem(boosterSettings);

            // Right after the booster inventory: booster buttons read both.
            builder.RegisterTutorialSystem(tutorialSettings);

            if (coinSettings != null) builder.RegisterInstance(coinSettings);

            builder.Register(resolver => new WinRewardSettings(
                    resolver.Resolve<GameRemoteConfig>(),
                    levelCatalog,
                    resolver.Resolve<ILevelProgressService>(),
                    coinSettings),
                Lifetime.Singleton);

            builder.RegisterObjectPool(poolCatalog);
            builder.RegisterCoinFlight(coinFlightSettings);

            // The game's side of the level: outcome, revive and pause all go through this one object.
            builder.Register<GameplayBridge>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            // Entering costs a life, winning refunds it; losing or leaving keeps it. A missing asset
            // makes the level free instead of breaking the scene.
            if (lifeSettings != null)
            {
                builder.RegisterEntryPoint<LevelStartLifePresenter>().AsSelf();
                builder.RegisterEntryPoint<WinLifeRefundPresenter>();
            }

            builder.RegisterEntryPoint<LevelStreakPresenter>();

            // Play time behind duration_play; LevelPlayTimePresenter in the popup block pauses it.
            builder.Register<I_LevelPlayTimeService, LevelPlayTimeService>(Lifetime.Singleton);

            // AsSelf: gameplay views resolve it directly to report booster actions.
            builder.Register<LevelTrackingPresenter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            if (gameplayHudPrefab != null)
            {
                builder.RegisterEntryPoint<GameplayHudPresenter>()
                    .WithParameter(gameplayHudPrefab)
                    .WithParameter<Transform>(gameplayHudContainer);
            }

            builder.RegisterPopupSystem(popupHost, popupTestTrigger, featureIntroSettings, coinSettings);

            RegisterGameplay(builder);
        }

        // The game's board, views and bootstrap. The default wires nothing but the entry point that
        // documents where the level index and the GameplayBridge come from.
        protected virtual void RegisterGameplay(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<MainGameBootstrap>();
        }
    }
}
