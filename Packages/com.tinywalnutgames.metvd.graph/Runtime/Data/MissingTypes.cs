using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Temporary stub types to resolve compilation errors.
    /// These types are referenced throughout the Graph package but not yet implemented.
    /// TODO: Implement proper functionality for these types.
    /// </summary>

    /// <summary>
    /// Room generation request component
    /// </summary>
    public struct RoomGenerationRequest : IComponentData
    {
        public RoomGeneratorType GeneratorType;
        public uint Seed;
        
        public RoomGenerationRequest(RoomGeneratorType generatorType, uint seed = 0)
        {
            GeneratorType = generatorType;
            Seed = seed;
        }
    }

    /// <summary>
    /// Jump physics configuration data
    /// </summary>
    public struct JumpPhysicsData : IComponentData
    {
        public float JumpHeight;
        public float JumpDistance;
        public float Gravity;
        
        public JumpPhysicsData(float jumpHeight, float jumpDistance, float gravity)
        {
            JumpHeight = jumpHeight;
            JumpDistance = jumpDistance;
            Gravity = gravity;
        }
    }

    /// <summary>
    /// Secret area configuration
    /// </summary>
    public struct SecretAreaConfig : IComponentData
    {
        public float Probability;
        public int MinSize;
        public int MaxSize;
    }

    /// <summary>
    /// Room pattern buffer element
    /// </summary>
    public struct RoomPatternElement : IBufferElementData
    {
        public int2 Position;
        public int PatternType;
    }

    /// <summary>
    /// Room module buffer element
    /// </summary>
    public struct RoomModuleElement : IBufferElementData
    {
        public int ModuleId;
        public int2 Position;
    }

    /// <summary>
    /// Jump arc validation component
    /// </summary>
    public struct JumpArcValidation : IComponentData
    {
        public bool IsValid;
        public float MaxDistance;
    }

    /// <summary>
    /// Jump connection buffer element
    /// </summary>
    public struct JumpConnectionElement : IBufferElementData
    {
        public int2 FromPosition;
        public int2 ToPosition;
        public float Distance;
    }

    /// <summary>
    /// Skill tag for player abilities
    /// </summary>
    public struct SkillTag : IComponentData
    {
        public uint SkillMask;
    }

    /// <summary>
    /// Biome influence component
    /// </summary>
    public struct BiomeInfluence : IComponentData
    {
        public float Strength;
        public int BiomeType;
    }

    /// <summary>
    /// Biome affinity component wrapper for ECS usage
    /// </summary>
    public struct BiomeAffinityComponent : IComponentData
    {
        public BiomeAffinity Affinity;
        
        public BiomeAffinityComponent(BiomeAffinity affinity)
        {
            Affinity = affinity;
        }
    }

    /// <summary>
    /// Room layout type enumeration
    /// </summary>
    public enum RoomLayoutType
    {
        Linear = 0,
        Branched = 1,
        Open = 2,
        Vertical = 3
    }
}