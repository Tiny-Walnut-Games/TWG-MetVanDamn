using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Room generation types aligned with the Best Fit Matrix for different gameplay goals
    /// </summary>
    public enum RoomGeneratorType : byte
    {
        PatternDrivenModular = 0,    // Movement skill puzzles (dash, wall-cling, grapple)
        ParametricChallenge = 1,     // Platforming testing grounds with jump arc solver
        WeightedTilePrefab = 2,      // Standard platforming with optional secrets
        VerticalSegment = 3,         // Vertical layout rooms (towers, shafts)
        HorizontalCorridor = 4,      // Horizontal layout rooms (flow platforming)
        BiomeWeightedTerrain = 5,    // Top-world terrain generation
        SkyBiomePlatform = 6,        // Sky biome with moving platforms
        LinearBranchingCorridor = 7, // Linear branching corridor layout
        StackedSegment = 8           // Stacked segment generation
    }

    /// <summary>
    /// Movement capability tags for skill-aware room generation
    /// Maps to existing Ability flags for consistency
    /// </summary>
    public struct MovementCapabilityTags : IComponentData
    {
        public Ability RequiredSkills;      // Skills needed to complete room
        public Ability OptionalSkills;     // Skills that provide alternate routes/secrets
        public BiomeAffinity BiomeType;    // Primary biome this room fits
        public float DifficultyRating;     // 0.0 (trivial) to 1.0 (expert)
        
        public MovementCapabilityTags(Ability required, Ability optional = Ability.None, 
                                     BiomeAffinity biome = BiomeAffinity.Any, float difficulty = 0.5f)
        {
            RequiredSkills = required;
            OptionalSkills = optional;
            BiomeType = biome;
            DifficultyRating = math.clamp(difficulty, 0.0f, 1.0f);
        }
    }

    /// <summary>
    /// Biome affinity for room templates/modules  
    /// </summary>
    public enum BiomeAffinity : byte
    {
        Any = 0,
        Forest = 1,
        Desert = 2,
        Mountain = 3,
        Ocean = 4,
        Sky = 5,
        Underground = 6,
        TechZone = 7,
        Volcanic = 8
    }

    /// <summary>
    /// Component wrapper for BiomeAffinity
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
    /// Room template data for procedural generation
    /// Contains layout patterns and skill requirements
    /// </summary>
    public struct RoomTemplate : IComponentData
    {
        public RoomGeneratorType GeneratorType;
        public MovementCapabilityTags CapabilityTags;
        public int2 MinSize;                // Minimum room dimensions
        public int2 MaxSize;                // Maximum room dimensions
        public float SecretAreaPercentage;  // % of tiles reserved for secrets (0.0-1.0)
        public bool RequiresJumpValidation; // Whether to run jump arc solver
        public uint TemplateId;             // Unique identifier for this template
        
        public RoomTemplate(RoomGeneratorType type, MovementCapabilityTags tags, 
                           int2 minSize, int2 maxSize, float secretPercent = 0.1f, 
                           bool jumpValidation = true, uint templateId = 0)
        {
            GeneratorType = type;
            CapabilityTags = tags;
            MinSize = minSize;
            MaxSize = maxSize;
            SecretAreaPercentage = math.clamp(secretPercent, 0.0f, 0.5f);
            RequiresJumpValidation = jumpValidation;
            TemplateId = templateId;
        }
    }

    /// <summary>
    /// Component for rooms that have completed procedural generation
    /// Links to navigation and visual generation phases
    /// </summary>
    public struct ProceduralRoomGenerated : IComponentData
    {
        public bool ContentGenerated;       // Room layout/content created
        public bool NavigationGenerated;   // Nav graph created
        public bool CinemachineGenerated;  // Camera zones created
        public uint GenerationSeed;        // Seed used for this room's generation
        public float GenerationTime;       // Time taken for generation (profiling)
        
        public ProceduralRoomGenerated(uint seed)
        {
            ContentGenerated = false;
            NavigationGenerated = false;
            CinemachineGenerated = false;
            GenerationSeed = seed;
            GenerationTime = 0.0f;
        }
    }

    /// <summary>
    /// Buffer element for room navigation connections with movement type tags
    /// Extends existing connection system with movement-aware navigation
    /// </summary>
    public struct RoomNavigationElement : IBufferElementData
    {
        public int2 FromPosition;           // Starting navigation point
        public int2 ToPosition;             // Destination navigation point
        public Ability RequiredMovement;   // Movement type needed for this connection
        public float TraversalCost;        // Cost/difficulty of this connection
        public bool IsSecret;               // Whether this is a secret/alternate route
        
        public RoomNavigationElement(int2 from, int2 to, Ability movement = Ability.Jump, 
                                   float cost = 1.0f, bool secret = false)
        {
            FromPosition = from;
            ToPosition = to;
            RequiredMovement = movement;
            TraversalCost = cost;
            IsSecret = secret;
        }
    }

    /// <summary>
    /// Component for jump arc physics validation
    /// Used by ParametricChallengeRoomGenerator and reachability checks
    /// </summary>
    public struct JumpArcPhysics : IComponentData
    {
        public float JumpHeight;            // Maximum jump height
        public float JumpDistance;         // Maximum horizontal jump distance
        public float DoubleJumpBonus;      // Additional height/distance for double jump
        public float GravityScale;         // Gravity affecting jump arcs
        public float WallJumpHeight;       // Height gained from wall jumps
        public float DashDistance;         // Horizontal dash distance
        public float GlideSpeed;           // Glide/fall speed reduction
        
        public JumpArcPhysics(float height = 3.0f, float distance = 4.0f, float doubleBonus = 1.5f,
                             float gravity = 1.0f, float wallHeight = 2.0f, float dash = 6.0f)
        {
            JumpHeight = height;
            JumpDistance = distance;
            DoubleJumpBonus = doubleBonus;
            GravityScale = gravity;
            WallJumpHeight = wallHeight;
            DashDistance = dash;
            GlideSpeed = 2.0f;
        }
    }
    
    /// <summary>
    /// Component to tag entities with skill requirements
    /// </summary>
    public struct SkillTag : IComponentData
    {
        public Ability RequiredSkills;
        public Ability OptionalSkills;
        
        public SkillTag(Ability required, Ability optional = Ability.None)
        {
            RequiredSkills = required;
            OptionalSkills = optional;
        }
    }
    
    /// <summary>
    /// Buffer element for room pattern generation data
    /// </summary>
    public struct RoomPatternElement : IBufferElementData
    {
        public int2 Position;
        public RoomFeatureType FeatureType;
        public uint Seed;
        public Ability RequiredAbility;
        
        public RoomPatternElement(int2 position, RoomFeatureType featureType, uint seed, Ability requiredAbility = Ability.None)
        {
            Position = position;
            FeatureType = featureType;
            Seed = seed;
            RequiredAbility = requiredAbility;
        }
    }
    
    /// <summary>
    /// Buffer element for room module references
    /// </summary>
    public struct RoomModuleElement : IBufferElementData
    {
        public Entity ModulePrefab;
        public int2 Position;
        public float Weight;
        
        public RoomModuleElement(Entity modulePrefab, int2 position, float weight = 1.0f)
        {
            ModulePrefab = modulePrefab;
            Position = position;
            Weight = weight;
        }
    }
    
    /// <summary>
    /// Component for room generation requests
    /// </summary>
    public struct RoomGenerationRequest : IComponentData
    {
        public RoomGeneratorType GeneratorType;
        public Entity RoomEntity;
        public int2 RoomBounds;
        public uint Seed;
        public uint GenerationSeed;  // Alias for Seed for compatibility
        public Ability AvailableSkills;
        public BiomeAffinity TargetBiome;
        public bool IsComplete;
        
        public RoomGenerationRequest(RoomGeneratorType generatorType, Entity roomEntity, int2 bounds, uint seed)
        {
            GeneratorType = generatorType;
            RoomEntity = roomEntity;
            RoomBounds = bounds;
            Seed = seed;
            GenerationSeed = seed;  // Sync both fields
            AvailableSkills = Ability.None;
            TargetBiome = BiomeAffinity.Any;
            IsComplete = false;
        }
        
        public RoomGenerationRequest(RoomGeneratorType generatorType, Entity roomEntity, int2 bounds, uint seed, Ability availableSkills)
        {
            GeneratorType = generatorType;
            RoomEntity = roomEntity;
            RoomBounds = bounds;
            Seed = seed;
            GenerationSeed = seed;
            AvailableSkills = availableSkills;
            TargetBiome = BiomeAffinity.Any;
            IsComplete = false;
        }
    }
    
    /// <summary>
    /// Feature types for room pattern generation
    /// </summary>
    public enum RoomFeatureType : byte
    {
        Platform = 0,
        Obstacle = 1,
        GrapplePoint = 2,
        SkillGate = 3,
        Secret = 4,
        Hazard = 5,
        PowerUp = 6,
        HealthPickup = 7,
        SaveStation = 8,
        Switch = 9,
        Enemy = 10,
        Door = 11,
        Collectible = 12
    }
    
    /// <summary>
    /// Jump physics data for room generation
    /// </summary>
    public struct JumpPhysicsData : IComponentData
    {
        public float JumpHeight;
        public float JumpDistance;
        public float GravityScale;
        public float MaxFallSpeed;
        public bool HasDoubleJump;
        public bool HasWallJump;
        public bool HasGlide;
        
        public JumpPhysicsData(float height = 3.0f, float distance = 4.0f, float gravity = 1.0f)
        {
            JumpHeight = height;
            JumpDistance = distance;
            GravityScale = gravity;
            MaxFallSpeed = 10.0f;
            HasDoubleJump = false;
            HasWallJump = false;
            HasGlide = false;
        }
        
        public JumpPhysicsData(float height, float distance, float gravity, float maxFallSpeed, bool doubleJump, bool wallJump, bool glide)
        {
            JumpHeight = height;
            JumpDistance = distance;
            GravityScale = gravity;
            MaxFallSpeed = maxFallSpeed;
            HasDoubleJump = doubleJump;
            HasWallJump = wallJump;
            HasGlide = glide;
        }
    }
    
    /// <summary>
    /// Configuration for secret areas in rooms
    /// </summary>
    public struct SecretAreaConfig : IComponentData
    {
        public float SecretProbability;
        public int MaxSecretsPerRoom;
        public Ability RequiredSkillForAccess;
        public int2 MinSecretSize;
        public int2 MaxSecretSize;
        public bool AllowStackedSecrets;
        public bool RequireHiddenAccess;
        public float SecretAreaPercentage;
        public bool UseAlternateRoutes;
        public bool UseDestructibleWalls;
        
        public SecretAreaConfig(float probability = 0.3f, int maxSecrets = 2, Ability requiredSkill = Ability.None)
        {
            SecretProbability = probability;
            MaxSecretsPerRoom = maxSecrets;
            RequiredSkillForAccess = requiredSkill;
            MinSecretSize = new int2(2, 2);
            MaxSecretSize = new int2(4, 4);
            AllowStackedSecrets = false;
            RequireHiddenAccess = true;
            SecretAreaPercentage = 0.15f;
            UseAlternateRoutes = false;
            UseDestructibleWalls = false;
        }
        
        public SecretAreaConfig(float probability, int2 minSize, int2 maxSize, Ability requiredSkill, bool allowStacked, bool requireHidden)
        {
            SecretProbability = probability;
            MaxSecretsPerRoom = 2;
            RequiredSkillForAccess = requiredSkill;
            MinSecretSize = minSize;
            MaxSecretSize = maxSize;
            AllowStackedSecrets = allowStacked;
            RequireHiddenAccess = requireHidden;
            SecretAreaPercentage = 0.15f;
            UseAlternateRoutes = false;
            UseDestructibleWalls = false;
        }
    }
    
    /// <summary>
    /// Jump arc validation component
    /// </summary>
    public struct JumpArcValidation : IComponentData
    {
        public bool IsValid;
        public float MaxJumpDistance;
        public float MinJumpHeight;
        
        public JumpArcValidation(bool isValid = true, float maxDistance = 8.0f, float minHeight = 2.0f)
        {
            IsValid = isValid;
            MaxJumpDistance = maxDistance;
            MinJumpHeight = minHeight;
        }
    }
    
    /// <summary>
    /// Buffer element for jump connections
    /// </summary>
    public struct JumpConnectionElement : IBufferElementData
    {
        public int2 StartPosition;
        public int2 EndPosition;
        public Ability RequiredAbility;
        public float Angle;
        public float Velocity;
        
        public JumpConnectionElement(int2 start, int2 end, Ability requiredAbility = Ability.None)
        {
            StartPosition = start;
            EndPosition = end;
            RequiredAbility = requiredAbility;
            Angle = 0f;
            Velocity = 0f;
        }
        
        public JumpConnectionElement(int2 start, int2 end, float angle, float velocity)
        {
            StartPosition = start;
            EndPosition = end;
            RequiredAbility = Ability.Jump;
            Angle = angle;
            Velocity = velocity;
        }
    }
}