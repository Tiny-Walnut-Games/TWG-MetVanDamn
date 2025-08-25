using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Types of room generators available for different gameplay goals
    /// </summary>
    public enum RoomGeneratorType : byte
    {
        /// <summary>Movement skill puzzles (dash, wall-cling, grapple)</summary>
        PatternDrivenModular = 0,
        /// <summary>Platforming puzzle testing grounds (jump/double-jump)</summary>
        ParametricChallenge = 1,
        /// <summary>Standard platforming + optional secrets</summary>
        WeightedTilePrefab = 2,
        /// <summary>Vertical layout rooms</summary>
        StackedSegment = 3,
        /// <summary>Horizontal layout rooms</summary>
        LinearBranchingCorridor = 4,
        /// <summary>Top-world terrain generation (overworld biomes)</summary>
        BiomeWeightedHeightmap = 5,
        /// <summary>Sky biome generation</summary>
        LayeredPlatformCloud = 6
    }

    /// <summary>
    /// Layout orientation for room generation
    /// </summary>
    public enum RoomLayoutType : byte
    {
        Horizontal = 0,
        Vertical = 1,
        Mixed = 2
    }

    /// <summary>
    /// Skill tag component for prefabs and tiles
    /// Enables skill-aware generation filtering
    /// </summary>
    public struct SkillTag : IComponentData
    {
        /// <summary>Required abilities to use this prefab/tile</summary>
        public Ability RequiredSkill;
        
        /// <summary>Optional abilities that enhance this prefab/tile</summary>
        public Ability OptionalSkill;
        
        /// <summary>Difficulty level of using this skill element</summary>
        public float SkillDifficulty;

        public SkillTag(Ability requiredSkill, Ability optionalSkill = Ability.None, float skillDifficulty = 0.5f)
        {
            RequiredSkill = requiredSkill;
            OptionalSkill = optionalSkill;
            SkillDifficulty = math.clamp(skillDifficulty, 0.0f, 1.0f);
        }
    }

    /// <summary>
    /// Biome affinity component for prefabs and tiles
    /// Controls biome-specific content selection
    /// </summary>
    public struct BiomeAffinity : IComponentData
    {
        /// <summary>Primary biome this element belongs to</summary>
        public BiomeType PrimaryBiome;
        
        /// <summary>Secondary compatible biomes</summary>
        public BiomeType SecondaryBiome;
        
        /// <summary>Polarity affinity for biome coherence</summary>
        public Polarity PolarityAffinity;
        
        /// <summary>Weight/probability of selection (0.0 to 1.0)</summary>
        public float SelectionWeight;

        public BiomeAffinity(BiomeType primaryBiome, Polarity polarityAffinity = Polarity.None, 
                           float selectionWeight = 1.0f, BiomeType secondaryBiome = BiomeType.Unknown)
        {
            PrimaryBiome = primaryBiome;
            SecondaryBiome = secondaryBiome;
            PolarityAffinity = polarityAffinity;
            SelectionWeight = math.clamp(selectionWeight, 0.0f, 1.0f);
        }

        /// <summary>
        /// Check if this element is compatible with a biome
        /// </summary>
        public readonly bool IsCompatibleWith(BiomeType biomeType, Polarity biomePolarity)
        {
            bool biomeMatch = PrimaryBiome == biomeType || SecondaryBiome == biomeType;
            bool polarityMatch = PolarityAffinity == Polarity.None || 
                               PolarityAffinity == Polarity.Any ||
                               (PolarityAffinity & biomePolarity) != 0;
            
            return biomeMatch && polarityMatch;
        }
    }

    /// <summary>
    /// Room generation request component
    /// Coordinates the 6-step pipeline flow
    /// </summary>
    public struct RoomGenerationRequest : IComponentData
    {
        /// <summary>Current pipeline step (1-6)</summary>
        public byte CurrentStep;
        
        /// <summary>Selected generator type for this room</summary>
        public RoomGeneratorType GeneratorType;
        
        /// <summary>Selected layout orientation</summary>
        public RoomLayoutType LayoutType;
        
        /// <summary>Available player skills for filtering</summary>
        public Ability AvailableSkills;
        
        /// <summary>Target biome for this room</summary>
        public BiomeType TargetBiome;
        
        /// <summary>Target polarity for this room</summary>
        public Polarity TargetPolarity;
        
        /// <summary>Seed for deterministic generation</summary>
        public uint GenerationSeed;
        
        /// <summary>Whether room generation is complete</summary>
        public bool IsComplete;

        public RoomGenerationRequest(RoomGeneratorType generatorType, BiomeType targetBiome, 
                                   Polarity targetPolarity, Ability availableSkills, uint seed)
        {
            CurrentStep = 1;
            GeneratorType = generatorType;
            LayoutType = RoomLayoutType.Mixed;
            AvailableSkills = availableSkills;
            TargetBiome = targetBiome;
            TargetPolarity = targetPolarity;
            GenerationSeed = seed;
            IsComplete = false;
        }
    }

    /// <summary>
    /// Secret area configuration for rooms
    /// Implements Secret Area Hooks requirement
    /// </summary>
    public struct SecretAreaConfig : IComponentData
    {
        /// <summary>Percentage of tiles reserved for secrets (0.0 to 1.0)</summary>
        public float SecretAreaPercentage;
        
        /// <summary>Minimum secret area size</summary>
        public int2 MinSecretSize;
        
        /// <summary>Maximum secret area size</summary>
        public int2 MaxSecretSize;
        
        /// <summary>Required skill to access secrets</summary>
        public Ability SecretSkillRequirement;
        
        /// <summary>Whether to place destructible walls</summary>
        public bool UseDestructibleWalls;
        
        /// <summary>Whether to place alternate routes</summary>
        public bool UseAlternateRoutes;

        public SecretAreaConfig(float secretPercentage = 0.15f, int2 minSize = default, int2 maxSize = default,
                              Ability secretSkill = Ability.None, bool destructibleWalls = true, bool alternateRoutes = true)
        {
            SecretAreaPercentage = math.clamp(secretPercentage, 0.0f, 0.5f);
            MinSecretSize = minSize.Equals(default) ? new int2(2, 2) : minSize;
            MaxSecretSize = maxSize.Equals(default) ? new int2(4, 4) : maxSize;
            SecretSkillRequirement = secretSkill;
            UseDestructibleWalls = destructibleWalls;
            UseAlternateRoutes = alternateRoutes;
        }
    }

    /// <summary>
    /// Jump physics parameters for Jump Arc Solver
    /// </summary>
    public struct JumpPhysicsData : IComponentData
    {
        /// <summary>Maximum jump height in world units</summary>
        public float MaxJumpHeight;
        
        /// <summary>Maximum horizontal jump distance</summary>
        public float MaxJumpDistance;
        
        /// <summary>Gravity strength</summary>
        public float Gravity;
        
        /// <summary>Player movement speed</summary>
        public float MovementSpeed;
        
        /// <summary>Double jump availability</summary>
        public bool HasDoubleJump;
        
        /// <summary>Wall jump availability</summary>
        public bool HasWallJump;
        
        /// <summary>Dash availability</summary>
        public bool HasDash;

        public JumpPhysicsData(float maxHeight = 4.0f, float maxDistance = 6.0f, float gravity = 9.81f, 
                             float speed = 5.0f, bool doubleJump = false, bool wallJump = false, bool dash = false)
        {
            MaxJumpHeight = maxHeight;
            MaxJumpDistance = maxDistance;
            Gravity = gravity;
            MovementSpeed = speed;
            HasDoubleJump = doubleJump;
            HasWallJump = wallJump;
            HasDash = dash;
        }
    }

    /// <summary>
    /// Buffer element for room generation modules/prefabs
    /// </summary>
    public struct RoomModuleElement : IBufferElementData
    {
        public Entity ModulePrefab;
        public float Weight;
        public SkillTag SkillRequirement;
        public BiomeAffinity BiomeRequirement;
        
        public static implicit operator RoomModuleElement(Entity entity) 
            => new() { ModulePrefab = entity, Weight = 1.0f };
    }

    /// <summary>
    /// Buffer element for pattern-driven room generation
    /// </summary>
    public struct RoomPatternElement : IBufferElementData
    {
        public int2 Position;
        public RoomFeatureType FeatureType;
        public uint PatternId;
        public Ability RequiredSkill;
        
        public RoomPatternElement(int2 position, RoomFeatureType featureType, uint patternId = 0, 
                                Ability requiredSkill = Ability.None)
        {
            Position = position;
            FeatureType = featureType;
            PatternId = patternId;
            RequiredSkill = requiredSkill;
        }
    }
}