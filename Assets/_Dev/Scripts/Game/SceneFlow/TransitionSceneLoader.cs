using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk.Unity;
using Game.Game.Data;

namespace Game.Game.SceneFlow
{
    // Wraps another ISceneLoader into: raise the loading screen -> swap the scene -> drop the loading screen.
    // The call site (the Play button, the win/lose popup...) knows nothing about the transition.
    public sealed class TransitionSceneLoader : ISceneLoader
    {
        readonly ISceneLoader inner;
        readonly SceneTransitionSettingsSO settings;

        public TransitionSceneLoader(ISceneLoader inner, SceneTransitionSettingsSO settings)
        {
            this.inner = inner;
            this.settings = settings;
        }

        public async UniTask LoadSceneAsync(string sceneName, CancellationToken ct = default)
        {
            var transition = SceneTransitionProvider.GetOrCreate(settings);
            if (transition == null)
            {
                await inner.LoadSceneAsync(sceneName, ct);
                MarkFirstScreenRevealed(sceneName);
                return;
            }

            ct.ThrowIfCancellationRequested();

            // The caller's ct stops here: it belongs to the OLD scene (its LifetimeScope is disposed and its popups
            // are destroyed the moment the scene unloads). Obeying it would strand the loading screen fully covering
            // the game mid-swap - that breaks the level itself, not just an animation.
            await transition.CloseAsync();

            // The full-cover loading screen must be DRAWN for at least one frame before the load is touched. The
            // frame that starts a load runs for hundreds of ms; landing it on the animation's last beat swallows that
            // beat whole - the eye still sees a half-faded image when the scene jumps.
            await UniTask.NextFrame();

            await inner.LoadSceneAsync(sceneName);

            // A new scene's heaviest frame comes RIGHT AFTER activation (the scene's own Awake/Start, plus Unity
            // unloading the assets nobody needs any more). Let that burst pass behind the loading screen before
            // dropping it - drop it early and the fade-out lands exactly on the hitch.
            if (settings.HoldFrames > 0) await UniTask.DelayFrame(settings.HoldFrames);

            if (settings.HoldSeconds > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(settings.HoldSeconds),
                    DelayType.UnscaledDeltaTime);
            }

            await transition.OpenAsync();

            MarkFirstScreenRevealed(sceneName);
        }

        // The end of the startup funnel: the overlay raised back in Splash is finally gone and the first screen is
        // up. Marked in both exit paths, because an unconfigured transition hard-cuts and would otherwise look like
        // every player abandoning the game during loading.
        //
        // MainGame counts too: Boot opens it directly during the onboarding run, and without this those players
        // would never reach loading_done. The mark is idempotent, so later level loads change nothing.
        static void MarkFirstScreenRevealed(string sceneName)
        {
            if (sceneName == GameScenes.Home || sceneName == GameScenes.MainGame) StartupFunnel.MarkHomeRevealed();
        }
    }
}
