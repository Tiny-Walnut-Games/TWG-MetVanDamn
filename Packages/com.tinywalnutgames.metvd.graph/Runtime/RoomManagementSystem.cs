using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

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
    /// Types of features that can exist in rooms
    /// </summary>
    public enum RoomFeatureType : byte
    {
        Enemy = 0,
        PowerUp = 1,
        HealthPickup = 2,
        SaveStation = 3,
        Obstacle = 4,
        Platform = 5,
        Switch = 6,
        Door = 7,
        Secret = 8,
        Collectible = 9
    }

    /// <summary>
    /// System that manages room state, navigation, and features
    /// Runs after sector hierarchy creation to populate rooms with content
    /// Now integrates with the new procedural room generation pipeline
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SectorRoomHierarchySystem))]
    public partial class RoomManagementSystem : SystemBase
    {
        private EntityQuery _roomsQuery;
        private EntityQuery _sectorsQuery;

        protected override void OnCreate()
        {
            // Use EntityQueryBuilder to avoid managed params array allocation
            _roomsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RoomHierarchyData, NodeId>()
                .Build(this);
            _sectorsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SectorHierarchyData, NodeId>()
                .Build(this);
            RequireForUpdate(_roomsQuery);
        }

        protected override void OnUpdate()
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
                if (EntityManager.HasComponent<RoomStateData>(entity))
                    continue;

                // Add room management components
                var random = new Unity.Mathematics.Random((uint)(nodeId.Value + 12345));
                
                // Add room state data
                var totalSecrets = DetermineSecretCount(roomData.Type, ref random);
                EntityManager.AddComponentData(entity, new RoomStateData(totalSecrets));

                // Add navigation data
                var isCriticalPath = roomData.Type == RoomType.Entrance || roomData.Type == RoomType.Exit || roomData.Type == RoomType.Boss;
                var traversalTime = CalculateTraversalTime(in roomData.Bounds, roomData.Type);
                CalculatePrimaryEntrance(in roomData.Bounds, out int2 primaryEntrance);
                EntityManager.AddComponentData(entity, new RoomNavigationData(primaryEntrance, isCriticalPath, traversalTime));

                // Add room features buffer
                var featuresBuffer = EntityManager.AddBuffer<RoomFeatureElement>(entity);
                PopulateRoomFeatures(featuresBuffer, in roomData, ref random);

                // Initialize room generation request for new procedural pipeline
                InitializeRoomGenerationRequest(EntityManager, entity, roomData, nodeId, ref random);
            }
        }

        /// <summary>
        /// Initialize room generation request for the new procedural pipeline
        /// </summary>
        private static void InitializeRoomGenerationRequest(EntityManager entityManager, Entity roomEntity, 
                                                           RoomHierarchyData roomData, NodeId nodeId, 
                                                           ref Unity.Mathematics.Random random)
        {
            // Determine generator type based on room type and characteristics
            var generatorType = DetermineGeneratorType(roomData.Type, roomData.Bounds);
            
            // Get biome information if available
            var targetBiome = BiomeType.HubArea;
            var targetPolarity = Polarity.None;
            
            // In a full implementation, would query for biome data from parent district/sector
            // For now, use default values
            
            // Determine available skills (in full implementation, would come from player state)
            var availableSkills = Ability.Jump | Ability.DoubleJump; // Basic starting abilities
            
            var generationRequest = new RoomGenerationRequest(
                generatorType, 
                targetBiome, 
                targetPolarity, 
                availableSkills, 
                random.NextUInt()
            );
            
            entityManager.AddComponentData(roomEntity, generationRequest);
            
            // Add additional components needed for specialized generators
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
                var secretConfig = new SecretAreaConfig(0.15f, new int2(2, 2), new int2(4, 4), 
                                                      Ability.None, true, true);
                entityManager.AddComponentData(roomEntity, secretConfig);
            }
        }

        /// <summary>
        /// Determine the appropriate generator type based on room characteristics
        /// </summary>
        private static RoomGeneratorType DetermineGeneratorType(RoomType roomType, RectInt bounds)
        {
            var aspectRatio = (float)bounds.width / bounds.height;
            
            return roomType switch
            {
                RoomType.Boss => RoomGeneratorType.PatternDrivenModular,     // Skill challenges
                RoomType.Treasure => RoomGeneratorType.ParametricChallenge, // Testing grounds
                RoomType.Save or RoomType.Shop or RoomType.Hub => RoomGeneratorType.WeightedTilePrefab, // Safe areas
                _ => aspectRatio > 1.5f ? RoomGeneratorType.LinearBranchingCorridor :  // Wide = horizontal
                     aspectRatio < 0.67f ? RoomGeneratorType.StackedSegment :         // Tall = vertical
                     RoomGeneratorType.WeightedTilePrefab                              // Square = standard
            };
        }

        /// <summary>
        /// Determine number of secrets based on room type and size
        /// </summary>
        private static int DetermineSecretCount(RoomType roomType, ref Unity.Mathematics.Random random)
        {
            return roomType switch
            {
                RoomType.Treasure => random.NextInt(2, 5),
                RoomType.Boss => random.NextInt(1, 3),
                RoomType.Hub => random.NextInt(0, 2),
                RoomType.Normal => random.NextFloat() > 0.7f ? random.NextInt(1, 2) : 0,
                _ => 0
            };
        }

        /// <summary>
        /// Calculate traversal time based on room size and type
        /// </summary>
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

        /// <summary>
        /// Calculate primary entrance position
        /// </summary>
        private static void CalculatePrimaryEntrance(in RectInt bounds, out int2 result)
        {
            result = new int2(bounds.x + bounds.width / 2, bounds.y);
        }

        /// <summary>
        /// Populate room with features based on type and size
        /// </summary>
        private static void PopulateRoomFeatures(DynamicBuffer<RoomFeatureElement> features, in RoomHierarchyData roomData, ref Unity.Mathematics.Random random)
        {
            var bounds = roomData.Bounds;
            var area = bounds.width * bounds.height;

            switch (roomData.Type)
            {
                case RoomType.Boss:
                    AddBossRoomFeatures(features, in bounds, ref random);
                    break;
                case RoomType.Treasure:
                    AddTreasureRoomFeatures(features, in bounds, ref random);
                    break;
                case RoomType.Save:
                    AddSaveRoomFeatures(features, in bounds, ref random);
                    break;
                case RoomType.Shop:
                    AddShopRoomFeatures(features, in bounds, ref random);
                    break;
                default:
                    AddNormalRoomFeatures(features, in bounds, area, ref random);
                    break;
            }
        }

        private static void AddBossRoomFeatures(DynamicBuffer<RoomFeatureElement> features, in RectInt bounds, ref Unity.Mathematics.Random random)
        {
            // Add boss spawn point
            var bossPos = new int2(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
            features.Add(new RoomFeatureElement(RoomFeatureType.Enemy, bossPos, random.NextUInt()));

            // Add some platforms for boss fight
            for (int i = 0; i < random.NextInt(2, 4); i++)
            {
                var platformPos = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                features.Add(new RoomFeatureElement(RoomFeatureType.Platform, platformPos, random.NextUInt()));
            }
        }

        private static void AddTreasureRoomFeatures(DynamicBuffer<RoomFeatureElement> features, in RectInt bounds, ref Unity.Mathematics.Random random)
        {
            // Add treasure chests/power-ups
            var treasureCount = random.NextInt(1, 3);
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

        private static void AddSaveRoomFeatures(DynamicBuffer<RoomFeatureElement> features, in RectInt bounds, ref Unity.Mathematics.Random random)
        {
            // Add save station
            var savePos = new int2(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
            features.Add(new RoomFeatureElement(RoomFeatureType.SaveStation, savePos, random.NextUInt()));
        }

        private static void AddShopRoomFeatures(DynamicBuffer<RoomFeatureElement> features, in RectInt bounds, ref Unity.Mathematics.Random random)
        {
            // Add platforms for shop items
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

        private static void AddNormalRoomFeatures(DynamicBuffer<RoomFeatureElement> features, in RectInt bounds, int area, ref Unity.Mathematics.Random random)
        {
            // Add enemies based on room size
            var enemyCount = math.min(area / 8, random.NextInt(0, 3));
            for (int i = 0; i < enemyCount; i++)
            {
                var enemyPos = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                features.Add(new RoomFeatureElement(RoomFeatureType.Enemy, enemyPos, random.NextUInt()));
            }

            // Add platforms and obstacles
            var featureCount = area / 12;
            for (int i = 0; i < featureCount; i++)
            {
                var featurePos = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                var featureType = random.NextFloat() > 0.6f ? RoomFeatureType.Platform : RoomFeatureType.Obstacle;
                features.Add(new RoomFeatureElement(featureType, featurePos, random.NextUInt()));
            }

            // Occasional health pickup
            if (random.NextFloat() > 0.8f)
            {
                var healthPos = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                features.Add(new RoomFeatureElement(RoomFeatureType.HealthPickup, healthPos, random.NextUInt()));
            }
        }
    }
}
