using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Shared
{
    /// <summary>
    /// Randomization modes for world generation
    /// </summary>
    public enum RandomizationMode : byte
    {
        None = 0,     // Use curated biome polarities & upgrade rules
        Partial = 1,  // Randomize biome polarities, keep upgrade list fixed
        Full = 2      // Randomize biome polarities, upgrade availability, and traversal rules
    }

    /// <summary>
    /// World configuration component data for procedural generation settings
    /// </summary>
    public struct WorldConfiguration : IComponentData
    {
        public int Seed;
        public int2 WorldSize;
        public int TargetSectors;
        public RandomizationMode RandomizationMode;
    }
}