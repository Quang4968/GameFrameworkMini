using Game.Core.Domain;

namespace Game.Core.Tests
{
    // The framework's BoosterType, E_TutorialId and GameFeature carry no game values, so the tests
    // exercising schedules and inventories cast their own. A game's tests use its real values.
    public static class Boosters
    {
        public static readonly BoosterType A = (BoosterType)0;
        public static readonly BoosterType B = (BoosterType)1;
        public static readonly BoosterType C = (BoosterType)2;
        public static readonly BoosterType D = (BoosterType)3;
        public static readonly BoosterType E = (BoosterType)4;
    }

    public static class Lessons
    {
        public static readonly E_TutorialId A = (E_TutorialId)1;
        public static readonly E_TutorialId B = (E_TutorialId)2;
        public static readonly E_TutorialId C = (E_TutorialId)3;
        public static readonly E_TutorialId D = (E_TutorialId)4;
        public static readonly E_TutorialId E = (E_TutorialId)5;
    }

    public static class Features
    {
        public static readonly GameFeature A = (GameFeature)0;
        public static readonly GameFeature B = (GameFeature)1;
        public static readonly GameFeature C = (GameFeature)2;
        public static readonly GameFeature D = (GameFeature)3;
    }
}
