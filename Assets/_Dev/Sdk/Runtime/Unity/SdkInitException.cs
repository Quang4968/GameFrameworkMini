using System;

namespace MoodBoost.Sdk.Unity
{
    // Carries a failed init into the crash reporter, because SdkRuntime swallows the original so the boot survives it.
    // A type of its own rather than a plain Exception so the reports group by SDK instead of collapsing into one bucket.
    public sealed class SdkInitException : Exception
    {
        public SdkInitException(in SdkInitReport report)
            : base($"{report.Id} failed to init after {report.ElapsedMs}ms: {report.Error}")
        {
        }
    }
}
