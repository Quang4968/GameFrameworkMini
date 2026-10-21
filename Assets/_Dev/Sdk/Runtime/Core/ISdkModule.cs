using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk
{
    // One self-initializing piece of the SDK (tracking, remote config, ads...). SdkRuntime runs them all in
    // ascending InitOrder before reporting ready.
    //
    // Important contract: InitializeAsync must NOT let an exception escape. An SDK dying from a network failure
    // or missing configuration is an everyday event; it must not be allowed to block the game's boot.
    public interface ISdkModule
    {
        string ModuleName { get; }

        int InitOrder { get; }

        UniTask InitializeAsync(CancellationToken ct);
    }
}
