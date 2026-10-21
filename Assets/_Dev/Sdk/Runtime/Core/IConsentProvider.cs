namespace MoodBoost.Sdk
{
    // The player's consent state, read by the ad networks BEFORE init.
    //
    // Split into an interface because every market has its own law (GDPR in the EU, CCPA in California, COPPA
    // when the game is child-directed) and because the real consent prompt (UMP, Funding Choices...) is yet
    // another third-party SDK — it has to plug in here without touching AdService.
    public interface IConsentProvider
    {
        bool HasGdprConsent { get; }

        bool HasCcpaOptOut { get; }

        bool IsChildDirected { get; }
    }
}
