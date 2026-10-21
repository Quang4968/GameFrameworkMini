using System;
using LitMotion;
using UnityEngine;

namespace Game.Game.Animation
{
    // One pooled coin of a coin burst. Its flight runs on this object, so a spawner torn down
    // mid-burst cannot strand it outside the pool.
    //
    // No TimedDespawner: it waits on scaled time, and the win popup stops the clock while coins fly.
    public sealed class FlyingCoin : MonoBehaviour
    {
        MotionHandle _motion;

        public void Launch(in CoinThrow path, Action<FlyingCoin> landed)
        {
            _motion.TryCancel();

            var rect = (RectTransform)transform;
            var coin = path;
            Apply(rect, coin, 0f);

            _motion = LMotion.Create(0f, coin.Duration, coin.Duration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() => landed?.Invoke(this))
                .Bind(time => Apply(rect, coin, time));
        }

        // Pool Boss broadcasts this; private and parameterless on purpose.
        void OnDespawned() => _motion.TryCancel();

        void OnDestroy() => _motion.TryCancel();

        static void Apply(RectTransform rect, in CoinThrow path, float time)
        {
            path.Evaluate(time, out var position, out var scale, out var angle);
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one * scale;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
