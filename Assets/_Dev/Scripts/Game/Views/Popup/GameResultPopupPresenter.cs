using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Services;
using VContainer.Unity;

namespace Game.Game.Views.Popup
{
    // Opens the Win or Lose popup when the level ends. The outcome comes through
    // ILevelOutcomeSource, so no gameplay view is named here.
    public sealed class GameResultPopupPresenter : IStartable, IDisposable
    {
        readonly ILevelOutcomeSource outcome;
        readonly IPopupService popupService;
        readonly IAudioService audio;
        readonly CancellationTokenSource cts = new();

        public GameResultPopupPresenter(ILevelOutcomeSource outcome, IPopupService popupService, IAudioService audio)
        {
            this.outcome = outcome;
            this.popupService = popupService;
            this.audio = audio;
        }

        public void Start()
        {
            outcome.Won += OpenWinPopup;
            outcome.Lost += OpenLosePopup;
        }

        public void Dispose()
        {
            outcome.Won -= OpenWinPopup;
            outcome.Lost -= OpenLosePopup;
            cts.Cancel();
            cts.Dispose();
        }

        void OpenWinPopup()
        {
            audio?.Play(AudioCue.LevelWin);
            popupService.OpenAsync(PopupId.Win, hasOverlay: true, ct: cts.Token).Forget();
        }

        void OpenLosePopup()
        {
            audio?.Play(AudioCue.LevelLose);
            popupService.OpenAsync(PopupId.Lose, hasOverlay: true, ct: cts.Token).Forget();
        }
    }
}
