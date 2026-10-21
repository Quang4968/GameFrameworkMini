using Game.Core.Domain;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Data
{
    // The framework's stand-in for a level database: only a count and a difficulty per level. A
    // game with real level data registers its own ILevelCatalog instead and leaves this asset out.
    [CreateAssetMenu(menuName = "Game/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalogSO : ScriptableObject, ILevelCatalog
    {
        [Tooltip("How many levels the game has. 0 shows \"Level 1\" forever and never advances.")]
        [SerializeField, Min(0)] int levelCount = 50;

        [Tooltip("Difficulty per level index, optional. Levels past the end of the list are Normal.")]
        [SerializeField] E_LevelDifficulty[] difficulties = new E_LevelDifficulty[0];

        public int LevelCount => levelCount;

        public E_LevelDifficulty GetDifficulty(int levelIndex)
            => levelIndex >= 0 && levelIndex < difficulties.Length ? difficulties[levelIndex] : E_LevelDifficulty.Normal;
    }
}
