namespace Game.Core.Domain
{
    // The boosters a player buys and spends. The inventory stores counts per value, so a game
    // appends its own values here and never reorders them: PlayerPrefs keys and BoosterSettings
    // rows follow the order. The framework itself names no booster.
    public enum BoosterType
    {
    }
}
