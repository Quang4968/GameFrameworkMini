using UnityEngine;

namespace Game.Game.Services
{
    // A coin counter that coin bursts can land on.
    public interface I_CoinFlightTarget
    {
        RectTransform Anchor { get; }

        // While held the counter keeps its old number, so it only rises once the coins land.
        void SetHeld(bool held);
    }
}
