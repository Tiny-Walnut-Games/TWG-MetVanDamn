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

        public void OnCreate(ref SystemState state)
        {
            _layoutDoneQuery = state.GetEntityQuery(ComponentType.ReadWrite<DistrictLayoutDoneTag>());
            _districtsQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<NodeId>(),
                ComponentType.ReadWrite<ConnectionBufferElement>());

            state.RequireForUpdate(_layoutDoneQuery);
            state.RequireForUpdate(_districtsQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Need the layout completion tag
            var layoutDoneArray = _layoutDoneQuery.ToComponentDataArray<DistrictLayoutDoneTag>(Allocator.Temp);
            if (layoutDoneArray.Length == 0)
            {
                layoutDoneArray.Dispose();
                return;
            }
            var layoutDone = layoutDoneArray[0];
            layoutDoneArray.Dispose();

            // Already built
            if (layoutDone.ConnectionCount > 0)
                return;

            // Gather districts
            var entities = _districtsQuery.ToEntityArray(Allocator.Temp);
            var nodeIds  = _districtsQuery.ToComponentDataArray<NodeId>(Allocator.Temp);

            try
            {
                // Count level-0 districts
                var districtCount = 0;
                for (int i = 0; i < nodeIds.Length; i++)
                {
                    if (nodeIds[i].Level == 0) districtCount++;
                }

                if (districtCount < 2)
                    return; // nothing to connect

                // Allocate working arrays (not using 'using var' to avoid CS1654 mutation restriction)
                var districtEntities   = new NativeArray<Entity>(districtCount, Allocator.Temp);
                var districtPositions  = new NativeArray<int2>(districtCount, Allocator.Temp);
                var districtNodeIds    = new NativeArray<uint>(districtCount, Allocator.Temp);

                try
                {
                    int districtIndex = 0;
                    for (int i = 0; i < nodeIds.Length; i++)
                    {
                        if (nodeIds[i].Level == 0)
                        {
                            districtEntities[districtIndex]  = entities[i];
                            districtPositions[districtIndex] = nodeIds[i].Coordinates;
                            districtNodeIds[districtIndex]   = nodeIds[i].Value;
                            districtIndex++;
                        }
                    }

                    // World seed for deterministic randomness
                    var worldConfigQuery = state.GetEntityQuery(ComponentType.ReadOnly<WorldConfiguration>());
                    var worldConfig = worldConfigQuery.GetSingleton<WorldConfiguration>();
                    var random = new Unity.Mathematics.Random((uint)(worldConfig.Seed + 1337));

                    // Build graph
                    var connectionCount = BuildConnectionGraph(
                        state.EntityManager,
                        districtEntities,
                        districtPositions,
                        districtNodeIds,
                        ref random);

                    // Update tag
                    var layoutDoneEntity = _layoutDoneQuery.GetSingletonEntity();
                    state.EntityManager.SetComponentData(layoutDoneEntity, new DistrictLayoutDoneTag(districtCount, connectionCount));
                }
                finally
                {
                    if (districtEntities.IsCreated)  districtEntities.Dispose();
                    if (districtPositions.IsCreated) districtPositions.Dispose();
                    if (districtNodeIds.IsCreated)   districtNodeIds.Dispose();
                }
            }
            finally
            {
                entities.Dispose();
                nodeIds.Dispose();
            }
        }

        private static int BuildConnectionGraph(
            EntityManager entityManager,
            NativeArray<Entity> districtEntities,
            NativeArray<int2> districtPositions,
            NativeArray<uint> districtNodeIds,
            ref Unity.Mathematics.Random random)
        {
            int connectionCount = 0;
            int k = math.min(3, districtPositions.Length - 1);

            // K-nearest neighbors
            for (int i = 0; i < districtPositions.Length; i++)
            {
                var sourcePos   = districtPositions[i];
                var sourceNode  = districtNodeIds[i];
                var sourceEntity = districtEntities[i];

                var distances = new NativeArray<DistanceEntry>(districtPositions.Length - 1, Allocator.Temp);
                try
                {
                    int dIdx = 0;
                    for (int j = 0; j < districtPositions.Length; j++)
                    {
                        if (i == j) continue;
                        var targetPos = districtPositions[j];
                        var dist = math.length(new float2(targetPos - sourcePos));
                        distances[dIdx++] = new DistanceEntry { Index = j, Distance = dist };
                    }

                    SortDistanceEntries(distances);

                    var buffer = entityManager.GetBuffer<ConnectionBufferElement>(sourceEntity);
                    for (int n = 0; n < math.min(k, distances.Length); n++)
                    {
                        var targetIndex = distances[n].Index;
                        var targetNode  = districtNodeIds[targetIndex];

                        bool exists = false;
                        for (int c = 0; c < buffer.Length; c++)
                        {
                            if (buffer[c].Value.ToNodeId == targetNode)
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (!exists)
                        {
                            var connection = new Connection(
                                sourceNode,
                                targetNode,
                                ConnectionType.Bidirectional,
                                Polarity.None,
                                distances[n].Distance * 0.1f);

                            buffer.Add(new ConnectionBufferElement { Value = connection });
                            connectionCount++;
                        }
                    }
                }
                finally
                {
                    distances.Dispose();
                }
            }

            // Random long edges
            int longEdgeCount = math.max(1, districtPositions.Length / 3);
            for (int e = 0; e < longEdgeCount; e++)
            {
                int sourceIdx = random.NextInt(0, districtPositions.Length);
                int targetIdx = random.NextInt(0, districtPositions.Length);
                if (sourceIdx == targetIdx)
                    targetIdx = (targetIdx + 1) % districtPositions.Length;

                var srcEntity = districtEntities[sourceIdx];
                var srcNode   = districtNodeIds[sourceIdx];
                var tgtNode   = districtNodeIds[targetIdx];

                var buffer = entityManager.GetBuffer<ConnectionBufferElement>(srcEntity);
                bool exists = false;
                for (int c = 0; c < buffer.Length; c++)
                {
                    if (buffer[c].Value.ToNodeId == tgtNode)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    var distance = math.length(new float2(districtPositions[targetIdx] - districtPositions[sourceIdx]));
                    var connection = new Connection(
                        srcNode,
                        tgtNode,
                        ConnectionType.Bidirectional,
                        Polarity.None,
                        distance * 0.15f);

                    buffer.Add(new ConnectionBufferElement { Value = connection });
                    connectionCount++;
                }
            }

            return connectionCount;
        }

        private static void SortDistanceEntries(NativeArray<DistanceEntry> distances)
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

        public struct DistanceEntry
        {
            public int Index;
            public float Distance;
        }
    }
}
