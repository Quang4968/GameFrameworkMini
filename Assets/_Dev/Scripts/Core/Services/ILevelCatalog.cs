using Game.Core.Domain;

namespace Game.Core.Services
{
    // What the framework needs to know about the game's level set: how many there are and how
    // hard each one is. The level content itself is the game's own type behind its own repository.
    public interface ILevelCatalog
    {
        int LevelCount { get; }
        E_LevelDifficulty GetDifficulty(int levelIndex);
    }
}
