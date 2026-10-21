namespace MoodBoost.Sdk
{
    // How one init attempt ended.
    //
    // Skipped is NOT a failure: it means the piece was never built for this session (no define symbol, no settings
    // asset). Merging it into Fail would read as "the store is broken" on a build that deliberately ships without IAP.
    public enum SdkInitResult
    {
        Success = 0,
        Fail = 1,
        Skipped = 2,
    }
}
