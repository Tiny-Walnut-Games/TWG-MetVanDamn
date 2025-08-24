using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Authoring
{
    [DisallowMultipleComponent]
    public class WorldConfigurationAuthoring : MonoBehaviour
    {
        [Tooltip("Seed for deterministic world generation")] public int seed = 12345;
        [Tooltip("World bounds size (X,Z)")] public int2 worldSize = new(64, 64);
        [Tooltip("Optional target number of sectors (advisory)")] public int targetSectors = 16;
        [Tooltip("Randomization mode for adaptive rule generation")] public RandomizationMode randomizationMode = RandomizationMode.Partial;
    }
}
