using LitMotion;
using UnityEngine;

namespace Game.Game.Animation
{
    // One coin's path, as a pure function of elapsed time: a ballistic throw with air drag, then a
    // sagging bezier to the counter. Closed form rather than stepped, so a frame hitch cannot drift it.
    public readonly struct CoinThrow
    {
        public readonly Vector2 Origin;
        public readonly Vector2 Destination;
        public readonly Vector2 Velocity;
        public readonly float Tumble;
        public readonly float Departure;
        public readonly float FlyDuration;
        public readonly float Gravity;
        public readonly float Drag;
        public readonly float FlyDip;
        public readonly float SpawnScale;
        public readonly float HoverScale;
        public readonly float EndScale;
        public readonly float PopDuration;

        public CoinThrow(Vector2 origin, Vector2 destination, Vector2 velocity, float tumble, float departure,
            float flyDuration, float gravity, float drag, float flyDip, float spawnScale, float hoverScale,
            float endScale, float popDuration)
        {
            Origin = origin;
            Destination = destination;
            Velocity = velocity;
            Tumble = tumble;
            Departure = departure;
            FlyDuration = flyDuration;
            Gravity = gravity;
            Drag = drag;
            FlyDip = flyDip;
            SpawnScale = spawnScale;
            HoverScale = hoverScale;
            EndScale = endScale;
            PopDuration = popDuration;
        }

        public float Duration => Departure + FlyDuration;

        public void Evaluate(float time, out Vector2 position, out float scale, out float angle)
        {
            // Coins still waiting their turn keep riding the throw, so they sink gently instead of freezing.
            if (time < Departure)
            {
                position = Thrown(time);
                scale = Mathf.LerpUnclamped(SpawnScale, HoverScale,
                    EaseUtility.Evaluate(Normalized(time, PopDuration), Ease.OutBack));
                angle = Tumble * Decay(time);
                return;
            }

            // Both phases read the same closed-form position at the departure instant: no seam.
            var rest = Thrown(Departure);
            var control = rest - new Vector2(0f, FlyDip);
            var f = EaseUtility.Evaluate(Normalized(time - Departure, FlyDuration), Ease.InCubic);

            position = Vector2.Lerp(Vector2.Lerp(rest, control, f), Vector2.Lerp(control, Destination, f), f);
            scale = Mathf.Lerp(HoverScale, EndScale, f);
            angle = Mathf.Lerp(Tumble * Decay(Departure), 0f, f);
        }

        // p(t) = p0 + (v0 - g/k)(1 - e^-kt)/k + (g/k)t — drag brakes hard early, leaving the drift g/k.
        Vector2 Thrown(float time)
        {
            var terminal = new Vector2(0f, -Gravity) / Drag;
            return Origin + (Velocity - terminal) * (Decay(time) / Drag) + terminal * time;
        }

        float Decay(float time) => 1f - Mathf.Exp(-Drag * time);

        static float Normalized(float elapsed, float duration) =>
            duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
    }
}
