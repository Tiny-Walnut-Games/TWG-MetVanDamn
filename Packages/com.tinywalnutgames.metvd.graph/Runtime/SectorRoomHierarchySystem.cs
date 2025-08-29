using System;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    public static class HierarchyConstants { public const uint SectorIdMultiplier = 1000; public const uint RoomsPerSectorMultiplier = 100; }
    public struct SectorHierarchyData : IComponentData { public int2 LocalGridSize; public int SectorCount; public bool IsSubdivided; public uint SectorSeed; public SectorHierarchyData(int2 localGridSize, int sectorCount, uint sectorSeed){ LocalGridSize = localGridSize; SectorCount = sectorCount; IsSubdivided = false; SectorSeed = sectorSeed; } }
    public struct RoomHierarchyData : IComponentData { public RectInt Bounds; public RoomType Type; public bool IsLeafRoom; public RoomHierarchyData(RectInt bounds, RoomType type, bool isLeafRoom=false){ Bounds=bounds; Type=type; IsLeafRoom=isLeafRoom; } }
    public enum RoomType : byte { Normal, Entrance, Exit, Boss, Treasure, Shop, Save, Hub }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(DistrictLayoutSystem))]
    public partial struct SectorRoomHierarchySystem : ISystem
    {
        private EntityQuery _districtsQuery; private EntityQuery _layoutDoneQuery;
        public void OnCreate(ref SystemState state)
        {
            _districtsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NodeId, SectorHierarchyData>()
                .Build(ref state);
            _layoutDoneQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<DistrictLayoutDoneTag>()
                .Build(ref state);
            state.RequireForUpdate(_districtsQuery);
            state.RequireForUpdate(_layoutDoneQuery);
        }
        public void OnUpdate(ref SystemState state)
        {
            if (_layoutDoneQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            using NativeArray<Entity> entities = _districtsQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<NodeId> nodeIds = _districtsQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            using NativeArray<SectorHierarchyData> sectorData = _districtsQuery.ToComponentDataArray<SectorHierarchyData>(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                NodeId nodeId = nodeIds[i]; SectorHierarchyData sectorHierarchy = sectorData[i];
                if (nodeId.Level == 0 && !sectorHierarchy.IsSubdivided)
                {
                    SubdivideDistrictIntoSectors(state.EntityManager, entities[i], nodeId, sectorHierarchy);
                }
            }
        }
        private static void SubdivideDistrictIntoSectors(EntityManager entityManager, Entity districtEntity, NodeId districtNodeId, SectorHierarchyData sectorHierarchy)
        {
            var random = new Unity.Mathematics.Random(sectorHierarchy.SectorSeed);
            int2 gridSize = sectorHierarchy.LocalGridSize; int totalCells = gridSize.x * gridSize.y; int actualSectorCount = math.min(sectorHierarchy.SectorCount, totalCells);
            for (int sectorIndex = 0; sectorIndex < actualSectorCount; sectorIndex++)
            {
                int gridX = sectorIndex % gridSize.x; int gridY = sectorIndex / gridSize.x;
                float jitterX = random.NextFloat(-0.3f, 0.3f); float jitterY = random.NextFloat(-0.3f, 0.3f);
                int2 sectorLocalCoords = new((int)(gridX + jitterX),(int)(gridY + jitterY));
                Entity sectorEntity = entityManager.CreateEntity();
                var sectorNodeId = new NodeId((uint)(districtNodeId._value * HierarchyConstants.SectorIdMultiplier + sectorIndex),1,districtNodeId._value,sectorLocalCoords);
                entityManager.AddComponentData(sectorEntity, sectorNodeId);
                CreateRoomsInSector(entityManager, sectorNodeId, ref random);
            }
            sectorHierarchy.IsSubdivided = true; entityManager.SetComponentData(districtEntity, sectorHierarchy);
        }
        private static void CreateRoomsInSector(EntityManager entityManager, NodeId sectorNodeId, ref Unity.Mathematics.Random random)
        {
            var sectorBounds = new RectInt(0,0,8,8);
            var roomQueue = new NativeList<RectInt>(Allocator.Temp) { sectorBounds };
            int roomCounter = 0; int maxRooms = 6; int minRoomSize = 2;
            while (roomQueue.Length > 0 && roomCounter < maxRooms)
            {
                RectInt currentBounds = roomQueue[0]; roomQueue.RemoveAt(0);
                if (currentBounds.width <= minRoomSize || currentBounds.height <= minRoomSize)
                { CreateLeafRoom(entityManager, sectorNodeId, currentBounds, roomCounter, ref random); roomCounter++; continue; }
                bool splitHorizontally = currentBounds.width > currentBounds.height ? random.NextFloat() > 0.3f : random.NextFloat() > 0.7f;
                if (splitHorizontally && currentBounds.height > minRoomSize * 2)
                {
                    int splitY = random.NextInt(currentBounds.y + minRoomSize, currentBounds.y + currentBounds.height - minRoomSize);
                    roomQueue.Add(new RectInt(currentBounds.x, currentBounds.y, currentBounds.width, splitY - currentBounds.y));
                    roomQueue.Add(new RectInt(currentBounds.x, splitY, currentBounds.width, currentBounds.y + currentBounds.height - splitY));
                }
                else if (!splitHorizontally && currentBounds.width > minRoomSize * 2)
                {
                    int splitX = random.NextInt(currentBounds.x + minRoomSize, currentBounds.x + currentBounds.width - minRoomSize);
                    roomQueue.Add(new RectInt(currentBounds.x, currentBounds.y, splitX - currentBounds.x, currentBounds.height));
                    roomQueue.Add(new RectInt(splitX, currentBounds.y, currentBounds.x + currentBounds.width - splitX, currentBounds.height));
                }
                else
                { CreateLeafRoom(entityManager, sectorNodeId, currentBounds, roomCounter, ref random); roomCounter++; }
            }
            while (roomQueue.Length > 0 && roomCounter < maxRooms)
            { CreateLeafRoom(entityManager, sectorNodeId, roomQueue[0], roomCounter, ref random); roomQueue.RemoveAt(0); roomCounter++; }
            roomQueue.Dispose();
        }
        private static void CreateLeafRoom(EntityManager entityManager, NodeId sectorNodeId, RectInt bounds, int roomIndex, ref Unity.Mathematics.Random random)
        {
            Entity roomEntity = entityManager.CreateEntity();
            var roomNodeId = new NodeId((uint)(sectorNodeId._value * HierarchyConstants.RoomsPerSectorMultiplier + roomIndex),2, sectorNodeId._value,new int2(bounds.x + bounds.width/2, bounds.y + bounds.height/2));
            RoomType roomType = RoomType.Normal;
            if (roomIndex == 0)
            {
                roomType = random.NextFloat() > 0.7f ? RoomType.Entrance : RoomType.Normal;
            }
            else if (bounds.width * bounds.height > 6)
            {
                float typeRoll = random.NextFloat();
                if (typeRoll > 0.9f)
                {
                    roomType = RoomType.Boss;
                }
                else if (typeRoll > 0.8f)
                {
                    roomType = RoomType.Treasure;
                }
                else if (typeRoll > 0.7f)
                {
                    roomType = RoomType.Save;
                }
                else
                {
                    roomType = RoomType.Normal;
                }
            }
            entityManager.AddComponentData(roomEntity, roomNodeId);
            entityManager.AddComponentData(roomEntity, new RoomHierarchyData(bounds, roomType, true));
        }
    }
}
