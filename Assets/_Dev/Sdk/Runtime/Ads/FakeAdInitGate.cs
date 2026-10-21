namespace MoodBoost.Sdk.Ads
{
    public static class FakeAdInitGate
    {
        public static bool ShouldInitialize(MediationId id, bool useTestUnits, bool isEditor)
            => id != MediationId.Fake || isEditor || useTestUnits;
    }
}
