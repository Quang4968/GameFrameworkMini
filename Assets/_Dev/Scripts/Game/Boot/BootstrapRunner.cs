using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Domain;
using Game.Core.GameLogic;
using Game.Core.Services;
using Game.Game.SceneFlow;
using UnityEngine;
using VContainer.Unity;

namespace Game.Game.Boot
{
    // Runs every IBootStep in ascending Order, then hands off to the next scene.
    public sealed class BootstrapRunner : IStartable
    {
        readonly IReadOnlyList<IBootStep> steps;
        readonly ISceneLoader sceneLoader;
        readonly ILevelProgressService levelProgress;
        readonly I_LevelSessionService session;

        public BootstrapRunner(IEnumerable<IBootStep> steps, ISceneLoader sceneLoader,
            ILevelProgressService levelProgress, I_LevelSessionService session)
        {
            this.steps = steps.OrderBy(step => step.Order).ToArray();
            this.sceneLoader = sceneLoader;
            this.levelProgress = levelProgress;
            this.session = session;
        }

        public void Start()
        {
            RunAsync().Forget();
        }

        async UniTaskVoid RunAsync()
        {
            var ct = CancellationToken.None;

            foreach (var step in steps)
            {
                Debug.Log($"[Boot] {step.StepName} ...");
                await step.ExecuteAsync(ct);
            }

            // The onboarding run opens straight into gameplay — Home is not reachable until it ends.
            if (OnboardingRules.SkipsHome(levelProgress.LevelShow))
            {
                session?.SetNextPlayType(E_LevelPlayType.Home);
                await sceneLoader.LoadSceneAsync(GameScenes.MainGame, ct);
                return;
            }

            await sceneLoader.LoadSceneAsync(GameScenes.Home, ct);
        }
    }
}
