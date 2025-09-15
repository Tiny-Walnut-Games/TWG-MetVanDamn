using Unity.Entities;

namespace TinyWalnutGames.MetVD.Core
    {
    /// <summary>
    /// Authoring-driven hint mapping a biome type to an elevation mask.
    /// Multiple hints of the same type will be OR'ed by the resolver system.
    /// </summary>
    public struct BiomeElevationHint : IComponentData
        {
        public BiomeType Type;
        public BiomeElevation Mask;
        }
    }
