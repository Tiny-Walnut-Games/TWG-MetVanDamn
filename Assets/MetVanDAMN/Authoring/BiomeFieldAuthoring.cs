using UnityEngine;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Biome;

namespace TinyWalnutGames.MetVD.Authoring
{
    public class BiomeFieldAuthoring : MonoBehaviour
    {
        public BiomeType primaryBiome = BiomeType.SolarPlains;
        public BiomeType secondaryBiome = BiomeType.Unknown;
        [Range(0f,1f)] public float strength = 1f;
        [Range(0f,1f)] public float gradient = 0.5f;
    }
}
