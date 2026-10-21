namespace Game.Core.Domain
{
    // How the player got into the level they are playing. Recorded when the scene is asked for, read by the level
    // tracking events - which is why it lives in Core and not on the SDK's own PlayType: Core cannot see the SDK.
    public enum E_LevelPlayType
    {
        // Entered from the home screen.
        Home,

        // "Next" after a win.
        Next,

        // "Replay" after a win or a loss.
        Restart,
    }
}
