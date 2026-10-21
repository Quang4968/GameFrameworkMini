using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Game.Boot
{
    public interface IBootStep
    {
        int Order { get; }
        string StepName { get; }
        UniTask ExecuteAsync(CancellationToken ct);
    }
}
