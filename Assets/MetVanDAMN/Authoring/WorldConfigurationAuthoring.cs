using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Authoring
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

    [DisallowMultipleComponent]
    public class WorldConfigurationAuthoring : MonoBehaviour
    {
        [Tooltip("Seed for deterministic world generation")] public int seed = 12345;
        [Tooltip("World bounds size (X,Z)")] public int2 worldSize = new(64, 64);
        [Tooltip("Optional target number of sectors (advisory)")] public int targetSectors = 16;
        [Tooltip("Randomization mode for adaptive rule generation")] public RandomizationMode randomizationMode = RandomizationMode.Partial;
    }

    public struct WorldConfiguration : IComponentData
    {
        public int Seed;
        public int2 WorldSize;
        public int TargetSectors;
        public RandomizationMode RandomizationMode;
    }
}
