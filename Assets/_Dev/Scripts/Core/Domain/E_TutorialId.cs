namespace Game.Core.Domain
{
    // One guided lesson, shown once ever. Progress is persisted per value, so append new values at
    // the END only - same rule as BoosterType and PopupId.
    public enum E_TutorialId
    {
        // Level 1. No booster and no gift: the hand simply shows what a tap is for.
        HowToPlay
    }
}
