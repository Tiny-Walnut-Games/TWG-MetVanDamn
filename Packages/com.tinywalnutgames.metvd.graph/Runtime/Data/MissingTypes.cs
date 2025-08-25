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
        public BiomeType TargetBiome;
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
        private static BiomeType ConvertAffinityToBiomeType(BiomeAffinity affinity)
        {
            return affinity switch
            {
                BiomeAffinity.Forest => BiomeType.SolarPlains,
                BiomeAffinity.Desert => BiomeType.VolcanicCore,
                BiomeAffinity.Mountain => BiomeType.CrystalCaverns,
                BiomeAffinity.Ocean => BiomeType.DeepUnderwater,
                BiomeAffinity.Sky => BiomeType.SkyGardens,
                BiomeAffinity.Underground => BiomeType.ShadowRealms,
                BiomeAffinity.TechZone => BiomeType.PowerPlant,
                BiomeAffinity.Volcanic => BiomeType.VolcanicCore,
                _ => BiomeType.HubArea
            };
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
    /// World configuration component
    /// </summary>
    public struct WorldConfiguration : IComponentData
    {
        public int WorldSeed;
        public int2 WorldSize;
        public RandomizationMode RandomizationMode;
        public BiomeGenerationSettings BiomeSettings;
        public DistrictGenerationSettings DistrictSettings;
        public SectorGenerationSettings SectorSettings;
        public RoomGenerationSettings RoomSettings;
        public bool EnableDebugMode;
        public bool EnableDetailedLogging;
        
        public WorldConfiguration(int seed, int2 size, RandomizationMode mode, BiomeGenerationSettings biome, 
                                DistrictGenerationSettings district, SectorGenerationSettings sector, 
                                RoomGenerationSettings room, bool debug, bool logging)
        {
            WorldSeed = seed;
            WorldSize = size;
            RandomizationMode = mode;
            BiomeSettings = biome;
            DistrictSettings = district;
            SectorSettings = sector;
            RoomSettings = room;
            EnableDebugMode = debug;
            EnableDetailedLogging = logging;
        }
        
        // Legacy properties for backward compatibility
        public int2 BiomeCountRange => new int2(1, 4);
        public int2 DistrictCountRange => new int2(2, 6);
        public float DistrictMinDistance => 10.0f;
        public float BiomeWeight => 1.0f;
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
    /// District settings component
    /// </summary>
    public struct DistrictSettings : IComponentData
    {
        public int DistrictCount;
        public float MinDistrictDistance;
        public DistrictGenerationSettings GenerationSettings;
    }

    /// <summary>
    /// Sector settings component
    /// </summary>
    public struct SectorSettings : IComponentData
    {
        public int SectorsPerDistrict;
        public SectorGenerationSettings GenerationSettings;
    }

    /// <summary>
    /// Debug settings component
    /// </summary>
    public struct DebugSettings : IComponentData
    {
        public bool EnableVerboseLogging;
        public bool EnablePerformanceMetrics;
        public bool EnableValidationChecks;
    }

    /// <summary>
    /// World bootstrap settings component
    /// </summary>
    public struct WorldBootstrapSettings : IComponentData
    {
        public WorldConfiguration WorldConfig;
        public DebugSettings DebugConfig;
    }
}