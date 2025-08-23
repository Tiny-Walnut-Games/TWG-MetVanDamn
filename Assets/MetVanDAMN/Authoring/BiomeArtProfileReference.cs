using Unity.Entities;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// Component data linking a biome entity to its art profile reference
    /// </summary>
    public struct BiomeArtProfileReference : IComponentData
    {
        /// <summary>
        /// Reference to the BiomeArtProfile ScriptableObject asset
        /// </summary>
        public UnityObjectRef<BiomeArtProfile> ProfileRef;
        
        /// <summary>
        /// Whether this art profile has been applied to the world
        /// </summary>
        public bool IsApplied;
        
        /// <summary>
        /// Projection type for this biome's tilemap generation
        /// </summary>
        public ProjectionType ProjectionType;
    }
    
    /// <summary>
    /// Projection types supported by the Grid Layer Editor
    /// </summary>
    public enum ProjectionType : byte
    {
        Platformer = 0,
        TopDown = 1,
        Isometric = 2,
        Hexagonal = 3
    }
}