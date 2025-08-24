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
    /// System that builds connection graph between districts after layout is complete
    /// Uses K-nearest neighbors plus random long edges for loops and replayability
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(DistrictLayoutSystem))]
    public partial struct ConnectionBuilderSystem : ISystem
    {
        private EntityQuery _layoutDoneQuery;
        private EntityQuery _districtsQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _layoutDoneQuery = state.GetEntityQuery(ComponentType.ReadWrite<DistrictLayoutDoneTag>());
            _districtsQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<NodeId>(),
                ComponentType.ReadWrite<ConnectionBufferElement>()
            );

            state.RequireForUpdate(_layoutDoneQuery);
            state.RequireForUpdate(_districtsQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Check if we need to build connections
            var layoutDoneArray = _layoutDoneQuery.ToComponentDataArray<DistrictLayoutDoneTag>(Allocator.Temp);
            if (layoutDoneArray.Length == 0) return;

            var layoutDone = layoutDoneArray[0];
            layoutDoneArray.Dispose();

            // Skip if connections already built
            if (layoutDone.ConnectionCount > 0) return;

            // Get all districts
            using var entities = _districtsQuery.ToEntityArray(Allocator.Temp);
            using var nodeIds = _districtsQuery.ToComponentDataArray<NodeId>(Allocator.Temp);

            // Filter to level 0 districts only
            var districtCount = 0;
            for (int i = 0; i < nodeIds.Length; i++)
            {
                if (nodeIds[i].Level == 0) districtCount++;
            }

            if (districtCount < 2) return; // Need at least 2 districts to connect

            // Create arrays for district data
            var districtEntitiesArray = new NativeArray<Entity>(districtCount, Allocator.Temp);
            var districtPositionsArray = new NativeArray<int2>(districtCount, Allocator.Temp);
            var districtNodeIdsArray = new NativeArray<uint>(districtCount, Allocator.Temp);

            int districtIndex = 0;
            for (int i = 0; i < nodeIds.Length; i++)
            {
                if (nodeIds[i].Level == 0)
                {
                    districtEntitiesArray[districtIndex] = entities[i];
                    districtPositionsArray[districtIndex] = nodeIds[i].Coordinates;
                    districtNodeIdsArray[districtIndex] = nodeIds[i].Value;
                    districtIndex++;
                }
            }

            // Get world configuration for random seed
            var worldConfigQuery = state.GetEntityQuery(ComponentType.ReadOnly<WorldConfiguration>());
            var worldConfig = worldConfigQuery.GetSingleton<WorldConfiguration>();
            var random = new Unity.Mathematics.Random((uint)(worldConfig.Seed + 1337)); // Different seed for connections

            // Build connection graph
            var connectionCount = BuildConnectionGraph(
                state.EntityManager,
                districtEntitiesArray,
                districtPositionsArray,
                districtNodeIdsArray,
                ref random
            );

            // Update layout done tag with connection count
            var layoutDoneEntity = _layoutDoneQuery.GetSingletonEntity();
            state.EntityManager.SetComponentData(layoutDoneEntity, new DistrictLayoutDoneTag(districtCount, connectionCount));

            // Dispose allocated native arrays
            districtEntitiesArray.Dispose();
            districtPositionsArray.Dispose();
            districtNodeIdsArray.Dispose();
        }

        /// <summary>
        /// Build connection graph using K-nearest neighbors plus random long edges
        /// </summary>
        [BurstCompile]
        private static int BuildConnectionGraph(
            EntityManager entityManager,
            NativeArray<Entity> districtEntities,
            NativeArray<int2> districtPositions,
            NativeArray<uint> districtNodeIds,
            ref Unity.Mathematics.Random random)
        {
            int connectionCount = 0;
            int k = math.min(3, districtPositions.Length - 1); // K-nearest neighbors (max 3)

            // For each district, connect to K nearest neighbors
            for (int i = 0; i < districtPositions.Length; i++)
            {
                var sourcePos = districtPositions[i];
                var sourceNodeId = districtNodeIds[i];
                var sourceEntity = districtEntities[i];

                // Find K nearest neighbors
                var distances = new NativeArray<DistanceEntry>(districtPositions.Length - 1, Allocator.Temp);
                int entryIndex = 0;

                for (int j = 0; j < districtPositions.Length; j++)
                {
                    if (i == j) continue; // Skip self

                    var targetPos = districtPositions[j];
                    var distance = math.length(new float2(targetPos - sourcePos));
                    distances[entryIndex] = new DistanceEntry
                    {
                        Index = j,
                        Distance = distance
                    };
                    entryIndex++;
                }

                // Sort by distance (simple bubble sort for small arrays)
                SortDistanceEntries(distances);

                // Connect to K nearest neighbors
                var connectionBuffer = entityManager.GetBuffer<ConnectionBufferElement>(sourceEntity);
                for (int k_idx = 0; k_idx < math.min(k, distances.Length); k_idx++)
                {
                    var targetIndex = distances[k_idx].Index;
                    var targetNodeId = districtNodeIds[targetIndex];

                    // Check if connection already exists (avoid duplicates)
                    bool connectionExists = false;
                    for (int c = 0; c < connectionBuffer.Length; c++)
                    {
                        if (connectionBuffer[c].Value.ToNodeId == targetNodeId)
                        {
                            connectionExists = true;
                            break;
                        }
                    }

                    if (!connectionExists)
                    {
                        var connection = new Connection(
                            sourceNodeId,
                            targetNodeId,
                            ConnectionType.Bidirectional,
                            Polarity.None,
                            distances[k_idx].Distance * 0.1f // Scale down traversal cost
                        );

                        connectionBuffer.Add(new ConnectionBufferElement { Value = connection });
                        connectionCount++;
                    }
                }

                distances.Dispose();
            }

            // Add random long edges for loops (1 per 3 districts, minimum 1)
            int longEdgeCount = math.max(1, districtPositions.Length / 3);
            for (int i = 0; i < longEdgeCount; i++)
            {
                int sourceIdx = random.NextInt(0, districtPositions.Length);
                int targetIdx = random.NextInt(0, districtPositions.Length);

                // Ensure different districts
                if (sourceIdx == targetIdx)
                {
                    targetIdx = (targetIdx + 1) % districtPositions.Length;
                }

                var sourceEntity = districtEntities[sourceIdx];
                var sourceNodeId = districtNodeIds[sourceIdx];
                var targetNodeId = districtNodeIds[targetIdx];

                // Check if long edge connection already exists
                var connectionBuffer = entityManager.GetBuffer<ConnectionBufferElement>(sourceEntity);
                bool connectionExists = false;
                for (int c = 0; c < connectionBuffer.Length; c++)
                {
                    if (connectionBuffer[c].Value.ToNodeId == targetNodeId)
                    {
                        connectionExists = true;
                        break;
                    }
                }

                if (!connectionExists)
                {
                    var distance = math.length(new float2(districtPositions[targetIdx] - districtPositions[sourceIdx]));
                    var connection = new Connection(
                        sourceNodeId,
                        targetNodeId,
                        ConnectionType.Bidirectional,
                        Polarity.None,
                        distance * 0.15f // Slightly higher cost for long edges
                    );

                    connectionBuffer.Add(new ConnectionBufferElement { Value = connection });
                    connectionCount++;
                }
            }

            return connectionCount;
        }

        /// <summary>
        /// Simple bubble sort for distance entries (suitable for small arrays)
        /// </summary>
        [BurstCompile]
        static void SortDistanceEntries(NativeArray<DistanceEntry> distances)
        {
            for (int i = 0; i < distances.Length - 1; i++)
            {
                for (int j = 0; j < distances.Length - i - 1; j++)
                {
                    if (distances[j].Distance > distances[j + 1].Distance)
                    {
                        (distances[j], distances[j + 1]) = (distances[j + 1], distances[j]);
                    }
                }
            }
        }

        /// <summary>
        /// Helper struct for distance calculations
        /// </summary>
        public struct DistanceEntry
        {
            public int Index;
            public float Distance;
        }
    }
}
