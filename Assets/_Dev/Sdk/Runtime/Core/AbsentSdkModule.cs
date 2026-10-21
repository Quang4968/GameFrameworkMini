using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk
{
    // A placeholder for a module this build does not contain (no define symbol, no settings asset). It does nothing
    // and reports itself as skipped.
    //
    // Why a module rather than a note on the side: SdkRuntime then reports it from its own loop, at the position in
    // the init order where the real thing would have run. Announcing it from outside means announcing it before the
    // init has even started, which puts every absent piece at the head of the funnel instead of where it belongs.
    public sealed class AbsentSdkModule : ISdkModule, ISdkModuleOutcome
    {
        public AbsentSdkModule(string moduleName, int initOrder)
        {
            ModuleName = moduleName + " (not built)";
            InitOrder = initOrder;
        }

        public string ModuleName { get; }

        public int InitOrder { get; }

        public SdkInitResult InitResult => SdkInitResult.Skipped;

        public string InitError => "not built";

        public UniTask InitializeAsync(CancellationToken ct) => UniTask.CompletedTask;
    }
}
