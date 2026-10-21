using System.Collections;
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk.Unity;
using Game.Game.Data;
using Game.Game.SceneFlow;
using Game.Game.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Game.Splash
{
    // Deliberately dependency-free: no VContainer, no async pipeline, no logic.
    // The only job of this scene is to paint the logo/background, hold it on screen
    // long enough to read, then get off screen.
    //
    // The transition alone reaches for SceneTransitionProvider directly instead of the container: Splash runs before
    // Boot, so there is no LifetimeScope in existence yet to inject one from.
    public sealed class SplashBootstrap : MonoBehaviour
    {
        [SerializeField] float minDisplaySeconds = 3f;

        [Tooltip("The loading screen is raised here in Splash and stays up through all of Boot - it IS Boot's " +
            "waiting screen. Leave it empty and Splash hard-cuts to Boot.")]
        [SerializeField] SceneTransitionSettingsSO sceneTransition;

        [Tooltip("Languages - the same asset as Boot. Built here so the splash and the loading screen are localized. " +
            "Empty: they keep their authored English text.")]
        [SerializeField] LocalizationSettingsSO localizationSettings;

        void Start()
        {
            LocalizationProvider.GetOrCreate(localizationSettings);
            StartCoroutine(HoldThenLoadBoot());
        }

        IEnumerator HoldThenLoadBoot()
        {
            yield return new WaitForSeconds(minDisplaySeconds);

            var transition = SceneTransitionProvider.GetOrCreate(sceneTransition);
            if (transition != null)
            {
                // Cover only, never uncover: the loading screen comes down at the end of the Home load, and that is
                // TransitionSceneLoader's job.
                yield return transition.CloseAsync().ToCoroutine();
            }

            // Splash has done its job: the hold is over and the overlay is covering. Called directly rather than
            // through a service because this scene has no container to inject one from.
            StartupFunnel.MarkSplashLoaded();

            SceneManager.LoadScene(GameScenes.Boot, LoadSceneMode.Single);
        }
    }
}
