using MoodBoost.Sdk.Tracking;
using Game.Core.Domain;
using Game.Game.Services;

namespace Game.Game.Views.Tutorial
{
    // tutorial_action for one showing of one lesson, so a lesson shown again counts from 0 again.
    // The event names no lesson on purpose: every lesson runs on its own level, and the
    // current_level user property already rides on each event.
    public sealed class TutorialActionTracker
    {
        const string ClickCategory = "click";
        const string UseBoosterCategory = "use_booster";

        readonly ITrackingService tracking;
        readonly BoosterType booster;

        int index;
        int clicks;

        public TutorialActionTracker(ITrackingService tracking, BoosterType booster)
        {
            this.tracking = tracking;
            this.booster = booster;
        }

        public void Start() => tracking.LogTutorialStart();

        public void Report(E_TutorialAction action)
        {
            switch (action)
            {
                case E_TutorialAction.Click:
                    clicks++;
                    tracking.LogTutorialAction(ClickCategory, $"click_{clicks}", ++index);
                    break;

                case E_TutorialAction.UseBooster:
                    tracking.LogTutorialAction(UseBoosterCategory,
                        $"use_booster_{ResourceNames.Of(booster)}", ++index);
                    break;
            }
        }

        // Takes the index after the last action, closing the same count start opened at 0.
        public void Finish() => tracking.LogTutorialFinish(++index);
    }
}
