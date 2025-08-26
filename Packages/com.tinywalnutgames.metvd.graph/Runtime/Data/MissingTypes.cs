using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Core data types for procedural room generation system.
    /// These types implement the complete ECS data contracts for the Graph package.
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
        public Core.BiomeType TargetBiome;
        public Polarity TargetPolarity;
        public RoomLayoutType LayoutType;
        public int CurrentStep;
        
        public RoomGenerationRequest(RoomGeneratorType generatorType, uint seed = 0, Ability skills = Ability.Jump, BiomeAffinity biome = BiomeAffinity.Any)
        {
            GeneratorType = generatorType;
            Seed = seed;
            IsComplete = false;
            AvailableSkills = skills;
            GenerationSeed = seed;
            TargetBiome = ConvertAffinityToBiomeType(biome);
            TargetPolarity = Polarity.None;
            LayoutType = RoomLayoutType.Linear;
            CurrentStep = 0;
        }
        
        public RoomGenerationRequest(RoomGeneratorType generatorType, BiomeAffinity targetBiome, Polarity targetPolarity, Ability availableSkills, uint seed)
        {
            GeneratorType = generatorType;
            Seed = seed;
            IsComplete = false;
            AvailableSkills = availableSkills;
            GenerationSeed = seed;
            TargetBiome = ConvertAffinityToBiomeType(targetBiome);
            TargetPolarity = targetPolarity;
            LayoutType = RoomLayoutType.Linear;
            CurrentStep = 0;
        }
        
        // Conversion helper
        private static Core.BiomeType ConvertAffinityToBiomeType(BiomeAffinity affinity)
        {
            return GraphTypeUtilities.ToCore(affinity);
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
        public Ability SecretSkillRequirement;
        
        public SecretAreaConfig(float probability, int minSize, int maxSize, float secretPercent, bool altRoutes, bool destructibleWalls)
        {
            Probability = probability;
            MinSize = minSize;
            MaxSize = maxSize;
            SecretAreaPercentage = secretPercent;
            MinSecretSize = new int2(minSize, minSize);
            UseAlternateRoutes = altRoutes;
            UseDestructibleWalls = destructibleWalls;
            SecretSkillRequirement = Ability.None;
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
        Horizontal = 4,
        Mixed = 5
    }

    /// <summary>
    /// World settings component (legacy compatibility)
    /// </summary>
    public struct WorldSettings : IComponentData
    {
        public int WorldSeed;
        public RandomizationMode RandomizationMode;
    }

    /// <summary>
    /// Biome settings component
    /// </summary>
    public struct BiomeSettings : IComponentData
    {
        public BiomeAffinity PrimaryBiome;
        public float BiomeWeight;
        public int BiomeVariationCount;
    }

    /// <summary>
    /// Jump arc physics configuration for calculation compatibility
    /// </summary>
    public struct JumpArcPhysics : IComponentData
    {
        public float JumpHeight;
        public float JumpDistance;
        public float DoubleJumpBonus;
        public float Gravity;
        public float WallJumpHeight;
        public float DashDistance;
        public float GlideSpeed;
        
        public JumpArcPhysics(float jumpHeight, float jumpDistance, float doubleBonus, float gravity, float wallJumpHeight, float dashDistance, float glideSpeed)
        {
            JumpHeight = jumpHeight;
            JumpDistance = jumpDistance;
            DoubleJumpBonus = doubleBonus;
            Gravity = gravity;
            WallJumpHeight = wallJumpHeight;
            DashDistance = dashDistance;
            GlideSpeed = glideSpeed;
        }
    }

    /// <summary>
    /// Utility methods for type conversions and ECS operations
    /// </summary>
    public static class GraphTypeUtilities
    {
        /// <summary>
        /// Convert uint to Entity for safe ECS operations
        /// </summary>
        public static Entity ToEntity(uint value)
        {
            return new Entity { Index = (int)(value & 0x7FFFFFFF), Version = (int)(value >> 31) };
        }

        /// <summary>
        /// Convert Entity to uint for serialization
        /// </summary>
        public static uint FromEntity(Entity entity)
        {
            return (uint)entity.Index | ((uint)entity.Version << 31);
        }

        /// <summary>
        /// Safe conversion from BiomeAffinity enum to BiomeType  
        /// </summary>
        public static Core.BiomeType ToCore(BiomeAffinity affinity)
        {
            return affinity switch
            {
                BiomeAffinity.Forest => Core.BiomeType.SolarPlains,
                BiomeAffinity.Desert => Core.BiomeType.VolcanicCore,
                BiomeAffinity.Mountain => Core.BiomeType.CrystalCaverns,
                BiomeAffinity.Ocean => Core.BiomeType.DeepUnderwater,
                BiomeAffinity.Sky => Core.BiomeType.SkyGardens,
                BiomeAffinity.Underground => Core.BiomeType.ShadowRealms,
                BiomeAffinity.TechZone => Core.BiomeType.PowerPlant,
                BiomeAffinity.Volcanic => Core.BiomeType.VolcanicCore,
                _ => Core.BiomeType.HubArea
            };
        }

        /// <summary>
        /// Create BiomeAffinityComponent from BiomeAffinity enum
        /// </summary>
        public static BiomeAffinityComponent CreateComponent(BiomeAffinity affinity)
        {
            return new BiomeAffinityComponent(affinity);
        }
    }
}