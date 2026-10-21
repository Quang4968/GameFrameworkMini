using System;
using Game.Core.Domain;

namespace Game.Core.Services
{
    public interface IFeatureIntroductionStateService
    {
        event Action<GameFeature> Shown;

        bool HasShown(GameFeature feature);
        void MarkShown(GameFeature feature);
    }
}
