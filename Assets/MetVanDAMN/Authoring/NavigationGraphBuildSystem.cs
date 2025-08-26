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
            _districtQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform>()
                .WithAll<NodeId>()
                .WithNone<NavNode>() // Only process districts that haven't been converted yet
                .Build();

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
            // Create navigation graph singleton if it doesn't exist
            if (_navigationGraphQuery.IsEmpty)
            {
                var navGraphEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(navGraphEntity, new NavigationGraph());
            }

            // Skip if no districts to process
            if (_districtQuery.IsEmpty)
                return;

            var navGraphEntity = SystemAPI.GetSingletonEntity<NavigationGraph>();
            var navGraph = SystemAPI.GetSingleton<NavigationGraph>();

            // Build navigation nodes from districts
            var nodeCount = BuildNavigationNodes(ref state);
            
            // Build navigation links from connections and gates
            var linkCount = BuildNavigationLinks(ref state);

            // Update navigation graph statistics
            navGraph.NodeCount = nodeCount;
            navGraph.LinkCount = linkCount;
            navGraph.IsReady = true;
            navGraph.LastRebuildTime = SystemAPI.Time.ElapsedTime;
            
            SystemAPI.SetSingleton(navGraph);
        }

        [BurstCompile]
        private int BuildNavigationNodes(ref SystemState state)
        {
            var nodeCount = 0;

            // Convert districts to navigation nodes
            foreach (var (transform, nodeId, entity) in 
                     SystemAPI.Query<RefRO<LocalTransform>, RefRO<NodeId>>()
                     .WithEntityAccess()
                     .WithNone<NavNode>())
            {
                var worldPosition = transform.ValueRO.Position;
                var districtNodeId = nodeId.ValueRO.Value;

                // Determine biome type and polarity from existing components
                var biomeType = BiomeType.Unknown;
                var primaryPolarity = Polarity.None;
                
                if (SystemAPI.HasComponent<TinyWalnutGames.MetVD.Core.Biome>(entity))
                {
                    var biome = SystemAPI.GetComponent<TinyWalnutGames.MetVD.Core.Biome>(entity);
                    biomeType = biome.Type;
                    primaryPolarity = biome.PrimaryPolarity;
                }

                // Create navigation node
                var navNode = new NavNode(districtNodeId, worldPosition, biomeType, primaryPolarity);
                state.EntityManager.AddComponentData(entity, navNode);

                // Add navigation link buffer for outgoing connections
                if (!SystemAPI.HasBuffer<NavLinkBufferElement>(entity))
                {
                    state.EntityManager.AddBuffer<NavLinkBufferElement>(entity);
                }

                nodeCount++;
            }

            return nodeCount;
        }

        [BurstCompile]
        private int BuildNavigationLinks(ref SystemState state)
        {
            var linkCount = 0;

            // Process all connections to create navigation links
            foreach (var (connection, entity) in 
                     SystemAPI.Query<RefRO<Connection>>()
                     .WithEntityAccess())
            {
                var conn = connection.ValueRO;
                
                // Find source and destination entities
                var sourceEntity = FindEntityByNodeId(ref state, conn.FromNodeId);
                var destEntity = FindEntityByNodeId(ref state, conn.ToNodeId);
                
                if (sourceEntity == Entity.Null || destEntity == Entity.Null)
                    continue;

                // Check for gate conditions on source or destination
                var gateConditions = CollectGateConditions(ref state, sourceEntity, destEntity);
                
                // Create navigation link with gate conditions
                var navLink = CreateNavLinkFromConnection(conn, gateConditions);
                
                // Add link to source entity's buffer
                if (SystemAPI.HasBuffer<NavLinkBufferElement>(sourceEntity))
                {
                    var linkBuffer = SystemAPI.GetBuffer<NavLinkBufferElement>(sourceEntity);
                    linkBuffer.Add(navLink);
                    linkCount++;
                }

                // For bidirectional connections, add reverse link
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
            }

            return linkCount;
        }

        [BurstCompile]
        private Entity FindEntityByNodeId(ref SystemState state, uint nodeId)
        {
            foreach (var (id, entity) in SystemAPI.Query<RefRO<NodeId>>().WithEntityAccess())
            {
                if (id.ValueRO.Value == nodeId)
                    return entity;
            }
            return Entity.Null;
        }

        [BurstCompile]
        private GateConditionCollection CollectGateConditions(ref SystemState state, Entity sourceEntity, Entity destEntity)
        {
            var gateConditions = new GateConditionCollection();
            
            // Collect gate conditions from source entity
            if (SystemAPI.HasBuffer<GateConditionBufferElement>(sourceEntity))
            {
                var sourceGates = SystemAPI.GetBuffer<GateConditionBufferElement>(sourceEntity);
                for (int i = 0; i < sourceGates.Length && i < 4; i++) // Limit to 4 conditions
                {
                    gateConditions.Add(sourceGates[i].Value);
                }
            }
            
            // Collect gate conditions from destination entity
            if (SystemAPI.HasBuffer<GateConditionBufferElement>(destEntity))
            {
                var destGates = SystemAPI.GetBuffer<GateConditionBufferElement>(destEntity);
                for (int i = 0; i < destGates.Length && i < (4 - gateConditions.Count); i++)
                {
                    gateConditions.Add(destGates[i].Value);
                }
            }

            return gateConditions;
        }

        [BurstCompile]
        private NavLink CreateNavLinkFromConnection(Connection connection, GateConditionCollection gates)
        {
            // Determine combined requirements from all gate conditions
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
                    
                // Increase cost for stricter gates
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

            // Override connection polarity with gate requirements if more restrictive
            var effectivePolarity = combinedPolarity != Polarity.None ? combinedPolarity : connection.RequiredPolarity;

            return new NavLink(
                connection.FromNodeId,
                connection.ToNodeId,
                connection.Type,
                effectivePolarity,
                combinedAbilities,
                maxTraversalCost,
                5.0f, // Default polarity mismatch cost multiplier
                strictestSoftness,
                $"Link_{connection.FromNodeId}_{connection.ToNodeId}"
            );
        }

        /// <summary>
        /// Helper struct for collecting gate conditions with fixed size
        /// </summary>
        private struct GateConditionCollection
        {
            private GateCondition _gate0;
            private GateCondition _gate1;
            private GateCondition _gate2;
            private GateCondition _gate3;
            private int _count;

            public int Count => _count;

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

            public GateCondition this[int index]
            {
                get
                {
                    return index switch
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
    }
}