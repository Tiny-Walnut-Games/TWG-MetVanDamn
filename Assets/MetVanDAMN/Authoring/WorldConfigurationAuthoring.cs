using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Authoring
{
    [DisallowMultipleComponent]
    public class WorldConfigurationAuthoring : MonoBehaviour
    {
        [Tooltip("Seed for deterministic world generation")] public int seed = 12345;
        [Tooltip("World bounds size (X,Z)")] public int2 worldSize = new(64, 64);
        [Tooltip("Optional target number of sectors (advisory)")] public int targetSectors = 16;
    }

    public struct WorldConfiguration : IComponentData
    {
        public int Seed;
        public int2 WorldSize;
        public int TargetSectors;
    }
}
