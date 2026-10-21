using LitMotion;
using UnityEngine;

namespace Game.Game.Animation
{
    // Idle bob of a booster icon around its authored position.
    // Sits on the icon, not the button: the button is placed by the bar's layout group.
    public sealed class BoosterIconFloat : MonoBehaviour
    {
        [Tooltip("Peak offset above and below the authored position, in UI units.")]
        [SerializeField, Min(0f)] float _amplitude = 6f;

        [Tooltip("Seconds for one full up-and-down cycle.")]
        [SerializeField, Min(0.01f)] float _period = 2.4f;

        [Tooltip("Cycle fraction added per sibling of the button, so neighbours bob out of step.")]
        [SerializeField, Range(0f, 1f)] float _phaseStep = 0.2f;

        RectTransform _rect;
        Vector2 _basePosition;
        MotionHandle _motion;

        void Awake()
        {
            _rect = (RectTransform)transform;
            _basePosition = _rect.anchoredPosition;
        }

        // Starts from the sine's zero crossing at the phase offset, so enabling never snaps the icon.
        void OnEnable()
        {
            var phase = transform.parent != null ? transform.parent.GetSiblingIndex() * _phaseStep : 0f;

            _motion = LMotion.Create(phase, phase + 1f, _period)
                .WithLoops(-1, LoopType.Restart)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .Bind(SetCycle);
        }

        void OnDisable()
        {
            _motion.TryCancel();
            _rect.anchoredPosition = _basePosition;
        }

        void SetCycle(float cycle)
        {
            var offset = Mathf.Sin(cycle * Mathf.PI * 2f) * _amplitude;
            _rect.anchoredPosition = _basePosition + new Vector2(0f, offset);
        }
    }
}
