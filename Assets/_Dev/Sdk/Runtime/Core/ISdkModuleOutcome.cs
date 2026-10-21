namespace MoodBoost.Sdk
{
    // Implemented by a module that can fail WITHOUT throwing - the store not connecting, a dependency check coming
    // back unavailable. Those paths return normally, so "no exception" would otherwise be reported as success and the
    // dashboard would show a healthy step for an SDK that never came up.
    public interface ISdkModuleOutcome
    {
        SdkInitResult InitResult { get; }

        // Why it did not succeed, null otherwise.
        string InitError { get; }
    }
}
