namespace MoodBoost.Sdk
{
    // What one piece of the SDK did when it initialized: how long it took and how it ended.
    //
    // Id is an analytics id from SdkInitIds, never ModuleName or ITrackingProvider.Name: those carry spaces and
    // parentheses (AdService builds its own from the configured waterfall), and an event name containing either is
    // dropped by GA4 without a word.
    public readonly struct SdkInitReport
    {
        public readonly string Id;

        // A module of SdkRuntime, as opposed to one provider inside a module. Both levels are reported: a module can
        // succeed while the provider it wraps quietly gave up.
        public readonly bool IsModule;

        public readonly long ElapsedMs;
        public readonly SdkInitResult Result;

        // The failure message, null on success.
        public readonly string Error;

        public SdkInitReport(string id, bool isModule, long elapsedMs, SdkInitResult result, string error = null)
        {
            Id = id;
            IsModule = isModule;
            ElapsedMs = elapsedMs;
            Result = result;
            Error = error;
        }

        public static SdkInitReport Module(string id, long elapsedMs, SdkInitResult result, string error = null) =>
            new SdkInitReport(id, isModule: true, elapsedMs, result, error);

        public static SdkInitReport Provider(string id, long elapsedMs, SdkInitResult result, string error = null) =>
            new SdkInitReport(id, isModule: false, elapsedMs, result, error);
    }
}
