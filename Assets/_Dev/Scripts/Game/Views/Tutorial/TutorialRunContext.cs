using Game.Core.Services;

namespace Game.Game.Views.Tutorial
{
    // What every step of a lesson shares: the two visuals, the copy, and the pause gate. Passed
    // down by the director so a step never has to guess which spotlight is "its own".
    public readonly struct TutorialRunContext
    {
        public TutorialSpotlight Spotlight { get; }

        public TutorialHandHint Hand { get; }

        public IGameplayPauseService Pause { get; }

        // Default line for the lesson; a step may override it.
        public string Message { get; }

        // Seconds of no interaction before the hand appears — the design sheet's X.
        public float HintDelaySeconds { get; }

        // Shared by the whole lesson, so action_index keeps counting from one step to the next.
        public TutorialActionTracker Tracker { get; }

        public TutorialRunContext(TutorialSpotlight spotlight, TutorialHandHint hand,
            IGameplayPauseService pause, string message, float hintDelaySeconds, TutorialActionTracker tracker)
        {
            Spotlight = spotlight;
            Hand = hand;
            Pause = pause;
            Message = message;
            HintDelaySeconds = hintDelaySeconds;
            Tracker = tracker;
        }
    }
}
