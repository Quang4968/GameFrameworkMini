using System;

namespace MoodBoost.Sdk
{
    // Announces how long an init took and how it ended. Implemented by SdkRuntime (one report per module) and by any
    // module that wraps providers of its own (one report per provider).
    //
    // Kept apart from ISdkModule on purpose: that three-member contract is what an external module has to implement,
    // and most modules have nothing below them to report.
    public interface ISdkInitReporter
    {
        event Action<SdkInitReport> InitCompleted;
    }
}
