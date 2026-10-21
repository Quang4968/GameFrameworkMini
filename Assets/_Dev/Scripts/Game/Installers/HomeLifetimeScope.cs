using Game.Core.Services;
using Game.Game.Boot;
using Game.Game.Data;
using Game.Game.SceneFlow;
using Game.Game.Services;
using Game.Game.Views.DailyReward;
using Game.Game.Views.Popup;
using MoodBoost.Sdk.Unity;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Game.Installers
{
    public sealed class HomeLifetimeScope : LifetimeScope
    {
        [Tooltip("Popup host of this scene. Empty: no popup opens here (settings and out-of-lives buttons go dead).")]
        [SerializeField] PopupHost popupHost;

        [Tooltip("SDK configuration - the same asset as Boot. Empty: the SDK runs in its empty mode.")]
        [SerializeField] SdkSettings sdkSettings;

        [Tooltip("Booster names, icons and prices. Empty: the inventory still works, popups show no booster art.")]
        [SerializeField] BoosterSettingsSO boosterSettings;

        [Tooltip("Optional: a debug trigger that opens any popup by id.")]
        [SerializeField] PopupTestTrigger popupTestTrigger;

        [Tooltip("Avatar sprites. Empty: the profile popup cannot open.")]
        [SerializeField] AvatarCatalogSO avatarCatalog;

        [Tooltip("Sounds and music of this scene. Empty: silent.")]
        [SerializeField] AudioSettingsSO audioSettings;

        [Tooltip("Level count and difficulties - the SAME asset as Boot and MainGame. Empty: Level 1 forever.")]
        [SerializeField] LevelCatalogSO levelCatalog;

        [Tooltip("Coin economy numbers. Empty: no coin reward at all.")]
        [SerializeField] CoinSettingsSO coinSettings;

        [Tooltip("Life economy numbers. Empty: Play never blocks on lives and the out-of-lives popup cannot open.")]
        [SerializeField] LifeSettingsSO lifeSettings;

        [Tooltip("Scene transition settings. Empty: hard cut.")]
        [SerializeField] SceneTransitionSettingsSO sceneTransition;

        [Tooltip("Pooled prefabs of this scene (coin burst). Empty: effects fall back to Instantiate with a warning.")]
        [SerializeField] PoolCatalogSO poolCatalog;

        [Tooltip("Coin flight tuning. Empty: coins are credited without the flight.")]
        [SerializeField] CoinFlightSettingsSO coinFlightSettings;

        [Tooltip("Daily reward item icons and claim timing. Empty: the daily reward popup never opens.")]
        [SerializeField] DailyRewardSettingsSO dailyRewardSettings;

        [Tooltip("Languages and their tables - the SAME asset in every scene. Empty: every label keeps its authored English text.")]
        [SerializeField] LocalizationSettingsSO localizationSettings;

        protected override void Configure(IContainerBuilder builder)
        {
            // The Boot scope is gone by now, so everything is registered again here.
            builder.Register<ISceneLoader>(_ => new TransitionSceneLoader(new SceneLoader(), sceneTransition), Lifetime.Singleton);

            builder.RegisterLocalization(localizationSettings);

            builder.RegisterLevelProgress(levelCatalog);

            // Music/sound/haptic flags sit outside the popup block: views outside popups read them too.
            builder.Register<ISettingsService, PlayerPrefsSettingsService>(Lifetime.Singleton);
            builder.Register<IHapticService, SdkHapticService>(Lifetime.Singleton);

            // The service dies with the scene, the AudioSource host does not, so a scene change
            // never cuts the music.
            builder.Register<IAudioService>(
                resolver => new AudioService(audioSettings, resolver.Resolve<ISettingsService>()),
                Lifetime.Singleton);

            // Built eagerly: the music must start when the scene opens, not on the first button.
            builder.RegisterBuildCallback(container => container.Resolve<IAudioService>());

            // Before any view that takes the coin burst.
            builder.RegisterObjectPool(poolCatalog);
            builder.RegisterCoinFlight(coinFlightSettings);

            builder.Register<ICoinService>(
                resolver => new PlayerPrefsCoinService(resolver.Resolve<GameRemoteConfig>().StartingCoins), Lifetime.Singleton);

            // Name and avatar. Outside the popup block: views outside popups read it too.
            builder.Register<I_ProfileService>(
                _ => new PlayerPrefsProfileService(avatarCatalog != null ? avatarCatalog.Count : 0), Lifetime.Singleton);
            if (avatarCatalog != null) builder.RegisterInstance(avatarCatalog);

            builder.RegisterSdkSystem(sdkSettings);

            builder.RegisterLifeSystem(lifeSettings);

            // Same PlayerPrefs key as MainGame, so booster counts carry across.
            builder.RegisterBoosterSystem(boosterSettings);

            // AFTER lives and level progress: the publisher reads both when the scene opens.
            builder.RegisterUserProperties();

            builder.RegisterDailyReward<ShopDailyRewardGranter>(dailyRewardSettings);

            // Popups are registered again here - each scene has its own host.
            if (popupHost != null)
            {
                builder.RegisterComponent<IPopupHost>(popupHost);
                builder.Register<IPopupService, PopupService>(Lifetime.Singleton);

                if (popupTestTrigger != null) builder.RegisterComponent(popupTestTrigger);

                // The single place that opens the out-of-lives popup and remembers the pending action.
                builder.Register<OutOfLivesGate>(Lifetime.Singleton);

                // Blocks Play when lives are out. Needs the popup, which needs LifeSettingsSO: a scene
                // without either just does nothing on Play, instead of throwing.
                if (lifeSettings != null) builder.RegisterEntryPoint<OutOfLivesGatePresenter>();

                // The popup resolves DailyRewardSettingsSO, registered only when assigned.
                if (dailyRewardSettings != null) builder.RegisterEntryPoint<DailyRewardAutoOpen>();
            }

            // AsSelf: OutOfLivesGatePresenter listens for LivesExhausted and Home's play button calls RequestPlay.
            builder.Register<HomeBootstrap>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
