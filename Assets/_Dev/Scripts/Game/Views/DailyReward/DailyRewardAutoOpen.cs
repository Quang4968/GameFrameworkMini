using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.GameLogic;
using Game.Core.Services;
using Game.Game.SceneFlow;
using Game.Game.Views.Popup;
using UnityEngine;
using VContainer.Unity;

namespace Game.Game.Views.DailyReward
{
    // Opens the daily reward popup on the first Home visit of a launch that has something to claim.
    // Offered once per launch: closed unclaimed, it does not come back on the next return to Home.
    public sealed class DailyRewardAutoOpen : IStartable, IDisposable
    {
        // Process-wide on purpose (outlives the Home scope). Reset on Play because domain reload is off.
        static bool offeredThisLaunch;

        readonly IPopupService popups;
        readonly IDailyRewardService rewards;
        readonly IDailyRewardSchedule schedule;
        readonly ILevelProgressService progress;
        readonly CancellationTokenSource cts = new();

        public DailyRewardAutoOpen(IPopupService popups, IDailyRewardService rewards,
            IDailyRewardSchedule schedule, ILevelProgressService progress)
        {
            this.popups = popups;
            this.rewards = rewards;
            this.schedule = schedule;
            this.progress = progress;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetLaunch() => offeredThisLaunch = false;

        public void Start() => OpenWhenRevealedAsync(cts.Token).Forget();

        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
        }

        async UniTaskVoid OpenWhenRevealedAsync(CancellationToken ct)
        {
            if (!ShouldOpen()) return;

            // Popping up under the loading screen would play the open animation where nobody sees it.
            await UniTask.WaitWhile(() => SceneTransitionProvider.IsCovering, cancellationToken: ct);
            if (!ShouldOpen()) return;

            offeredThisLaunch = true;
            await popups.OpenAsync(PopupId.DailyReward, ct: ct);
        }

        bool ShouldOpen()
        {
            var unlocked = DailyRewardRules.IsUnlocked(progress?.LevelShow ?? 0, schedule.UnlockLevel);
            var canClaim = DailyRewardRules.CanClaim(rewards.LastClaimDay, rewards.Today);
            return DailyRewardRules.ShouldAutoOpen(offeredThisLaunch, unlocked, canClaim);
        }
    }
}
