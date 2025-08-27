using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// System responsible for building the navigation graph from baked district, connection, and gate data
    /// Converts authoring components into runtime navigation nodes and links
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(BuildConnectionBuffersSystem))]
    public partial struct NavigationGraphBuildSystem : ISystem
    {
        private EntityQuery _districtQuery;
        private EntityQuery _connectionQuery;
        private EntityQuery _gateQuery;
        private EntityQuery _navigationGraphQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Query for districts that can become navigation nodes
            var builder = SystemAPI.QueryBuilder()
#if UNITY_TRANSFORMS_LOCALTRANSFORM
                .WithAll<LocalTransform>()
#elif UNITY_TRANSFORMS_TRANSLATION
                .WithAll<Unity.Transforms.Translation>()
#endif
                .WithAll<NodeId>()
                .WithNone<NavNode>(); // Only process districts that haven't been converted yet

            _districtQuery = builder.Build();

            // Query for connections between districts
            _connectionQuery = SystemAPI.QueryBuilder()
                .WithAll<Connection>()
                .Build();

            // Query for gate conditions
            _gateQuery = SystemAPI.QueryBuilder()
                .WithAll<GateConditionBufferElement>()
                .WithAll<NodeId>()
                .Build();

            // Query for navigation graph singleton
            _navigationGraphQuery = SystemAPI.QueryBuilder()
                .WithAll<NavigationGraph>()
                .Build();

            state.RequireForUpdate(_districtQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Create the singleton only when missing
            if (_navigationGraphQuery.IsEmpty)
            {
                var created = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(created, new NavigationGraph
                {
                    NodeCount = 0,
                    LinkCount = 0,
                    IsReady = false,
                    LastRebuildTime = SystemAPI.Time.ElapsedTime
                });
            }

            if (_districtQuery.IsEmpty)
                return;

            var navGraph = SystemAPI.GetSingleton<NavigationGraph>();

            var nodeCount = BuildNavigationNodes(ref state);
            var linkCount = BuildNavigationLinks(ref state);

            navGraph.NodeCount = nodeCount;
            navGraph.LinkCount = linkCount;
            navGraph.IsReady = true;
            navGraph.LastRebuildTime = SystemAPI.Time.ElapsedTime;

            SystemAPI.SetSingleton(navGraph);
        }

        [BurstCompile]
        private readonly int BuildNavigationNodes(ref SystemState state)
        {
            var nodeCount = 0;

            foreach (var (nodeId, entity) in
                     SystemAPI.Query<RefRO<NodeId>>()
                         .WithEntityAccess()
                         .WithNone<NavNode>())
            {
                var districtNodeId = nodeId.ValueRO.Value;
                float3 worldPosition = float3.zero;

#if UNITY_TRANSFORMS_LOCALTRANSFORM
                if (SystemAPI.HasComponent<LocalTransform>(entity))
                {
                    worldPosition = SystemAPI.GetComponent<LocalTransform>(entity).Position;
                }
#elif UNITY_TRANSFORMS_TRANSLATION
                if (SystemAPI.HasComponent<Unity.Transforms.Translation>(entity))
                {
                    worldPosition = SystemAPI.GetComponent<Unity.Transforms.Translation>(entity).Value;
                }
#endif
                var biomeType = BiomeType.Unknown;
                var primaryPolarity = Polarity.None;

                if (SystemAPI.HasComponent<TinyWalnutGames.MetVD.Core.Biome>(entity))
                {
                    var biome = SystemAPI.GetComponent<TinyWalnutGames.MetVD.Core.Biome>(entity);
                    biomeType = biome.Type;
                    primaryPolarity = biome.PrimaryPolarity;
                }

                var navNode = new NavNode(districtNodeId, worldPosition, biomeType, primaryPolarity);
                state.EntityManager.AddComponentData(entity, navNode);

                if (!SystemAPI.HasBuffer<NavLinkBufferElement>(entity))
                {
                    state.EntityManager.AddBuffer<NavLinkBufferElement>(entity);
                }

                nodeCount++;
            }

            return nodeCount;
        }

        [BurstCompile]
        private readonly int BuildNavigationLinks(ref SystemState state)
        {
            var linkCount = 0;

            foreach (var (connection, connectionEntity) in
                     SystemAPI.Query<RefRO<Connection>>()
                         .WithEntityAccess())
            {
                var conn = connection.ValueRO;

                // Meaningful use of connection entity: skip inactive connections (ensures correct gating)
                if (!conn.IsActive)
                    continue;

                var sourceEntity = FindEntityByStateNodeId(ref state, conn.FromNodeId);
                var destEntity = FindEntityByStateNodeId(ref state, conn.ToNodeId);

                if (sourceEntity == Entity.Null || destEntity == Entity.Null)
                    continue;

                var gateConditions = CollectGateConditions(ref state, sourceEntity, destEntity);
                var navLink = CreateNavLinkFromConnection(conn, gateConditions);

                if (SystemAPI.HasBuffer<NavLinkBufferElement>(sourceEntity))
                {
                    var linkBuffer = SystemAPI.GetBuffer<NavLinkBufferElement>(sourceEntity);
                    linkBuffer.Add(navLink);
                    linkCount++;
                }

                if (conn.Type == ConnectionType.Bidirectional)
                {
                    var reverseLink = navLink;
                    reverseLink.FromNodeId = conn.ToNodeId;
                    reverseLink.ToNodeId = conn.FromNodeId;

                    if (SystemAPI.HasBuffer<NavLinkBufferElement>(destEntity))
                    {
                        var reverseLinkBuffer = SystemAPI.GetBuffer<NavLinkBufferElement>(destEntity);
                        reverseLinkBuffer.Add(reverseLink);
                        linkCount++;
                    }
                }

                // Optional: mark connection as discovered after link generation if not already
                if (!conn.IsDiscovered)
                {
                    var updated = conn;
                    updated.IsDiscovered = true;
                    state.EntityManager.SetComponentData(connectionEntity, updated);
                }
            }

            return linkCount;
        }

        [BurstCompile]
        private readonly Entity FindEntityByStateNodeId(ref SystemState state, uint nodeId)
        {
            // Meaningful use of state: build a filtered query once per call
            var query = state.GetEntityQuery(ComponentType.ReadOnly<NodeId>());
            using var ids = query.ToComponentDataArray<NodeId>(Allocator.Temp);
            using var entities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i].Value == nodeId)
                    return entities[i];
            }
            return Entity.Null;
        }

        [BurstCompile]
        private readonly GateConditionCollection CollectGateConditions(ref SystemState state, Entity sourceEntity, Entity destEntity)
        {
            // Use state for performant lookups (avoids IDE0060)
            var gateLookup = state.GetBufferLookup<GateConditionBufferElement>(true);
            var gateConditions = new GateConditionCollection();

            if (gateLookup.HasBuffer(sourceEntity))
            {
                var sourceGates = gateLookup[sourceEntity];
                for (int i = 0; i < sourceGates.Length && i < 4; i++)
                {
                    gateConditions.Add(sourceGates[i].Value);
                }
            }

            if (gateLookup.HasBuffer(destEntity))
            {
                var destGates = gateLookup[destEntity];
                for (int i = 0; i < destGates.Length && i < (4 - gateConditions.Count); i++)
                {
                    gateConditions.Add(destGates[i].Value);
                }
            }

            return gateConditions;
        }

        [BurstCompile]
        private readonly NavLink CreateNavLinkFromConnection(Connection connection, GateConditionCollection gates)
        {
            var combinedPolarity = Polarity.None;
            var combinedAbilities = Ability.None;
            var strictestSoftness = GateSoftness.Trivial;
            var maxTraversalCost = connection.TraversalCost;

            for (int i = 0; i < gates.Count; i++)
            {
                var gate = gates[i];
                combinedPolarity |= gate.RequiredPolarity;
                combinedAbilities |= gate.RequiredAbilities;

                if (gate.Softness < strictestSoftness)
                    strictestSoftness = gate.Softness;

                var gateCostMultiplier = (int)gate.Softness switch
                {
                    0 => 5.0f,  // Hard
                    1 => 4.0f,  // VeryDifficult
                    2 => 3.0f,  // Difficult
                    3 => 2.0f,  // Moderate
                    4 => 1.5f,  // Easy
                    5 => 1.1f,  // Trivial
                    _ => 1.0f
                };
                maxTraversalCost = math.max(maxTraversalCost, connection.TraversalCost * gateCostMultiplier);
            }

            var effectivePolarity = combinedPolarity != Polarity.None ? combinedPolarity : connection.RequiredPolarity;

            return new NavLink(
                connection.FromNodeId,
                connection.ToNodeId,
                connection.Type,
                effectivePolarity,
                combinedAbilities,
                maxTraversalCost,
                5.0f,
                strictestSoftness,
                $"Link_{connection.FromNodeId}_{connection.ToNodeId}"
            );
        }

        private struct GateConditionCollection
        {
            private GateCondition _gate0;
            private GateCondition _gate1;
            private GateCondition _gate2;
            private GateCondition _gate3;
            private int _count;

            public readonly int Count => _count;

            public void Add(GateCondition gate)
            {
                switch (_count)
                {
                    case 0: _gate0 = gate; break;
                    case 1: _gate1 = gate; break;
                    case 2: _gate2 = gate; break;
                    case 3: _gate3 = gate; break;
                }
                if (_count < 4) _count++;
            }

            public readonly GateCondition this[int index] => index switch
            {
                0 => _gate0,
                1 => _gate1,
                2 => _gate2,
                3 => _gate3,
                _ => default
            };
        }
    }
}
