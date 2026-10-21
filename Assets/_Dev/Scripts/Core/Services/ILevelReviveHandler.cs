namespace Game.Core.Services
{
    // The game's side of a revive: whether the board can be saved and how. Coins, ads and tracking
    // around it belong to LevelReviveController in the framework.
    public interface ILevelReviveHandler
    {
        bool CanRevive { get; }
        bool TryRevive();
    }
}
