using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Domain;

namespace Game.Game.Views.Popup
{
    // The only place the unlock-reward popup is opened. Same shape as BuyBoosterGate, carrying the
    // lesson instead of the booster type. The blocked work travels with the request and resumes on
    // claim, so nobody polls for the popup closing.
    public sealed class TutorialRewardGate : IDisposable
    {
        readonly IPopupService popups;
        readonly CancellationTokenSource cts = new();

        Action pendingAction;

        public TutorialRewardGate(IPopupService popups)
        {
            this.popups = popups;
        }

        // The popup reads this while rendering, so set it BEFORE opening.
        public TutorialDefinition Tutorial { get; private set; }

        public void Open(TutorialDefinition tutorial, Action onClaimed = null)
        {
            Tutorial = tutorial;

            // Overwrite, not queue: a second open is a newer intent, not an extra one.
            pendingAction = onClaimed;

            popups.OpenAsync(PopupId.BoosterUnlock, hasOverlay: true, ct: cts.Token).Forget();
        }

        // PopupBoosterUnlockController calls this once the claim button has been pressed.
        public void NotifyClaimed()
        {
            // Lift the work out BEFORE running it — it may reopen this gate.
            var action = pendingAction;
            pendingAction = null;

            popups.Close(PopupId.BoosterUnlock);
            action?.Invoke();
        }

        // Closed without claiming: the lesson runs again next level, so drop the pending work
        // instead of firing it at an unrelated moment.
        public void Cancel()
        {
            pendingAction = null;
        }

        public void Dispose()
        {
            pendingAction = null;
            cts.Cancel();
            cts.Dispose();
        }
    }
}
