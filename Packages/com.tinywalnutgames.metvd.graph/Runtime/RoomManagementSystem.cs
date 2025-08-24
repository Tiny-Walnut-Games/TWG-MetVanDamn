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
            _roomsQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<RoomHierarchyData>(),
                ComponentType.ReadOnly<NodeId>()
            );
            
            _sectorsQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<SectorHierarchyData>(),
                ComponentType.ReadOnly<NodeId>()
            );

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

                // Add room management components
                var random = new Unity.Mathematics.Random((uint)(nodeId.Value + 12345));
                
                // Add room state data
                var totalSecrets = DetermineSecretCount(roomData.Type, ref random);
                state.EntityManager.AddComponentData(entity, new RoomStateData(totalSecrets));

                // Add navigation data
                var isCriticalPath = roomData.Type == RoomType.Entrance || roomData.Type == RoomType.Exit || roomData.Type == RoomType.Boss;
                var traversalTime = CalculateTraversalTime(roomData.Bounds, roomData.Type);
                var primaryEntrance = CalculatePrimaryEntrance(roomData.Bounds);
                state.EntityManager.AddComponentData(entity, new RoomNavigationData(primaryEntrance, isCriticalPath, traversalTime));

                // Add room features buffer
                var featuresBuffer = state.EntityManager.AddBuffer<RoomFeatureElement>(entity);
                PopulateRoomFeatures(featuresBuffer, roomData, ref random);
            }
        }

        /// <summary>
        /// Determine number of secrets based on room type and size
        /// </summary>
        [BurstCompile]
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
        [BurstCompile]
        private static float CalculateTraversalTime(RectInt bounds, RoomType roomType)
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
        [BurstCompile]
        private static int2 CalculatePrimaryEntrance(RectInt bounds)
        {
            // Use bottom-center as default entrance
            return new int2(bounds.x + bounds.width / 2, bounds.y);
        }

        /// <summary>
        /// Populate room with features based on type and size
        /// </summary>
        [BurstCompile]
        private static void PopulateRoomFeatures(DynamicBuffer<RoomFeatureElement> features, RoomHierarchyData roomData, ref Unity.Mathematics.Random random)
        {
            var bounds = roomData.Bounds;
            var area = bounds.width * bounds.height;

            switch (roomData.Type)
            {
                case RoomType.Boss:
                    AddBossRoomFeatures(features, bounds, ref random);
                    break;
                case RoomType.Treasure:
                    AddTreasureRoomFeatures(features, bounds, ref random);
                    break;
                case RoomType.Save:
                    AddSaveRoomFeatures(features, bounds, ref random);
                    break;
                case RoomType.Shop:
                    AddShopRoomFeatures(features, bounds, ref random);
                    break;
                default:
                    AddNormalRoomFeatures(features, bounds, area, ref random);
                    break;
            }
        }

        [BurstCompile]
        private static void AddBossRoomFeatures(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, ref Unity.Mathematics.Random random)
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

        [BurstCompile]
        private static void AddTreasureRoomFeatures(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, ref Unity.Mathematics.Random random)
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

        [BurstCompile]
        private static void AddSaveRoomFeatures(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, ref Unity.Mathematics.Random random)
        {
            // Add save station
            var savePos = new int2(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2);
            features.Add(new RoomFeatureElement(RoomFeatureType.SaveStation, savePos, random.NextUInt()));
        }

        [BurstCompile]
        private static void AddShopRoomFeatures(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, ref Unity.Mathematics.Random random)
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

        [BurstCompile]
        private static void AddNormalRoomFeatures(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, int area, ref Unity.Mathematics.Random random)
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
