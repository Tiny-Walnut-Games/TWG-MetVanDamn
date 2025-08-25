using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// District placement strategy for procedural layout
    /// </summary>
    public enum DistrictPlacementStrategy : byte
    {
        PoissonDisc = 0,    // Organic spacing using simple rejection sampling
        JitteredGrid = 1    // Grid-based with jitter for variation
    }

    /// <summary>
    /// System responsible for procedural district layout before WFC
    /// Places districts using deterministic algorithms based on world configuration
    /// </summary>
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
            // Query for unplaced districts (Level 0, Coordinates (0,0))
            _unplacedQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<NodeId>(),
                ComponentType.ReadOnly<WfcState>()
            );

            // Query for world configuration
            _worldConfigQuery = state.GetEntityQuery(ComponentType.ReadOnly<WorldConfiguration>());

            // Query to check if layout is already done
            _layoutDoneQuery = state.GetEntityQuery(ComponentType.ReadOnly<DistrictLayoutDoneTag>());

            state.RequireForUpdate(_unplacedQuery);
            state.RequireForUpdate(_worldConfigQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Skip if layout already done
            if (!_layoutDoneQuery.IsEmptyIgnoreFilter)
                return;

            var worldConfig = _worldConfigQuery.GetSingleton<WorldConfiguration>();

            // Find unplaced districts (those at coordinates 0,0 with Level 0)
            var unplacedEntities = _unplacedQuery.ToEntityArray(Allocator.Temp);
            var nodeIds = _unplacedQuery.ToComponentDataArray<NodeId>(Allocator.Temp);

            try
            {
                var unplacedCount = 0;
                for (int i = 0; i < nodeIds.Length; i++)
                {
                    var nodeId = nodeIds[i];
                    if (nodeId.Level == 0 && nodeId.Coordinates.x == 0 && nodeId.Coordinates.y == 0)
                    {
                        unplacedCount++;
                    }
                }

                if (unplacedCount == 0)
                {
                    // No unplaced districts, mark as done
                    var layoutDoneEntity = state.EntityManager.CreateEntity();
                    state.EntityManager.AddComponentData(layoutDoneEntity, new DistrictLayoutDoneTag(0, 0));
                    return;
                }

                // Use TargetSectors to determine how many districts to place
                // If TargetSectors is specified, limit the number of districts accordingly
                var targetDistrictCount = worldConfig.TargetSectors > 0 ?
                    math.min(worldConfig.TargetSectors, unplacedCount) : unplacedCount;

                // Initialize random generator with world seed
                var random = new Unity.Mathematics.Random((uint)worldConfig.Seed);

                // Choose placement strategy based on target district count
                var strategy = targetDistrictCount > 16 ? DistrictPlacementStrategy.JitteredGrid : DistrictPlacementStrategy.PoissonDisc;

                // Generate district positions for target count
                var positions = new NativeArray<int2>(targetDistrictCount, Allocator.Temp);
                try
                {
                    GenerateDistrictPositions(positions, worldConfig.WorldSize, strategy, ref random);

                    // Apply positions to unplaced districts (up to target count)
                    int positionIndex = 0;
                    int placedCount = 0;
                    for (int i = 0; i < nodeIds.Length && placedCount < targetDistrictCount; i++)
                    {
                        var nodeId = nodeIds[i];
                        if (nodeId.Level == 0 && nodeId.Coordinates.x == 0 && nodeId.Coordinates.y == 0)
                        {
                            nodeId.Coordinates = positions[positionIndex++];
                            state.EntityManager.SetComponentData(unplacedEntities[i], nodeId);

                            // Add SectorHierarchyData to each placed district for later subdivision
                            var sectorData = new SectorHierarchyData(
                                new int2(6, 6), // Local grid size for sectors
                                math.max(1, worldConfig.TargetSectors / targetDistrictCount), // Sectors per district
                                random.NextUInt()
                            );
                            state.EntityManager.AddComponentData(unplacedEntities[i], sectorData);

                            placedCount++;
                        }
                    }

                    // Mark layout as complete
                    var doneEntity = state.EntityManager.CreateEntity();
                    state.EntityManager.AddComponentData(doneEntity, new DistrictLayoutDoneTag(placedCount, 0));
                }
                finally
                {
                    if (positions.IsCreated) positions.Dispose();
                }
            }
            finally
            {
                if (unplacedEntities.IsCreated) unplacedEntities.Dispose();
                if (nodeIds.IsCreated) nodeIds.Dispose();
            }
        }

        /// <summary>
        /// Generate district positions using the specified strategy
        /// </summary>
        [BurstCompile]
        static void GenerateDistrictPositions(NativeArray<int2> positions, int2 worldSize,
            DistrictPlacementStrategy strategy, ref Unity.Mathematics.Random random)
        {
            switch (strategy)
            {
                case DistrictPlacementStrategy.PoissonDisc:
                    GeneratePoissonDiscPositions(positions, worldSize, ref random);
                    break;
                case DistrictPlacementStrategy.JitteredGrid:
                    GenerateJitteredGridPositions(positions, worldSize, ref random);
                    break;
            }
        }

        /// <summary>
        /// Generate positions using Poisson-disc sampling for organic spacing
        /// Simple rejection sampling implementation
        /// </summary>
        [BurstCompile]
        static void GeneratePoissonDiscPositions(NativeArray<int2> positions, int2 worldSize,
            ref Unity.Mathematics.Random random)
        {
            float minDistance = math.min(worldSize.x, worldSize.y) * 0.2f; // 20% of world size as minimum distance
            int maxAttempts = 30;

            for (int i = 0; i < positions.Length; i++)
            {
                bool validPosition = false;
                int attempts = 0;

                while (!validPosition && attempts < maxAttempts)
                {
                    // Generate random position within world bounds
                    int2 candidate = new(
                        random.NextInt(0, worldSize.x),
                        random.NextInt(0, worldSize.y)
                    );

                    // Check distance to all previously placed positions
                    validPosition = true;
                    for (int j = 0; j < i; j++)
                    {
                        float distance = math.length(new float2(candidate - positions[j]));
                        if (distance < minDistance)
                        {
                            validPosition = false;
                            break;
                        }
                    }

                    if (validPosition)
                    {
                        positions[i] = candidate;
                    }

                    attempts++;
                }

                // Fallback if no valid position found
                if (!validPosition)
                {
                    positions[i] = new int2(
                        random.NextInt(0, worldSize.x),
                        random.NextInt(0, worldSize.y)
                    );
                }
            }
        }

        /// <summary>
        /// Generate positions using jittered grid for larger district counts
        /// </summary>
        [BurstCompile]
        static void GenerateJitteredGridPositions(NativeArray<int2> positions, int2 worldSize,
            ref Unity.Mathematics.Random random)
        {
            int gridDim = (int)math.ceil(math.sqrt(positions.Length));
            float2 cellSize = new float2(worldSize) / gridDim;
            float jitterAmount = math.min(cellSize.x, cellSize.y) * 0.3f; // 30% jitter

            // Create shuffled indices for grid cells
            var shuffledIndices = new NativeArray<int>(positions.Length, Allocator.Temp);
            for (int i = 0; i < shuffledIndices.Length; i++)
            {
                shuffledIndices[i] = i;
            }

            // Simple shuffle
            for (int i = shuffledIndices.Length - 1; i > 0; i--)
            {
                int j = random.NextInt(0, i + 1);
                (shuffledIndices[i], shuffledIndices[j]) = (shuffledIndices[j], shuffledIndices[i]);
            }

            // Assign positions based on shuffled grid
            for (int i = 0; i < positions.Length; i++)
            {
                int gridIndex = shuffledIndices[i];
                int gridX = gridIndex % gridDim;
                int gridY = gridIndex / gridDim;

                // Calculate cell center
                float2 cellCenter = new float2(gridX + 0.5f, gridY + 0.5f) * cellSize;

                // Apply jitter
                float2 jitter = new(
                    random.NextFloat(-jitterAmount, jitterAmount),
                    random.NextFloat(-jitterAmount, jitterAmount)
                );

                float2 finalPosition = cellCenter + jitter;

                // Clamp to world bounds
                positions[i] = new(
                    math.clamp((int)finalPosition.x, 0, worldSize.x - 1),
                    math.clamp((int)finalPosition.y, 0, worldSize.y - 1)
                );
            }
        }
    }
}
