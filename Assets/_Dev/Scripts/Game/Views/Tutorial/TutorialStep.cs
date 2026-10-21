using System;
using UnityEngine;

namespace Game.Game.Views.Tutorial
{
    // One thing the player has to do. The base owns what every step does the same way: follow the
    // target, wait out the hint delay, dim the screen, report when done. Subclasses answer two
    // questions: where the target is (TryGetFocus) and what counts as done (Finish).
    // Must live on an object that is never switched off — Update drives the whole thing.
    public abstract class TutorialStep : MonoBehaviour
    {
        [Tooltip("Dims the rest of the screen and accepts taps on the target only. Also holds the " +
            "đứng yên suốt bước này — người chơi đang bị bắt bấm một chỗ, không nên thua trong lúc đó.")]
        [SerializeField] bool blocksOtherInput;

        [Tooltip("Chữ đi kèm bàn tay ở riêng bước này. Bỏ trống thì dùng câu chung của bài học " +
            "trong TutorialSettings.")]
        [SerializeField] string messageOverride;

        [Tooltip("Số giây không tương tác trước khi bàn tay hiện. Để số ÂM thì dùng số của bài học " +
            "trong TutorialSettings; 0 là hiện ngay.")]
        [SerializeField] float hintDelayOverrideSeconds = -1f;


        TutorialRunContext context;
        Action onFinished;
        Action onCancelled;

        float elapsed;
        IDisposable pauseHold;

        public bool IsRunning { get; private set; }

        // Read by the lesson as a whole: a lesson that never blocks must not freeze the model.
        public bool BlocksOtherInput => blocksOtherInput;

        // Where the target sits on screen right now and which side the hand reaches in from.
        // False means "no target at this instant" — the step keeps waiting rather than finishing,
        // because the board is still moving.
        protected abstract bool TryGetFocus(out Rect screenRect, out E_HandApproach approach);

        protected abstract void OnBegin();

        protected virtual void OnEnd() { }

        // Per-frame tick since Begin. A step that ends on TIME rather than on player action calls
        // Finish from here.
        protected virtual void OnTick(float elapsedSeconds) { }

        // What finishing this step counts as in tutorial_action. None for a step that ends on its
        // own, like a flight or a pre-filled board: only what the player does is an action.
        protected virtual E_TutorialAction TrackedAction => E_TutorialAction.None;

        // cancelled fires when the step is abandoned, NOT when it completes. Without it the
        // sequence still believes this step is running and can never play again.
        public void Begin(TutorialRunContext runContext, Action finished, Action cancelled)
        {
            if (IsRunning) return;

            context = runContext;
            onFinished = finished;
            onCancelled = cancelled;
            elapsed = 0f;
            IsRunning = true;

            // The game holds still while the player is told to press one exact thing.
            if (blocksOtherInput && context.Pause != null) pauseHold = context.Pause.TryHold();

            OnBegin();
        }

        // Abandoned rather than completed: leaving the scene, or the level ending underneath.
        public void Cancel()
        {
            if (!IsRunning) return;

            // Lift it out BEFORE calling: the sequence answers by calling Cancel again, and that
            // call must land on the !IsRunning branch above instead of looping.
            var cancelled = onCancelled;
            Teardown();
            onFinished = null;
            onCancelled = null;
            cancelled?.Invoke();
        }

        protected void Finish()
        {
            if (!IsRunning) return;

            var finished = onFinished;
            Teardown();
            onFinished = null;
            onCancelled = null;

            // Before the next step begins, so actions log in the order the player made them.
            context.Tracker?.Report(TrackedAction);
            finished?.Invoke();
        }

        // The hand is for a player who has gone quiet, not one who is trying things.
        protected void ResetHintDelay() => elapsed = 0f;

        // Virtual so a step whose screen already carries the instruction can drop the line.
        protected virtual string Message =>
            string.IsNullOrWhiteSpace(messageOverride) ? context.Message : messageOverride;

        protected float HintDelaySeconds =>
            hintDelayOverrideSeconds < 0f ? context.HintDelaySeconds : hintDelayOverrideSeconds;

        void Update()
        {
            if (!IsRunning) return;

            // Unscaled: the lesson keeps its own clock even if gameplay is slowed or stopped.
            elapsed += Time.unscaledDeltaTime;

            OnTick(elapsed);
            if (!IsRunning) return;

            if (!TryGetFocus(out var focus, out var approach))
            {
                // No target this frame: cover the screen rather than leave a hole where it was.
                if (blocksOtherInput) context.Spotlight?.CloseHole();
                context.Hand?.Hide();
                return;
            }

            if (blocksOtherInput) context.Spotlight?.OpenAt(focus);

            if (elapsed < HintDelaySeconds) return;

            var hand = context.Hand;
            if (hand == null) return;

            if (!hand.IsShowing) hand.Show(Message);
            hand.PointAt(focus, approach);
        }

        void OnDisable()
        {
            // Switched off mid-lesson: never leave the game frozen or the screen dimmed.
            if (IsRunning) Cancel();
        }

        void Teardown()
        {
            IsRunning = false;

            OnEnd();

            if (blocksOtherInput) context.Spotlight?.Close();
            context.Hand?.Hide();

            pauseHold?.Dispose();
            pauseHold = null;
        }
    }
}
