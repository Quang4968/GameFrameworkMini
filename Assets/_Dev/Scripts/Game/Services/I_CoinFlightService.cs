using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Game.Services
{
    // Throws a burst of pooled coins from any UI element to the coin counter on screen.
    // Lives in Game/Services, not Core, because it speaks RectTransform.
    public interface I_CoinFlightService
    {
        // Completes when the last coin lands, or at once when there is nothing to fly to.
        // multiplier: a x2 payout throws twice the coins.
        UniTask PlayAsync(RectTransform source, int multiplier, CancellationToken ct);

        // Freezes every counter until disposed; take it BEFORE the coins are credited when crediting
        // happens elsewhere (store delivery), or the number jumps ahead of the coins.
        IDisposable HoldCounters();

        // The most recently added target that is still active receives the coins.
        void AddTarget(I_CoinFlightTarget target);

        void RemoveTarget(I_CoinFlightTarget target);
    }
}
