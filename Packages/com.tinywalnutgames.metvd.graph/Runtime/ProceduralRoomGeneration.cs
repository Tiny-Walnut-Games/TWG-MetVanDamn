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
        SkyBiomePlatform = 6         // Sky biome with moving platforms
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
        public float GlideSpeed;           // Gliding speed for air movement
        
        public JumpArcPhysics(float height = 3.0f, float distance = 4.0f, float doubleBonus = 1.5f,
                             float gravity = 1.0f, float wallHeight = 2.0f, float dash = 6.0f, float glide = 2.0f)
        {
            JumpHeight = height;
            JumpDistance = distance;
            DoubleJumpBonus = doubleBonus;
            GravityScale = gravity;
            WallJumpHeight = wallHeight;
            DashDistance = dash;
            GlideSpeed = glide;
        }
    }
}