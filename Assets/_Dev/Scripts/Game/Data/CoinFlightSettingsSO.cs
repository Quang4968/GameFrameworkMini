using Game.Game.Animation;
using UnityEngine;

namespace Game.Game.Data
{
    // Look and timing of the coin burst, shared by every place that pays out coins (win, shop...).
    // Distances are in units of the layer canvas, whose CanvasScaler fixes them to 1080x1920.
    [CreateAssetMenu(menuName = "Game/Coin Flight Settings", fileName = "CoinFlightSettings")]
    public sealed class CoinFlightSettingsSO : ScriptableObject
    {
        [Tooltip("One flying coin. Must be declared in the PoolCatalog of every scene that pays out " +
            "coins; empty turns the whole effect off.")]
        [SerializeField] FlyingCoin _coinPrefab;

        [Tooltip("Screen Space Overlay canvas the coins fly in, built once per scene. Its sorting " +
            "order must sit above popups and below the scene transition. Empty turns the effect off.")]
        [SerializeField] Canvas _layerPrefab;

        [Tooltip("Coins thrown by a x1 payout. A x2 claim throws twice as many.")]
        [SerializeField, Min(1)] int _coinsPerBurst = 24;

        [Tooltip("Upper bound on coins in one burst, whatever the multiplier.")]
        [SerializeField, Min(1)] int _maxCoins = 60;

        [Tooltip("Seconds the handful scatters before the FIRST coin leaves for the counter.")]
        [SerializeField, Min(0f)] float _scatterDuration = 0.42f;

        [Tooltip("Seconds between two coins taking off. 0 sends the whole handful at once.")]
        [SerializeField, Min(0f)] float _flyStagger = 0.045f;

        [Tooltip("Cap on the total take-off spread: a bigger burst packs tighter, not longer.")]
        [SerializeField, Min(0f)] float _maxFlySpread = 0.5f;

        [SerializeField, Min(0f)] float _flyDuration = 0.5f;

        [Tooltip("Initial throw speed, rolled per coin between these two.")]
        [SerializeField, Min(0f)] float _minThrowSpeed = 700f;

        [SerializeField, Min(0f)] float _maxThrowSpeed = 1250f;

        [Tooltip("Throw angle range in degrees; 90 is straight up.")]
        [SerializeField] Vector2 _throwAngleRange = new(20f, 160f);

        [Tooltip("Units per second squared pulling the coins down after the throw.")]
        [SerializeField, Min(0f)] float _gravity = 2100f;

        [Tooltip("Air drag. Higher brakes harder and keeps the handful tighter.")]
        [SerializeField, Min(0.01f)] float _drag = 6.5f;

        [Tooltip("Max tilt in degrees while scattering; coins straighten on the way to the counter.")]
        [SerializeField, Min(0f)] float _maxTumble = 40f;

        [Tooltip("How far the path sags before whipping up to the counter.")]
        [SerializeField, Min(0f)] float _flyDip = 70f;

        [SerializeField, Min(0f)] float _spawnScale = 0.3f;

        [SerializeField, Min(0f)] float _hoverScale = 1.45f;

        [SerializeField, Min(0f)] float _endScale = 0.7f;

        public FlyingCoin CoinPrefab => _coinPrefab;
        public Canvas LayerPrefab => _layerPrefab;
        public float ScatterDuration => _scatterDuration;
        public float FlyDuration => _flyDuration;
        public float Gravity => _gravity;
        public float Drag => _drag;
        public float FlyDip => _flyDip;
        public float SpawnScale => _spawnScale;
        public float HoverScale => _hoverScale;
        public float EndScale => _endScale;

        public int CoinCount(int multiplier) => Mathf.Clamp(_coinsPerBurst * Mathf.Max(1, multiplier), 1, _maxCoins);

        public float Spread(int count) => Mathf.Min(_flyStagger * Mathf.Max(0, count - 1), _maxFlySpread);

        // Last coin's take-off plus its flight: the whole burst is over by then.
        public float TotalDuration(int count) => _scatterDuration + Spread(count) + _flyDuration;

        public Vector2 RollVelocity()
        {
            var angle = Random.Range(_throwAngleRange.x, _throwAngleRange.y) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(_minThrowSpeed, _maxThrowSpeed);
        }

        public float RollTumble() => Random.Range(-_maxTumble, _maxTumble);
    }
}
