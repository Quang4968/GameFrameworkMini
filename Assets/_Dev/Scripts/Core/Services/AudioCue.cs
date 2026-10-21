namespace Game.Core.Services
{
    // What a sound means, never which file plays: the clip and its volume are AudioSettings' call.
    //
    // AudioSettings.asset stores cues by index, so new cues go at the END only - inserting one
    // shifts every later row onto the wrong cue, silently. The values below are the ones the
    // framework plays itself; a game appends its own after them.
    public enum AudioCue
    {
        ButtonClick,
        PopupOpen,
        PopupClose,
        LevelWin,
        LevelLose,
        CoinReward,
        ButtonPlay,
        ButtonConfirm,
        CoinAppear,
        PurchaseSuccess,
        CoinCollect
    }
}
