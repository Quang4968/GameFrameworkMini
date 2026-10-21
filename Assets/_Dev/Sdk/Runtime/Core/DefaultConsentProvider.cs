namespace MoodBoost.Sdk
{
    // The consent reader used when no CMP is plugged in: treat the player as having consented.
    //
    // Correct only for a game released outside the EEA/UK. Releasing there MUST replace this with a CMP-backed
    // implementation - leaving this one in place means sending ad requests with the wrong consent flags.
    //
    // Child-directed is the exception: it is declared by the publisher in SdkSettings rather than asked of the player,
    // so it stays accurate with no CMP present and still reaches the ad networks as COPPA.
    public sealed class DefaultConsentProvider : IConsentProvider
    {
        readonly bool childDirected;

        public DefaultConsentProvider(bool childDirected = false)
        {
            this.childDirected = childDirected;
        }

        public bool HasGdprConsent => true;

        public bool HasCcpaOptOut => false;

        public bool IsChildDirected => childDirected;
    }
}
