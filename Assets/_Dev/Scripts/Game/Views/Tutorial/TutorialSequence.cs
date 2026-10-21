using System;
using Game.Core.Domain;
using UnityEngine;

namespace Game.Game.Views.Tutorial
{
    // One lesson as an ordered list of steps. Nothing but wiring lives here, so teaching another
    // booster is a prefab object rather than new code.
    public sealed class TutorialSequence : MonoBehaviour
    {
        [Tooltip("Bài học này dạy cái gì. TutorialDirector tìm sequence theo đúng id này.")]
        [SerializeField] E_TutorialId id;

        [Tooltip("Các bước, chạy theo đúng thứ tự trong danh sách.")]
        [SerializeField] TutorialStep[] steps;

        Action onFinished;
        int current = -1;

        public E_TutorialId Id => id;

        public bool IsPlaying => current >= 0;

        // Whether this lesson takes the screen at any point. A lesson that only points leaves the
        // player every control they already had.
        public bool BlocksInput
        {
            get
            {
                for (int i = 0; i < Length; i++)
                {
                    var step = StepAt(i);
                    if (step != null && step.BlocksOtherInput) return true;
                }

                return false;
            }
        }

        public void Play(TutorialRunContext context, Action finished)
        {
            if (IsPlaying) return;

            onFinished = finished;
            Run(context, 0);
        }

        public void Stop()
        {
            if (!IsPlaying) return;

            var step = StepAt(current);
            current = -1;
            onFinished = null;

            step?.Cancel();
        }

        void Run(TutorialRunContext context, int index)
        {
            // A half-filled list in the Inspector must not stall the lesson silently.
            while (index < Length && StepAt(index) == null) index++;

            if (index >= Length)
            {
                var finished = onFinished;
                current = -1;
                onFinished = null;
                finished?.Invoke();
                return;
            }

            current = index;
            StepAt(index).Begin(context, () => Run(context, index + 1), Stop);
        }

        int Length => steps != null ? steps.Length : 0;

        TutorialStep StepAt(int index) => index >= 0 && index < Length ? steps[index] : null;
    }
}
