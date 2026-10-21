namespace Game.Core.Domain
{
    // How far the player got through one tutorial. Compared with <, so keep the values ordered and
    // append-only. RewardClaimed is the mark that MATTERS: from there on the lesson never runs
    // again. Done only records that the player also used the booster.
    public enum E_TutorialStage
    {
        NotStarted = 0,

        // Gift taken. The tutorial is over as far as showing it goes — it never opens again.
        RewardClaimed = 1,

        // Player also used the booster once. Gameplay gates nothing on it, but booster lessons send their MMP
        // tutorial_complete_<lesson> event on reaching it - see TutorialCompletionPresenter.
        Done = 2
    }
}
