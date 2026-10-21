using Game.Core.Services;
using Game.Game.Boot;
using Game.Game.Boot.BootSteps;
using Game.Game.Data;
using Game.Game.SceneFlow;
using Game.Game.Services;
using MoodBoost.Sdk.Unity;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Game.Installers
{
    public sealed class BootLifetimeScope : LifetimeScope
    {
        [Tooltip("Level count and difficulties. Must be the SAME asset Home and MainGame use, or the level " +
            "number on the Play button drifts from the level MainGame loads. Empty: Level 1 forever.")]
        [SerializeField] LevelCatalogSO levelCatalog;

        [Tooltip("SDK configuration (mediation app key, ad units, ad frequency). Empty: the SDK runs in its " +
            "empty mode - no ads, no tracking - and the scene still works.")]
        [SerializeField] SdkSettings sdkSettings;

        [Tooltip("Scene transition settings. Empty: scenes still change, just with a hard cut.")]
        [SerializeField] SceneTransitionSettingsSO sceneTransition;

        [Tooltip("Languages and their tables - the SAME asset in every scene. Empty: every label keeps its authored English text.")]
        [SerializeField] LocalizationSettingsSO localizationSettings;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ISceneLoader>(_ => new TransitionSceneLoader(new SceneLoader(), sceneTransition), Lifetime.Singleton);

            builder.RegisterLocalization(localizationSettings);

            // InitializeSdkBootStep hands the SDK its level function from here.
            builder.RegisterLevelProgress(levelCatalog);

            // Only the session store, not the whole user-property block: Boot writes play_type when
            // it opens MainGame directly, but publishing properties belongs to Home and MainGame.
            builder.Register<I_LevelSessionService, PlayerPrefsLevelSessionService>(Lifetime.Singleton);

            // The SDK is built once per session; later scenes only re-register references to it.
            builder.RegisterSdkSystem(sdkSettings);

            // Boot steps run in ascending IBootStep.Order; add new ones here. InitializeSdkBootStep
            // is Order -100: analytics must be ready before the first event.
            builder.Register<IBootStep, InitializeSdkBootStep>(Lifetime.Singleton);

            builder.RegisterEntryPoint<BootstrapRunner>();
        }
    }
}
