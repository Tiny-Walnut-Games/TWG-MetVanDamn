using System;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Constants for hierarchical ID generation
    /// </summary>
    public static class HierarchyConstants
    {
        public const uint SectorIdMultiplier = 1000;
        public const uint RoomsPerSectorMultiplier = 100;
    }

    /// <summary>
    /// Component for sector hierarchy data within districts
    /// </summary>
    public struct SectorHierarchyData : IComponentData
    {
        /// <summary>
        /// Local grid size for sector subdivision (e.g., 6x6)
        /// </summary>
        public int2 LocalGridSize;
        
        /// <summary>
        /// Number of sectors to create within this district
        /// </summary>
        public int SectorCount;
        
        /// <summary>
        /// Whether sector subdivision has been completed
        /// </summary>
        public bool IsSubdivided;
        
        /// <summary>
        /// Random seed for deterministic sector generation
        /// </summary>
        public uint SectorSeed;

        public SectorHierarchyData(int2 localGridSize, int sectorCount, uint sectorSeed)
        {
            LocalGridSize = localGridSize;
            SectorCount = sectorCount;
            IsSubdivided = false;
            SectorSeed = sectorSeed;
        }
    }

    /// <summary>
    /// Component for room hierarchy data within sectors
    /// </summary>
    public struct RoomHierarchyData : IComponentData
    {
        /// <summary>
        /// Bounds of this room within its sector
        /// </summary>
        public RectInt Bounds;
        
        /// <summary>
        /// Room type for different gameplay purposes
        /// </summary>
        public RoomType Type;
        
        /// <summary>
        /// Whether this room has been subdivided to minimum size
        /// </summary>
        public bool IsLeafRoom;

        public RoomHierarchyData(RectInt bounds, RoomType type, bool isLeafRoom = false)
        {
            Bounds = bounds;
            Type = type;
            IsLeafRoom = isLeafRoom;
        }
    }

    /// <summary>
    /// Room types for different gameplay purposes
    /// </summary>
    public enum RoomType : byte
    {
        Normal = 0,
        Entrance = 1,
        Exit = 2,
        Boss = 3,
        Treasure = 4,
        Shop = 5,
        Save = 6,
        Hub = 7
    }

    /// <summary>
    /// System that creates sector and room hierarchies within each district
    /// Runs after district layout to subdivide districts into gameplay areas
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(DistrictLayoutSystem))]
    public partial struct SectorRoomHierarchySystem : ISystem
    {
        private EntityQuery _districtsQuery;
        private EntityQuery _layoutDoneQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _districtsQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<NodeId>(),
                ComponentType.ReadWrite<SectorHierarchyData>()
            );
            _layoutDoneQuery = state.GetEntityQuery(ComponentType.ReadOnly<DistrictLayoutDoneTag>());

            state.RequireForUpdate(_districtsQuery);
            state.RequireForUpdate(_layoutDoneQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Wait for district layout to complete
            if (_layoutDoneQuery.IsEmptyIgnoreFilter) return;

            // Get districts that need sector subdivision
            using var entities = _districtsQuery.ToEntityArray(Allocator.Temp);
            using var nodeIds = _districtsQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            using var sectorData = _districtsQuery.ToComponentDataArray<SectorHierarchyData>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var nodeId = nodeIds[i];
                var sectorHierarchy = sectorData[i];

                // Only process level 0 districts that haven't been subdivided
                if (nodeId.Level == 0 && !sectorHierarchy.IsSubdivided)
                {
                    SubdivideDistrictIntoSectors(state.EntityManager, entities[i], nodeId, sectorHierarchy);
                }
            }
        }

        /// <summary>
        /// Subdivide a district into sectors and rooms
        /// </summary>
        [BurstCompile]
        private static void SubdivideDistrictIntoSectors(EntityManager entityManager, Entity districtEntity, 
            NodeId districtNodeId, SectorHierarchyData sectorHierarchy)
        {
            var random = new Unity.Mathematics.Random(sectorHierarchy.SectorSeed);
            
            // Create local grid for sectors within district bounds
            var gridSize = sectorHierarchy.LocalGridSize;
            var totalCells = gridSize.x * gridSize.y;
            var actualSectorCount = math.min(sectorHierarchy.SectorCount, totalCells);

            // Create sectors using jittered placement
            for (int sectorIndex = 0; sectorIndex < actualSectorCount; sectorIndex++)
            {
                // Calculate sector position within local grid
                int gridX = sectorIndex % gridSize.x;
                int gridY = sectorIndex / gridSize.x;
                
                // Add jitter within the grid cell
                float jitterX = random.NextFloat(-0.3f, 0.3f);
                float jitterY = random.NextFloat(-0.3f, 0.3f);
                
                int2 sectorLocalCoords = new int2(
                    (int)(gridX + jitterX),
                    (int)(gridY + jitterY)
                );

                // Create sector entity
                var sectorEntity = entityManager.CreateEntity();
                var sectorNodeId = new NodeId(
                    (uint)(districtNodeId.Value * HierarchyConstants.SectorIdMultiplier + sectorIndex), // Unique sector ID
                    1, // Level 1 = sector
                    districtNodeId.Value, // Parent is district
                    sectorLocalCoords
                );
                
                entityManager.AddComponentData(sectorEntity, sectorNodeId);

                // Create rooms within this sector using BSP subdivision
                CreateRoomsInSector(entityManager, sectorEntity, sectorNodeId, ref random);
            }

            // Mark district as subdivided
            sectorHierarchy.IsSubdivided = true;
            entityManager.SetComponentData(districtEntity, sectorHierarchy);
        }

        /// <summary>
        /// Create rooms within a sector using BSP (Binary Space Partitioning) subdivision
        /// </summary>
        [BurstCompile]
        private static void CreateRoomsInSector(EntityManager entityManager, Entity sectorEntity, 
            NodeId sectorNodeId, ref Unity.Mathematics.Random random)
        {
            // Define sector bounds (local coordinate system)
            var sectorBounds = new RectInt(0, 0, 8, 8); // 8x8 local grid for rooms
            
            // Use BSP to create rooms
            var roomQueue = new NativeList<RectInt>(Allocator.Temp);
            roomQueue.Add(sectorBounds);

            int roomCounter = 0;
            int maxRooms = 6; // Maximum rooms per sector
            int minRoomSize = 2; // Minimum room dimension

            while (roomQueue.Length > 0 && roomCounter < maxRooms)
            {
                var currentBounds = roomQueue[0];
                roomQueue.RemoveAt(0);

                // Check if room is too small to subdivide
                if (currentBounds.width <= minRoomSize || currentBounds.height <= minRoomSize)
                {
                    CreateLeafRoom(entityManager, sectorNodeId, currentBounds, roomCounter, ref random);
                    roomCounter++;
                    continue;
                }

                // Decide split direction (prefer splitting along longer axis)
                bool splitHorizontally = currentBounds.width > currentBounds.height ? 
                    random.NextFloat() > 0.3f : random.NextFloat() > 0.7f;

                if (splitHorizontally && currentBounds.height > minRoomSize * 2)
                {
                    // Split horizontally
                    int splitY = random.NextInt(currentBounds.y + minRoomSize, 
                        currentBounds.y + currentBounds.height - minRoomSize);
                    
                    roomQueue.Add(new RectInt(currentBounds.x, currentBounds.y, 
                        currentBounds.width, splitY - currentBounds.y));
                    roomQueue.Add(new RectInt(currentBounds.x, splitY, 
                        currentBounds.width, currentBounds.y + currentBounds.height - splitY));
                }
                else if (!splitHorizontally && currentBounds.width > minRoomSize * 2)
                {
                    // Split vertically
                    int splitX = random.NextInt(currentBounds.x + minRoomSize, 
                        currentBounds.x + currentBounds.width - minRoomSize);
                    
                    roomQueue.Add(new RectInt(currentBounds.x, currentBounds.y, 
                        splitX - currentBounds.x, currentBounds.height));
                    roomQueue.Add(new RectInt(splitX, currentBounds.y, 
                        currentBounds.x + currentBounds.width - splitX, currentBounds.height));
                }
                else
                {
                    // Can't split, create leaf room
                    CreateLeafRoom(entityManager, sectorNodeId, currentBounds, roomCounter, ref random);
                    roomCounter++;
                }
            }

            // Create any remaining rooms from the queue
            while (roomQueue.Length > 0 && roomCounter < maxRooms)
            {
                CreateLeafRoom(entityManager, sectorNodeId, roomQueue[0], roomCounter, ref random);
                roomQueue.RemoveAt(0);
                roomCounter++;
            }

            roomQueue.Dispose();
        }

        /// <summary>
        /// Create a leaf room entity
        /// </summary>
        [BurstCompile]
        private static void CreateLeafRoom(EntityManager entityManager, NodeId sectorNodeId, 
            RectInt bounds, int roomIndex, ref Unity.Mathematics.Random random)
        {
            var roomEntity = entityManager.CreateEntity();
            
            // Create room node ID
            var roomNodeId = new NodeId(
                (uint)(sectorNodeId.Value * HierarchyConstants.RoomsPerSectorMultiplier + roomIndex), // Unique room ID
                2, // Level 2 = room
                sectorNodeId.Value, // Parent is sector
                new int2(bounds.x + bounds.width/2, bounds.y + bounds.height/2) // Room center
            );

            // Determine room type
            RoomType roomType = RoomType.Normal;
            if (roomIndex == 0)
            {
                roomType = random.NextFloat() > 0.7f ? RoomType.Entrance : RoomType.Normal;
            }
            else if (bounds.width * bounds.height > 6) // Larger rooms
            {
                float typeRoll = random.NextFloat();
                if (typeRoll > 0.9f) roomType = RoomType.Boss;
                else if (typeRoll > 0.8f) roomType = RoomType.Treasure;
                else if (typeRoll > 0.7f) roomType = RoomType.Save;
                else roomType = RoomType.Normal;
            }

            entityManager.AddComponentData(roomEntity, roomNodeId);
            entityManager.AddComponentData(roomEntity, new RoomHierarchyData(bounds, roomType, true));
        }
    }
}
