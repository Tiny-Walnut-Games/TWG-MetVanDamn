using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Graph
{
    public enum DistrictPlacementStrategy : byte { PoissonDisc = 0, JitteredGrid = 1 }

    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct DistrictLayoutSystem : ISystem
    {
        private EntityQuery _unplacedQuery;
        private EntityQuery _worldConfigQuery;
        private EntityQuery _layoutDoneQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _unplacedQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NodeId, WfcState>()
                .Build(ref state);
            _worldConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<WorldConfiguration>()
                .Build(ref state);
            _layoutDoneQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<DistrictLayoutDoneTag>()
                .Build(ref state);
            state.RequireForUpdate(_unplacedQuery);
            state.RequireForUpdate(_worldConfigQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_layoutDoneQuery.IsEmptyIgnoreFilter)
                return;
            var worldConfig = _worldConfigQuery.GetSingleton<WorldConfiguration>();
            var unplacedEntities = _unplacedQuery.ToEntityArray(Allocator.Temp);
            var nodeIds = _unplacedQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            try
            {
                var unplacedCount = 0;
                for (int i = 0; i < nodeIds.Length; i++)
                {
                    var nodeId = nodeIds[i];
                    if (nodeId.Level == 0 && nodeId.Coordinates.x == 0 && nodeId.Coordinates.y == 0)
                        unplacedCount++;
                }
                if (unplacedCount == 0)
                {
                    var layoutDoneEntity = state.EntityManager.CreateEntity();
                    state.EntityManager.AddComponentData(layoutDoneEntity, new DistrictLayoutDoneTag(0, 0));
                    return;
                }
                var targetDistrictCount = worldConfig.TargetSectors > 0 ? math.min(worldConfig.TargetSectors, unplacedCount) : unplacedCount;
                var random = new Unity.Mathematics.Random((uint)worldConfig.Seed);
                var strategy = targetDistrictCount > 16 ? DistrictPlacementStrategy.JitteredGrid : DistrictPlacementStrategy.PoissonDisc;
                var positions = new NativeArray<int2>(targetDistrictCount, Allocator.Temp);
                try
                {
                    GenerateDistrictPositions(positions, worldConfig.WorldSize, strategy, ref random);
                    int positionIndex = 0;
                    int placedCount = 0;
                    for (int i = 0; i < nodeIds.Length && placedCount < targetDistrictCount; i++)
                    {
                        var nodeId = nodeIds[i];
                        if (nodeId.Level == 0 && nodeId.Coordinates.x == 0 && nodeId.Coordinates.y == 0)
                        {
                            nodeId.Coordinates = positions[positionIndex++];
                            state.EntityManager.SetComponentData(unplacedEntities[i], nodeId);
                            var sectorData = new SectorHierarchyData(new int2(6, 6), math.max(1, worldConfig.TargetSectors / targetDistrictCount), random.NextUInt());
                            state.EntityManager.AddComponentData(unplacedEntities[i], sectorData);
                            placedCount++;
                        }
                    }
                    var doneEntity = state.EntityManager.CreateEntity();
                    state.EntityManager.AddComponentData(doneEntity, new DistrictLayoutDoneTag(placedCount, 0));
                }
                finally { if (positions.IsCreated) positions.Dispose(); }
            }
            finally
            {
                if (unplacedEntities.IsCreated) unplacedEntities.Dispose();
                if (nodeIds.IsCreated) nodeIds.Dispose();
            }
        }

        static void GenerateDistrictPositions(NativeArray<int2> positions, int2 worldSize, DistrictPlacementStrategy strategy, ref Unity.Mathematics.Random random)
        {
            switch (strategy)
            {
                case DistrictPlacementStrategy.PoissonDisc: GeneratePoissonDiscPositions(positions, worldSize, ref random); break;
                case DistrictPlacementStrategy.JitteredGrid: GenerateJitteredGridPositions(positions, worldSize, ref random); break;
            }
        }
        static void GeneratePoissonDiscPositions(NativeArray<int2> positions, int2 worldSize, ref Unity.Mathematics.Random random)
        {
            float minDistance = math.min(worldSize.x, worldSize.y) * 0.2f;
            int maxAttempts = 30;
            for (int i = 0; i < positions.Length; i++)
            {
                bool validPosition = false; int attempts = 0;
                while (!validPosition && attempts < maxAttempts)
                {
                    int2 candidate = new(random.NextInt(0, worldSize.x), random.NextInt(0, worldSize.y));
                    validPosition = true;
                    for (int j = 0; j < i; j++)
                    {
                        float distance = math.length(new float2(candidate - positions[j]));
                        if (distance < minDistance) { validPosition = false; break; }
                    }
                    if (validPosition) positions[i] = candidate;
                    attempts++;
                }
                if (!validPosition)
                    positions[i] = new(random.NextInt(0, worldSize.x), random.NextInt(0, worldSize.y));
            }
        }
        static void GenerateJitteredGridPositions(NativeArray<int2> positions, int2 worldSize, ref Unity.Mathematics.Random random)
        {
            int gridDim = (int)math.ceil(math.sqrt(positions.Length));
            float2 cellSize = new float2(worldSize) / gridDim;
            float jitterAmount = math.min(cellSize.x, cellSize.y) * 0.3f;
            var shuffledIndices = new NativeArray<int>(positions.Length, Allocator.Temp);
            for (int i = 0; i < shuffledIndices.Length; i++) shuffledIndices[i] = i;
            for (int i = shuffledIndices.Length - 1; i > 0; i--)
            { int j = random.NextInt(0, i + 1); (shuffledIndices[i], shuffledIndices[j]) = (shuffledIndices[j], shuffledIndices[i]); }
            for (int i = 0; i < positions.Length; i++)
            {
                int gridIndex = shuffledIndices[i];
                int gridX = gridIndex % gridDim; int gridY = gridIndex / gridDim;
                float2 cellCenter = new float2(gridX + 0.5f, gridY + 0.5f) * cellSize;
                float2 jitter = new(random.NextFloat(-jitterAmount, jitterAmount), random.NextFloat(-jitterAmount, jitterAmount));
                float2 finalPosition = cellCenter + jitter;
                positions[i] = new(math.clamp((int)finalPosition.x, 0, worldSize.x - 1), math.clamp((int)finalPosition.y, 0, worldSize.y - 1));
            }
            shuffledIndices.Dispose();
        }
    }
}
