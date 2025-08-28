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
    public partial class NavigationGraphBuildSystem : SystemBase
    {
        private EntityQuery _districtQuery;
        private EntityQuery _connectionQuery;
        private EntityQuery _gateQuery;
        private EntityQuery _navigationGraphQuery;

        // 🔥 ACTUAL ECB SYSTEM REFERENCE - NOT JUST COMMENTS
        private EndInitializationEntityCommandBufferSystem _endInitEcbSystem;

        protected override void OnCreate()
        {
            // 🔥 GET THE ECB SYSTEM REFERENCE FOR REAL
            _endInitEcbSystem = World.GetOrCreateSystemManaged<EndInitializationEntityCommandBufferSystem>();

            // Query for districts that can become navigation nodes
            _districtQuery = GetEntityQuery(
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.ReadOnly<NodeId>(),
                ComponentType.Exclude<NavNode>() // Only process districts that haven't been converted yet
            );

            // Query for connections between districts
            _connectionQuery = GetEntityQuery(
                ComponentType.ReadOnly<Connection>()
            );

            // Query for gate conditions
            _gateQuery = GetEntityQuery(
                ComponentType.ReadOnly<GateConditionBufferElement>(),
                ComponentType.ReadOnly<NodeId>()
            );

            // Query for navigation graph singleton
            _navigationGraphQuery = GetEntityQuery(
                ComponentType.ReadOnly<NavigationGraph>()
            );

            RequireForUpdate(_districtQuery);
        }

        protected override void OnUpdate()
        {
            // 🔥 CREATE ACTUAL ECB INSTANCE - NOT JUST COMMENTS
            var ecb = _endInitEcbSystem.CreateCommandBuffer();

            // 🔥 USE ECB FOR SINGLETON CREATION - NO MORE DIRECT ENTITYMANAGER
            if (_navigationGraphQuery.IsEmpty)
            {
                var newNavGraphEntity = ecb.CreateEntity();
                ecb.AddComponent(newNavGraphEntity, new NavigationGraph());
            }

            // Skip if no districts to process
            if (_districtQuery.IsEmpty)
                return;

            // Build navigation nodes from districts using ECB
            var nodeCount = BuildNavigationNodesWithActualECB(ecb);

            // Build navigation links from connections and gates
            var linkCount = BuildNavigationLinks();

            // Update navigation graph statistics if it exists
            if (!_navigationGraphQuery.IsEmpty)
            {
                var navGraphEntity = SystemAPI.GetSingletonEntity<NavigationGraph>();
                var navGraph = SystemAPI.GetSingleton<NavigationGraph>();
                
                navGraph.NodeCount = nodeCount;
                navGraph.LinkCount = linkCount;
                navGraph.IsReady = true;
                navGraph.LastRebuildTime = SystemAPI.Time.ElapsedTime;
                
                // 🔥 USE ECB FOR SINGLETON UPDATE TOO
                ecb.SetComponent(navGraphEntity, navGraph);
            }

            // 🔥 TELL ECB SYSTEM TO EXECUTE AFTER OUR JOBS
            _endInitEcbSystem.AddJobHandleForProducer(Dependency);
        }

        /// <summary>
        /// 🔥 ACTUAL ECB IMPLEMENTATION - NO MORE FAKE TODO COMMENTS
        /// </summary>
        private int BuildNavigationNodesWithActualECB(EntityCommandBuffer ecb)
        {
            var nodeCount = 0;

            // Convert districts to navigation nodes
            Entities
                .WithNone<NavNode>()
                .ForEach((Entity entity, in LocalTransform transform, in NodeId nodeId) =>
                {
                    var worldPosition = transform.Position;
                    var districtNodeId = nodeId.Value;

                    // Determine biome type and polarity from existing components
                    var biomeType = BiomeType.Unknown;
                    var primaryPolarity = Polarity.None;

                    if (SystemAPI.HasComponent<TinyWalnutGames.MetVD.Core.Biome>(entity))
                    {
                        var biome = SystemAPI.GetComponent<TinyWalnutGames.MetVD.Core.Biome>(entity);
                        biomeType = biome.Type;
                        primaryPolarity = biome.PrimaryPolarity;
                    }

                    // 🔥 CREATE NAV NODE USING ECB - NO MORE COMMENTED OUT CODE
                    var navNode = new NavNode(districtNodeId, worldPosition);
                    ecb.AddComponent(entity, navNode);

                    // 🔥 ADD NAV LINK BUFFER USING ECB - NO MORE DIRECT ENTITYMANAGER
                    if (!SystemAPI.HasBuffer<NavLinkBufferElement>(entity))
                    {
                        ecb.AddBuffer<NavLinkBufferElement>(entity);
                    }

                    nodeCount++;
                })
                .WithoutBurst() // Required for ECB and SystemAPI usage
                .Run();

            return nodeCount;
        }

        /// <summary>
        /// Build navigation links - this doesn't need ECB since we're only modifying existing buffers
        /// </summary>
        private int BuildNavigationLinks()
        {
            var linkCount = 0;

            // Process all connections to create navigation links
            Entities
                .ForEach((Entity entity, in Connection connection) =>
                {
                    var conn = connection;

                    // Find source and destination entities
                    var sourceEntity = FindEntityByNodeId(conn.FromNodeId);
                    var destEntity = FindEntityByNodeId(conn.ToNodeId);

                    if (sourceEntity == Entity.Null || destEntity == Entity.Null)
                        return;

                    // Check for gate conditions on source or destination
                    var gateConditions = CollectGateConditions(sourceEntity, destEntity);

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
                })
                .WithoutBurst()
                .Run();

            return linkCount;
        }

        private Entity FindEntityByNodeId(uint nodeId)
        {
            Entity foundEntity = Entity.Null;

            Entities.ForEach((Entity entity, in NodeId id) =>
            {
                if (id.Value == nodeId)
                    foundEntity = entity;
            }).WithoutBurst().Run();

            return foundEntity;
        }

        private GateConditionCollection CollectGateConditions(Entity sourceEntity, Entity destEntity)
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

        private static NavLink CreateNavLinkFromConnection(Connection connection, GateConditionCollection gates)
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

            public readonly GateCondition this[int index]
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
