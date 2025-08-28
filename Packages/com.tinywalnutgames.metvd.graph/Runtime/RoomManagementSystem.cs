using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using System;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Component for tracking room state and features
    /// </summary>
    public struct RoomStateData : IComponentData
    {
        /// <summary>
        /// Whether this room has been visited by the player
        /// </summary>
        public bool IsVisited;

        /// <summary>
        /// Whether this room has been fully explored
        /// </summary>
        public bool IsExplored;

        /// <summary>
        /// Number of secrets/treasures discovered in this room
        /// </summary>
        public int SecretsFound;

        /// <summary>
        /// Total number of secrets available in this room
        /// </summary>
        public int TotalSecrets;

        /// <summary>
        /// Completion percentage for this room (0.0 to 1.0)
        /// </summary>
        public float CompletionPercentage;

        public RoomStateData(int totalSecrets = 0)
        {
            IsVisited = false;
            IsExplored = false;
            SecretsFound = 0;
            TotalSecrets = totalSecrets;
            CompletionPercentage = 0.0f;
        }
    }

    /// <summary>
    /// Component for room connections and pathfinding
    /// </summary>
    public struct RoomNavigationData : IComponentData
    {
        /// <summary>
        /// Number of entrances to this room
        /// </summary>
        public int EntranceCount;

        /// <summary>
        /// Primary entrance direction (for AI navigation)
        /// </summary>
        public int2 PrimaryEntrance;

        /// <summary>
        /// Whether this room is on the critical path
        /// </summary>
        public bool IsCriticalPath;

        /// <summary>
        /// Estimated traversal time through this room
        /// </summary>
        public float TraversalTime;

        public RoomNavigationData(int2 primaryEntrance, bool isCriticalPath = false, float traversalTime = 5.0f)
        {
            EntranceCount = 1;
            PrimaryEntrance = primaryEntrance;
            IsCriticalPath = isCriticalPath;
            TraversalTime = traversalTime;
        }
    }

    /// <summary>
    /// Buffer element for room features (enemies, items, obstacles)
    /// </summary>
    public struct RoomFeatureElement : IBufferElementData
    {
        public RoomFeatureType Type;
        public int2 Position;
        public uint FeatureId;

        public RoomFeatureElement(RoomFeatureType type, int2 position, uint featureId = 0)
        {
            Type = type;
            Position = position;
            FeatureId = featureId;
        }
    }

    /// <summary>
    /// Optimized progression data using actual enum values and distance calculations
    /// </summary>
    public struct ProgressionAnalysis
    {
        public float DistanceFromOrigin;
        public int ProgressionTier;
        public uint SpatialHash;

        public ProgressionAnalysis(NodeId nodeId)
        {
            DistanceFromOrigin = math.length((float2)nodeId.Coordinates);
            ProgressionTier = CalculateProgressionTier(DistanceFromOrigin);
            SpatialHash = CalculateSpatialHash(nodeId);
        }

        private static int CalculateProgressionTier(float distance)
        {
            // Progressive difficulty tiers based on distance
            return math.min((int)(distance / 3.0f), 7); // 8 tiers (0-7)
        }

        private static uint CalculateSpatialHash(NodeId nodeId)
        {
            return nodeId._value ^ ((uint)nodeId.Coordinates.x << 16) ^ ((uint)nodeId.Coordinates.y << 8) ^ nodeId.ParentId;
        }
    }

    /// <summary>
    /// System that manages room state, navigation, and features
    /// Runs after sector hierarchy creation to populate rooms with content
    /// Now integrates with the new procedural room generation pipeline
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SectorRoomHierarchySystem))]
    public partial struct RoomManagementSystem : ISystem
    {
        private EntityQuery _roomsQuery;
        private EntityQuery _sectorsQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Use EntityQueryBuilder to avoid managed params array allocation
            _roomsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RoomHierarchyData, NodeId>()
                .Build(ref state);
            _sectorsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SectorHierarchyData, NodeId>()
                .Build(ref state);
            state.RequireForUpdate(_roomsQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Get all rooms that need management data
            using var roomEntities = _roomsQuery.ToEntityArray(Allocator.Temp);
            using var roomHierarchyData = _roomsQuery.ToComponentDataArray<RoomHierarchyData>(Allocator.Temp);
            using var roomNodeIds = _roomsQuery.ToComponentDataArray<NodeId>(Allocator.Temp);

            for (int i = 0; i < roomEntities.Length; i++)
            {
                var entity = roomEntities[i];
                var roomData = roomHierarchyData[i];
                var nodeId = roomNodeIds[i];

                // Skip rooms that already have management data
                if (state.EntityManager.HasComponent<RoomStateData>(entity))
                    continue;

                // Analyze progression using actual nodeId data
                var progression = new ProgressionAnalysis(nodeId);

                // Add room management components using optimized data
                var random = new Unity.Mathematics.Random((uint)(nodeId._value + 12345));

                // Add room state data with progression-based secret count
                var totalSecrets = DetermineSecretCount(roomData.Type, progression, ref random);
                state.EntityManager.AddComponentData(entity, new RoomStateData(totalSecrets));

                // Add navigation data
                var isCriticalPath = roomData.Type == RoomType.Entrance || roomData.Type == RoomType.Exit || roomData.Type == RoomType.Boss;
                var traversalTime = CalculateTraversalTime(in roomData.Bounds, roomData.Type);
                CalculatePrimaryEntrance(in roomData.Bounds, out int2 primaryEntrance);
                state.EntityManager.AddComponentData(entity, new RoomNavigationData(primaryEntrance, isCriticalPath, traversalTime));

                // Add room features buffer
                var featuresBuffer = state.EntityManager.AddBuffer<RoomFeatureElement>(entity);
                PopulateRoomFeatures(featuresBuffer, in roomData, progression, ref random);

                // Initialize room generation request for new procedural pipeline
                InitializeRoomGenerationRequest(state.EntityManager, entity, roomData, nodeId, progression, ref random);
            }
        }

        /// <summary>
        /// Initialize room generation request using existing enums and dynamic discovery
        /// Properly utilizes nodeId for spatial-aware biome, polarity, and skill determination
        /// </summary>
        private static void InitializeRoomGenerationRequest(EntityManager entityManager, Entity roomEntity,
                                                           RoomHierarchyData roomData, NodeId nodeId,
                                                           ProgressionAnalysis progression,
                                                           ref Unity.Mathematics.Random random)
        {
            // Determine generator type based on room type and characteristics
            var generatorType = DetermineGeneratorType(roomData.Type, roomData.Bounds);

            // Use nodeId to determine biome based on spatial location and hierarchy
            var targetBiome = DetermineBiomeFromNodeId(nodeId);

            // Use nodeId to determine polarity based on coordinates and parent relationships
            var targetPolarity = DeterminePolarityFromNodeId(nodeId);

            // Use nodeId coordinates and hierarchy to simulate progression-based skills
            var availableSkills = DetermineAvailableSkillsFromNodeId(nodeId);

            // Create generation request with nodeId-enhanced seed
            var generationRequest = new RoomGenerationRequest(
                generatorType,
                targetBiome,
                targetPolarity,
                availableSkills,
                CombineNodeIdWithRandom(nodeId, random.NextUInt())
            )
            {
                RoomEntity = roomEntity,
                RoomBounds = new int2(roomData.Bounds.width, roomData.Bounds.height)
            };

            entityManager.AddComponentData(roomEntity, generationRequest);

            // Add specialized components based on generator type
            AddSpecializedComponents(entityManager, roomEntity, generatorType, roomData, progression);
        }

        /// <summary>
        /// Determine biome based on nodeId spatial coordinates and parent relationships
        /// Uses existing BiomeType enum with spatial coherence logic
        /// </summary>
        private static BiomeType DetermineBiomeFromNodeId(NodeId nodeId)
        {
            // Get all BiomeType enum values dynamically
            var biomeValues = Enum.GetValues(typeof(BiomeType));
            var validBiomes = new BiomeType[biomeValues.Length - 1]; // Exclude Unknown
            int validIndex = 0;

            foreach (BiomeType biome in biomeValues)
            {
                if (biome != BiomeType.Unknown)
                {
                    validBiomes[validIndex++] = biome;
                }
            }

            // Use nodeId coordinates to create deterministic but varied biome regions
            var coords = nodeId.Coordinates;
            var distanceFromOrigin = math.sqrt(coords.x * coords.x + coords.y * coords.y);
            
            // Create a hash from nodeId properties for deterministic selection
            var biomeHash = (uint)(nodeId._value ^ (coords.x << 16) ^ (coords.y << 8) ^ nodeId.ParentId);
            
            // Apply spatial influence to create biome regions
            // Closer to origin = more hub-like biomes, further = more exotic/dangerous
            var distanceInfluence = math.clamp(distanceFromOrigin / 20.0f, 0.0f, 1.0f);
            
            // Use quadrant-based regional bias for spatial coherence
            int quadrantBias = (coords.x >= 0, coords.y >= 0) switch
            {
                (true, true) => 1,    // First quadrant (+ +)
                (false, true) => 2,   // Second quadrant (- +)
                (false, false) => 3,  // Third quadrant (- -)
                (true, false) => 4    // Fourth quadrant (+ -)
            };

            // Combine hash with spatial bias for regional coherence
            var finalHash = biomeHash + (quadrantBias << 24) + (uint)(distanceInfluence * 1000);
            
            // Select biome index from valid biomes
            var biomeIndex = (int)(finalHash % (uint)validBiomes.Length);

            // discard biome selections that are too dangerous for close distances
            if (distanceFromOrigin < 5.0f)
            {
                // Filter out high-danger biomes for close distances
                var safeBiomes = new System.Collections.Generic.List<BiomeType>();
                foreach (var biome in validBiomes)
                {
                    if (biome != BiomeType.VoidChambers && biome != BiomeType.Hell &&
                        biome != BiomeType.PlasmaFields && biome != BiomeType.Cosmic)
                    {
                        safeBiomes.Add(biome);
                    }
                }
                if (safeBiomes.Count > 0)
                {
                    biomeIndex = (int)(finalHash % (uint)safeBiomes.Count);
                    return safeBiomes[biomeIndex];
                }
            }

            return validBiomes[biomeIndex];
        }

        /// <summary>
        /// Determine polarity based on nodeId characteristics
        /// Uses spatial coordinates and parent relationships for coherent polarity distribution
        /// </summary>
        private static Polarity DeterminePolarityFromNodeId(NodeId nodeId)
        {
            // Get all Polarity enum values dynamically
            var polarityValues = Enum.GetValues(typeof(Polarity));
            var validPolarities = new System.Collections.Generic.List<Polarity>();

            foreach (Polarity polarity in polarityValues)
            {
                // Exclude None and composite values for base selection
                if (polarity != Polarity.None && !IsCompositePolarity(polarity))
                {
                    validPolarities.Add(polarity);
                }
            }

            // Use nodeId hash to determine polarity in a deterministic way
            var hash = (uint)(nodeId._value ^ (nodeId.Coordinates.x << 16) ^ (nodeId.Coordinates.y << 8));

            // Select base polarity using spatial hash
            var polarityIndex = (int)(hash % (uint)validPolarities.Count);
            var selectedPolarity = validPolarities[polarityIndex];

            // For areas far from origin, add dual polarity combinations
            var distanceFromOrigin = math.length((float2)nodeId.Coordinates);
            if (distanceFromOrigin > 10.0f)
            {
                var secondaryIndex = (int)((nodeId.ParentId >> 8) % (uint)validPolarities.Count);
                if (secondaryIndex != polarityIndex)
                {
                    selectedPolarity |= validPolarities[secondaryIndex];
                }
            }

            return selectedPolarity;
        }

        /// <summary>
        /// Determine available skills based on nodeId position (simulating progression)
        /// Uses distance from origin and parent hierarchy for skill complexity
        /// </summary>
        private static Ability DetermineAvailableSkillsFromNodeId(NodeId nodeId)
        {
            var distanceFromOrigin = math.sqrt(nodeId.Coordinates.x * nodeId.Coordinates.x + nodeId.Coordinates.y * nodeId.Coordinates.y);
            
            // Basic abilities always available
            var skills = Ability.Jump;
            
            // Progressive ability unlocking based on distance and nodeId hierarchy
            if (distanceFromOrigin > 2) skills |= Ability.DoubleJump;
            if (distanceFromOrigin > 5) skills |= Ability.WallJump;
            if (distanceFromOrigin > 8) skills |= Ability.Dash;
            if (distanceFromOrigin > 12) skills |= Ability.Grapple;
            
            // Add environmental abilities based on nodeId coordinates
            var coords = nodeId.Coordinates;
            if (coords.y < -5) skills |= Ability.Swim; // Deep areas need swimming
            if (math.abs(coords.x) > 10) skills |= Ability.Climb; // Far areas need climbing
            if (coords.x > 15) skills |= Ability.HeatResistance; // Eastern areas are hot
            if (coords.x < -15) skills |= Ability.ColdResistance; // Western areas are cold
            
            // Add tool abilities based on parent ID patterns
            var parentPattern = nodeId.ParentId % 8;
            if (parentPattern == 0 && distanceFromOrigin > 5) skills |= Ability.Scan;
            if (parentPattern == 1 && distanceFromOrigin > 8) skills |= Ability.Bomb;
            if (parentPattern == 2 && distanceFromOrigin > 10) skills |= Ability.Drill;
            if (parentPattern == 3 && distanceFromOrigin > 12) skills |= Ability.Hack;
            
            // Add polarity access based on distance and nodeId hash
            if (distanceFromOrigin > 15)
            {
                var polarityHash = nodeId._value & 0xFF;
                if ((polarityHash & 0x01) != 0) skills |= Ability.SunAccess;
                if ((polarityHash & 0x02) != 0) skills |= Ability.MoonAccess;
                if ((polarityHash & 0x04) != 0) skills |= Ability.HeatAccess;
                if ((polarityHash & 0x08) != 0) skills |= Ability.ColdAccess;
            }
            
            return skills;
        }

        /// <summary>
        /// Combine nodeId with random seed for deterministic but varied generation
        /// </summary>
        private static uint CombineNodeIdWithRandom(NodeId nodeId, uint randomSeed)
        {
            // Create a hash that combines nodeId properties with the random seed
            // This ensures rooms at the same nodeId generate consistently, but with variation
            return (uint)(nodeId._value ^ (nodeId.Coordinates.x << 16) ^ (nodeId.Coordinates.y << 8) ^ nodeId.ParentId ^ randomSeed);
        }

        ///// <summary>
        ///// Initialize room generation request using existing enums and dynamic discovery
        ///// </summary>
        //private static void InitializeRoomGenerationRequest(EntityManager entityManager, Entity roomEntity,
        //                                                   RoomHierarchyData roomData, NodeId nodeId,
        //                                                   ProgressionAnalysis progression,
        //                                                   ref Unity.Mathematics.Random random)
        //{
        //    // Determine generator type based on room type and characteristics
        //    var generatorType = DetermineGeneratorType(roomData.Type, roomData.Bounds);

        //    // Use dynamic biome selection from existing BiomeType enum
        //    var targetBiome = DetermineBiomeFromProgression(progression);

        //    // Use dynamic polarity selection from existing Polarity enum
        //    var targetPolarity = DeterminePolarityFromProgression(progression);

        //    // Use dynamic skill determination from existing Ability enum
        //    var availableSkills = DetermineSkillsFromProgression(progression, targetBiome);

        //    var generationRequest = new RoomGenerationRequest(
        //        generatorType,
        //        targetBiome,
        //        targetPolarity,
        //        availableSkills,
        //        progression.SpatialHash ^ random.NextUInt()
        //    )
        //    {
        //        RoomEntity = roomEntity,
        //        RoomBounds = new int2(roomData.Bounds.width, roomData.Bounds.height)
        //    };

        //    entityManager.AddComponentData(roomEntity, generationRequest);

        //    // Add specialized components based on generator type
        //    AddSpecializedComponents(entityManager, roomEntity, generatorType, roomData, progression);
        //}

        /// <summary>
        /// Dynamically determine biome using existing BiomeType enum values
        /// Uses reflection to discover all available biome types at runtime
        /// </summary>
        private static BiomeType DetermineBiomeFromProgression(ProgressionAnalysis progression)
        {
            // Get all BiomeType enum values dynamically
            var biomeValues = Enum.GetValues(typeof(BiomeType));
            var validBiomes = new BiomeType[biomeValues.Length - 1]; // Exclude Unknown
            int validIndex = 0;

            foreach (BiomeType biome in biomeValues)
            {
                if (biome != BiomeType.Unknown)
                {
                    validBiomes[validIndex++] = biome;
                }
            }

            // Apply distance-based biome filtering for appropriate progression
            var filteredBiomes = FilterBiomesByProgression(validBiomes, progression.ProgressionTier);

            // Select biome using spatial hash
            var biomeIndex = (int)(progression.SpatialHash % (uint)filteredBiomes.Length);
            return filteredBiomes[biomeIndex];
        }

        /// <summary>
        /// Filter biomes based on progression tier for appropriate difficulty
        /// </summary>
        private static BiomeType[] FilterBiomesByProgression(BiomeType[] allBiomes, int progressionTier)
        {
            // Early game: filter out dangerous biomes
            if (progressionTier < 2)
            {
                var safeBiomes = new System.Collections.Generic.List<BiomeType>();
                foreach (var biome in allBiomes)
                {
                    // Exclude high-danger biomes for early progression
                    if (biome != BiomeType.VoidChambers && biome != BiomeType.Hell &&
                        biome != BiomeType.PlasmaFields && biome != BiomeType.Cosmic)
                    {
                        safeBiomes.Add(biome);
                    }
                }
                return safeBiomes.ToArray();
            }

            // Late game: all biomes are available
            return allBiomes;
        }

        /// <summary>
        /// Dynamically determine polarity using existing Polarity enum values
        /// </summary>
        private static Polarity DeterminePolarityFromProgression(ProgressionAnalysis progression)
        {
            // Get all Polarity enum values dynamically
            var polarityValues = Enum.GetValues(typeof(Polarity));
            var validPolarities = new System.Collections.Generic.List<Polarity>();

            foreach (Polarity polarity in polarityValues)
            {
                // Exclude None and composite values for base selection
                if (polarity != Polarity.None && !IsCompositePolarity(polarity))
                {
                    validPolarities.Add(polarity);
                }
            }

            // Select base polarity
            var polarityIndex = (int)((progression.SpatialHash >> 8) % (uint)validPolarities.Count);
            var selectedPolarity = validPolarities[polarityIndex];

            // For advanced areas, add dual polarity combinations
            if (progression.ProgressionTier > 4)
            {
                var secondaryIndex = (int)((progression.SpatialHash >> 16) % (uint)validPolarities.Count);
                if (secondaryIndex != polarityIndex)
                {
                    selectedPolarity |= validPolarities[secondaryIndex];
                }
            }

            return selectedPolarity;
        }

        /// <summary>
        /// Check if polarity is a composite (combination) value
        /// </summary>
        private static bool IsCompositePolarity(Polarity polarity)
        {
            // Check if polarity has multiple bits set (composite)
            return (polarity & (polarity - 1)) != 0;
        }

        /// <summary>
        /// Dynamically determine skills using existing Ability enum based on progression
        /// </summary>
        private static Ability DetermineSkillsFromProgression(ProgressionAnalysis progression, BiomeType biome)
        {
            var skills = Ability.Jump; // Always start with basic jump

            // Progressive ability unlocking based on distance and tier
            // Use the actual Ability enum values instead of hardcoded progression

            // Movement progression
            if (progression.ProgressionTier >= 1) skills |= Ability.DoubleJump;
            if (progression.ProgressionTier >= 2) skills |= Ability.WallJump;
            if (progression.ProgressionTier >= 3) skills |= Ability.Dash;
            if (progression.ProgressionTier >= 4) skills |= Ability.ArcJump;
            if (progression.ProgressionTier >= 5) skills |= Ability.Grapple;
            if (progression.ProgressionTier >= 6) skills |= Ability.ChargedJump;
            if (progression.ProgressionTier >= 7) skills |= Ability.TeleportArc;

            // Add biome-specific environmental abilities
            skills |= GetBiomeSpecificAbilities(biome);

            // Add tools based on progression
            if (progression.ProgressionTier >= 3) skills |= Ability.Scan;
            if (progression.ProgressionTier >= 4) skills |= Ability.Bomb;
            if (progression.ProgressionTier >= 5) skills |= Ability.Drill;
            if (progression.ProgressionTier >= 6) skills |= Ability.Hack;

            // Add polarity access abilities for advanced areas
            if (progression.ProgressionTier >= 4)
            {
                // Use spatial hash to determine which polarity abilities to grant
                var polarityMask = progression.SpatialHash & 0xFF;
                if ((polarityMask & 0x01) != 0) skills |= Ability.SunAccess;
                if ((polarityMask & 0x02) != 0) skills |= Ability.MoonAccess;
                if ((polarityMask & 0x04) != 0) skills |= Ability.HeatAccess;
                if ((polarityMask & 0x08) != 0) skills |= Ability.ColdAccess;
            }

            return skills;
        }

        /// <summary>
        /// Get biome-specific environmental abilities using actual BiomeType enum
        /// </summary>
        private static Ability GetBiomeSpecificAbilities(BiomeType biome)
        {
            return biome switch
            {
                // Water biomes require swimming
                BiomeType.Ocean or BiomeType.DeepUnderwater => Ability.Swim,

                // Cold biomes require cold resistance
                BiomeType.FrozenWastes or BiomeType.IceCatacombs or
                BiomeType.CryogenicLabs or BiomeType.IcyCanyon or BiomeType.Tundra => Ability.ColdResistance,

                // Hot biomes require heat resistance
                BiomeType.VolcanicCore or BiomeType.Hell or BiomeType.Volcanic => Ability.HeatResistance,

                // Mountain/cave biomes benefit from climbing
                BiomeType.Mountains or BiomeType.CrystalCaverns => Ability.Climb,

                // Tech biomes require hacking
                BiomeType.PowerPlant or BiomeType.CryogenicLabs => Ability.Hack,

                // High-pressure environments
                BiomeType.VoidChambers or BiomeType.Cosmic => Ability.PressureResistance,

                _ => Ability.None
            };
        }

        /// <summary>
        /// Enhanced secret count determination with progression influence
        /// </summary>
        private static int DetermineSecretCount(RoomType roomType, ProgressionAnalysis progression, ref Unity.Mathematics.Random random)
        {
            var baseCount = roomType switch
            {
                RoomType.Treasure => random.NextInt(2, 5),
                RoomType.Boss => random.NextInt(1, 3),
                RoomType.Hub => random.NextInt(0, 2),
                RoomType.Normal => random.NextFloat() > 0.7f ? random.NextInt(1, 2) : 0,
                _ => 0
            };

            // Add progression-based bonus secrets (more secrets in advanced areas)
            var progressionBonus = progression.ProgressionTier / 3;
            return baseCount + progressionBonus;
        }

        /// <summary>
        /// Enhanced room feature population with progression-aware content
        /// </summary>
        private static void PopulateRoomFeatures(DynamicBuffer<RoomFeatureElement> features,
                                                in RoomHierarchyData roomData,
                                                ProgressionAnalysis progression,
                                                ref Unity.Mathematics.Random random)
        {
            var bounds = roomData.Bounds;
            var area = bounds.width * bounds.height;

            switch (roomData.Type)
            {
                case RoomType.Boss:
                    AddProgressiveBossRoomFeatures(features, in bounds, progression, ref random);
                    break;
                case RoomType.Treasure:
                    AddProgressiveTreasureRoomFeatures(features, in bounds, progression, ref random);
                    break;
                case RoomType.Save:
                    AddSaveRoomFeatures(features, in bounds, ref random);
                    break;
                case RoomType.Shop:
                    AddShopRoomFeatures(features, in bounds, ref random);
                    break;
                default:
                    AddProgressiveNormalRoomFeatures(features, in bounds, area, progression, ref random);
                    break;
            }
        }

        /// <summary>
        /// Add specialized components efficiently based on generator type and progression
        /// </summary>
        private static void AddSpecializedComponents(EntityManager entityManager, Entity roomEntity,
                                                    RoomGeneratorType generatorType, RoomHierarchyData roomData,
                                                    ProgressionAnalysis progression)
        {
            if (generatorType == RoomGeneratorType.ParametricChallenge)
            {
                var jumpPhysics = new JumpPhysicsData(4.0f, 6.0f, 9.81f, 5.0f, true, false, false);
                entityManager.AddComponentData(roomEntity, jumpPhysics);
                entityManager.AddComponentData(roomEntity, new JumpArcValidation(false, 0, 0));
                entityManager.AddBuffer<JumpConnectionElement>(roomEntity);
            }

            if (generatorType == RoomGeneratorType.PatternDrivenModular)
            {
                entityManager.AddBuffer<RoomPatternElement>(roomEntity);
                entityManager.AddBuffer<RoomModuleElement>(roomEntity);
            }

            if (generatorType == RoomGeneratorType.WeightedTilePrefab ||
                roomData.Type == RoomType.Treasure || roomData.Type == RoomType.Normal)
            {
                // Dynamic secret intensity based on progression
                var secretIntensity = math.clamp(0.1f + (progression.ProgressionTier * 0.05f), 0.05f, 0.5f);
                var secretConfig = new SecretAreaConfig(secretIntensity, new int2(2, 2), new int2(4, 4),
                                                      Ability.None, true, true);
                entityManager.AddComponentData(roomEntity, secretConfig);
            }
        }

        // ========================================
        // PROGRESSIVE ROOM FEATURE METHODS
        // ========================================

        private static void AddProgressiveBossRoomFeatures(DynamicBuffer<RoomFeatureElement> features,
                                                          in RectInt bounds, ProgressionAnalysis progression,
                                                          ref Unity.Mathematics.Random random)
        {
            // Boss spawn point
            var bossPos = new int2(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
            features.Add(new RoomFeatureElement(RoomFeatureType.Enemy, bossPos, random.NextUInt()));

            // Progressive platform complexity
            var platformCount = 2 + progression.ProgressionTier;
            for (int i = 0; i < platformCount; i++)
            {
                var platformPos = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                features.Add(new RoomFeatureElement(RoomFeatureType.Platform, platformPos, random.NextUInt()));
            }
        }

        private static void AddProgressiveTreasureRoomFeatures(DynamicBuffer<RoomFeatureElement> features,
                                                             in RectInt bounds, ProgressionAnalysis progression,
                                                             ref Unity.Mathematics.Random random)
        {
            // More treasures in advanced areas
            var treasureCount = 1 + (progression.ProgressionTier / 2);
            for (int i = 0; i < treasureCount; i++)
            {
                var treasurePos = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                var featureType = random.NextFloat() > 0.5f ? RoomFeatureType.PowerUp : RoomFeatureType.Collectible;
                features.Add(new RoomFeatureElement(featureType, treasurePos, random.NextUInt()));
            }
        }

        private static void AddProgressiveNormalRoomFeatures(DynamicBuffer<RoomFeatureElement> features,
                                                           in RectInt bounds, int area,
                                                           ProgressionAnalysis progression,
                                                           ref Unity.Mathematics.Random random)
        {
            // Progressive enemy and feature scaling
            var baseEnemyCount = area / 8;
            var progressiveEnemyCount = baseEnemyCount + (progression.ProgressionTier / 3);
            var enemyCount = math.min(progressiveEnemyCount, random.NextInt(0, 5));

            for (int i = 0; i < enemyCount; i++)
            {
                var enemyPos = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                features.Add(new RoomFeatureElement(RoomFeatureType.Enemy, enemyPos, random.NextUInt()));
            }

            // Progressive feature complexity
            var featureCount = (area / 12) + progression.ProgressionTier;
            for (int i = 0; i < featureCount; i++)
            {
                var featurePos = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                var featureType = random.NextFloat() > 0.6f ? RoomFeatureType.Platform : RoomFeatureType.Obstacle;
                features.Add(new RoomFeatureElement(featureType, featurePos, random.NextUInt()));
            }

            // Progressive health pickup scaling (harder areas = less health)
            var healthChance = 0.8f - (progression.ProgressionTier * 0.05f);
            if (random.NextFloat() > healthChance)
            {
                var healthPos = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                features.Add(new RoomFeatureElement(RoomFeatureType.HealthPickup, healthPos, random.NextUInt()));
            }
        }

        // ========================================
        // REMAINING ORIGINAL METHODS (unchanged for compatibility)
        // ========================================

        private static RoomGeneratorType DetermineGeneratorType(RoomType roomType, RectInt bounds)
        {
            var aspectRatio = (float)bounds.width / bounds.height;

            return roomType switch
            {
                RoomType.Boss => RoomGeneratorType.PatternDrivenModular,
                RoomType.Treasure => RoomGeneratorType.ParametricChallenge,
                RoomType.Save or RoomType.Shop or RoomType.Hub => RoomGeneratorType.WeightedTilePrefab,
                _ => aspectRatio > 1.5f ? RoomGeneratorType.LinearBranchingCorridor :
                     aspectRatio < 0.67f ? RoomGeneratorType.StackedSegment :
                     RoomGeneratorType.WeightedTilePrefab
            };
        }

        private static float CalculateTraversalTime(in RectInt bounds, RoomType roomType)
        {
            var area = bounds.width * bounds.height;
            var baseTime = math.sqrt(area) * 0.5f;

            return roomType switch
            {
                RoomType.Boss => baseTime * 3.0f,
                RoomType.Treasure => baseTime * 1.5f,
                RoomType.Hub => baseTime * 0.8f,
                _ => baseTime
            };
        }

        private static void CalculatePrimaryEntrance(in RectInt bounds, out int2 result)
        {
            result = new int2(bounds.x + bounds.width / 2, bounds.y);
        }

        private static void AddSaveRoomFeatures(DynamicBuffer<RoomFeatureElement> features, in RectInt bounds, ref Unity.Mathematics.Random random)
        {
            var savePos = new int2(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
            features.Add(new RoomFeatureElement(RoomFeatureType.SaveStation, savePos, random.NextUInt()));
        }

        private static void AddShopRoomFeatures(DynamicBuffer<RoomFeatureElement> features, in RectInt bounds, ref Unity.Mathematics.Random random)
        {
            var itemCount = random.NextInt(2, 5);
            for (int i = 0; i < itemCount; i++)
            {
                var itemPos = new int2(
                    bounds.x + 1 + (i * (bounds.width - 2) / itemCount),
                    bounds.y + bounds.height / 2
                );
                features.Add(new RoomFeatureElement(RoomFeatureType.Collectible, itemPos, random.NextUInt()));
            }
        }
    }
}
