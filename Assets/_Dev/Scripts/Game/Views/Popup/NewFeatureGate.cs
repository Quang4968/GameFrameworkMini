using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Domain;
using Game.Core.Services;

namespace Game.Game.Views.Popup
{
    public sealed class NewFeatureGate : IDisposable
    {
        readonly IPopupService popups;
        readonly IFeatureIntroductionStateService state;
        readonly CancellationTokenSource cts = new();

        public NewFeatureGate(IPopupService popupService, IFeatureIntroductionStateService introductionState)
        {
            popups = popupService;
            state = introductionState;
        }

        public GameFeature Feature { get; private set; }

        public void Open(GameFeature feature)
        {
            Feature = feature;
            OpenAsync(feature, cts.Token).Forget();
        }

        public void Close() => popups.Close(PopupId.NewFeature);

        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
        }

        async UniTaskVoid OpenAsync(GameFeature feature, CancellationToken ct)
        {
            var view = await popups.OpenAsync(PopupId.NewFeature, hasOverlay: true, ct: ct);
            if (view != null) state.MarkShown(feature);
        }
    }
}
