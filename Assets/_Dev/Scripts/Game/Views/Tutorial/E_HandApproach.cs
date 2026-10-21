namespace Game.Game.Views.Tutorial
{
    // Which side the pointing hand comes in from. A target on the field is pointed at from above;
    // a button is pointed at from below, so the hand never hangs over the row of buttons under it.
    public enum E_HandApproach
    {
        FromAbove,
        FromBelow,
    }
}
