using Unity.Entities;

namespace TinyWalnutGames.MetVD.Core
    {
    /// <summary>
    /// Optional component describing the elevation layer(s) applicable to an entity's biome.
    /// If absent, systems should assume BiomeElevation.Any (no restriction).
    /// </summary>
    public struct BiomeElevationMask : IComponentData
        {
        public BiomeElevation Value;

        public BiomeElevationMask(BiomeElevation value)
            {
            Value = value;
            }
        }
    }
