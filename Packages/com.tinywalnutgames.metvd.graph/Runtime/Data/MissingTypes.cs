using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

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
        public bool IsComplete;
        public Ability AvailableSkills;
        public uint GenerationSeed;
        public BiomeAffinity TargetBiome;
        
        public RoomGenerationRequest(RoomGeneratorType generatorType, uint seed = 0, Ability skills = Ability.Jump, BiomeAffinity biome = BiomeAffinity.Any)
        {
            GeneratorType = generatorType;
            Seed = seed;
            IsComplete = false;
            AvailableSkills = skills;
            GenerationSeed = seed;
            TargetBiome = biome;
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
        public float MaxJumpHeight;
        public float WallJumpHeight;
        public float DashDistance;
        public float GlideSpeed;
        
        public JumpPhysicsData(float jumpHeight, float jumpDistance, float gravity)
        {
            JumpHeight = jumpHeight;
            JumpDistance = jumpDistance;
            Gravity = gravity;
            MaxJumpHeight = jumpHeight;
            WallJumpHeight = jumpHeight * 0.8f;
            DashDistance = jumpDistance * 1.5f;
            GlideSpeed = 2.0f;
        }
        
        public JumpPhysicsData(float jumpHeight, float jumpDistance, float gravity, float maxJumpHeight, float wallJumpHeight, float dashDistance, float glideSpeed)
        {
            JumpHeight = jumpHeight;
            JumpDistance = jumpDistance;
            Gravity = gravity;
            MaxJumpHeight = maxJumpHeight;
            WallJumpHeight = wallJumpHeight;
            DashDistance = dashDistance;
            GlideSpeed = glideSpeed;
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
        public float SecretAreaPercentage;
        public int2 MinSecretSize;
        public bool UseAlternateRoutes;
        public bool UseDestructibleWalls;
        
        public SecretAreaConfig(float probability, int minSize, int maxSize, float secretPercent, bool altRoutes, bool destructibleWalls)
        {
            Probability = probability;
            MinSize = minSize;
            MaxSize = maxSize;
            SecretAreaPercentage = secretPercent;
            MinSecretSize = new int2(minSize, minSize);
            UseAlternateRoutes = altRoutes;
            UseDestructibleWalls = destructibleWalls;
        }
    }

    /// <summary>
    /// Room pattern buffer element
    /// </summary>
    public struct RoomPatternElement : IBufferElementData
    {
        public int2 Position;
        public int PatternType;
        public int Rotation;
        public float Weight;
        
        public RoomPatternElement(int2 position, int patternType)
        {
            Position = position;
            PatternType = patternType;
            Rotation = 0;
            Weight = 1.0f;
        }
        
        public RoomPatternElement(int2 position, int patternType, int rotation)
        {
            Position = position;
            PatternType = patternType;
            Rotation = rotation;
            Weight = 1.0f;
        }
        
        public RoomPatternElement(int2 position, int patternType, int rotation, float weight)
        {
            Position = position;
            PatternType = patternType;
            Rotation = rotation;
            Weight = weight;
        }
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
        public float RequiredHeight;
        
        public JumpArcValidation(bool isValid, float maxDistance, float requiredHeight)
        {
            IsValid = isValid;
            MaxDistance = maxDistance;
            RequiredHeight = requiredHeight;
        }
    }

    /// <summary>
    /// Jump connection buffer element
    /// </summary>
    public struct JumpConnectionElement : IBufferElementData
    {
        public int2 FromPosition;
        public int2 ToPosition;
        public float Distance;
        public Ability RequiredSkill;
        
        public JumpConnectionElement(int2 fromPos, int2 toPos, float distance, Ability skill)
        {
            FromPosition = fromPos;
            ToPosition = toPos;
            Distance = distance;
            RequiredSkill = skill;
        }
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
    public struct BiomeInfluence : IBufferElementData
    {
        public float Strength;
        public int BiomeType;
        public BiomeAffinity Biome;
        public float Influence;
        public float Distance;
        
        public BiomeInfluence(BiomeAffinity biome, float influence, float distance)
        {
            Biome = biome;
            Influence = influence;
            Distance = distance;
            Strength = influence;
            BiomeType = (int)biome;
        }
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
        Vertical = 3,
        Horizontal = 4
    }
}