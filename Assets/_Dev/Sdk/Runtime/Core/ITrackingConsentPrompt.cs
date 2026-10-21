using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk
{
    // The game's own screen asking the player to allow tracking. With no CMP in the build it IS the consent form:
    // PromptConsentModule shows it once per install and reads the answer, and on iOS the ATT system dialog follows it.
    public interface ITrackingConsentPrompt
    {
        // true when the player agreed (Continue); false when they closed the screen instead, which is not an answer -
        // nothing is remembered and the next launch asks again.
        UniTask<bool> ShowAsync(CancellationToken ct);
    }
}
