using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using TinyWalnutGames.MetVD.Core;
#if !UNITY_TRANSFORMS_LOCALTRANSFORM
// Fallback alias so code compiles when the new LocalTransform package symbol is not defined.
// Provide a lightweight compatibility struct in Core.Compat namespace (mirrors pos only).
using LocalTransform = TinyWalnutGames.MetVD.Core.Compat.LocalTransformCompat;
#endif

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// Builds (or refreshes) the runtime NavigationGraph from authored districts, connections and gate conditions.
    /// - Converts district entities into NavNode components.
    /// - Generates NavLink buffer entries from Connection components (bidirectional support).
    /// - Aggregates GateConditionBufferElement (limited to max 4 per link) into link requirements.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(BuildConnectionBuffersSystem))]
    public partial struct NavigationGraphBuildSystem : ISystem
    {
        private EntityQuery _districtQuery;
        private EntityQuery _connectionQuery;
        private EntityQuery _gateConditionQuery;
        private EntityQuery _navigationGraphQuery;

        public void OnCreate(ref SystemState state)
        {
            // Districts that have not yet been converted to NavNodes
            _districtQuery = SystemAPI.QueryBuilder()
#if UNITY_TRANSFORMS_LOCALTRANSFORM
                .WithAll<LocalTransform>()
#endif
                .WithAll<NodeId>()
                .WithNone<NavNode>()
                .Build();

            // Raw connections (authored / generated)
            _connectionQuery = SystemAPI.QueryBuilder()
                .WithAll<Connection>()
                .Build();

            // Entities that carry gate conditions (optional)
            _gateConditionQuery = SystemAPI.QueryBuilder()
                .WithAll<GateConditionBufferElement, NodeId>()
                .Build();

            // NavigationGraph singleton
            _navigationGraphQuery = SystemAPI.QueryBuilder()
                .WithAll<NavigationGraph>()
                .Build();

            // We at least need districts (graph can exist early but will remain empty)
            state.RequireForUpdate(_districtQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            // Ensure singleton exists
            if (_navigationGraphQuery.IsEmpty)
            {
                var navGraphEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(navGraphEntity, new NavigationGraph());
            }

            // Nothing to process: return early (keeps system cheap while waiting for authoring bake)
            if (_districtQuery.IsEmpty && _connectionQuery.IsEmpty)
                return;

            // Build a temporary map (NodeId -> Entity) once per update for faster lookups than re-querying per connection
            var nodeLookup = BuildNodeLookup(ref state, out int nodeCountAdded);

            // Convert new districts into NavNodes
            var addedNodes = BuildNavigationNodes(ref state);

            // Build links
            var builtLinks = BuildNavigationLinks(ref state, nodeLookup);

            nodeLookup.Dispose();

            // Update / write back NavigationGraph statistics
            var navGraph = SystemAPI.GetSingleton<NavigationGraph>();
            navGraph.NodeCount += addedNodes;          // accumulate (in case system runs multiple times incrementally)
            navGraph.LinkCount += builtLinks;
            navGraph.IsReady = navGraph.NodeCount > 0;
            navGraph.LastRebuildTime = SystemAPI.Time.ElapsedTime;
            SystemAPI.SetSingleton(navGraph);
        }

        /// <summary>
        /// Builds a map from nodeId.Value -> Entity for fast O(1) connection resolution.
        /// Includes both already-converted nodes and newly pending districts (those without NavNode yet).
        /// </summary>
        private NativeParallelHashMap<uint, Entity> BuildNodeLookup(ref SystemState state, out int count)
        {
            // Count all NodeId holders (districts + already converted)
            var nodeIdQuery = SystemAPI.QueryBuilder()
                .WithAll<NodeId>()
                .Build();

            var entities = nodeIdQuery.ToEntityArray(Allocator.Temp);
            var nodeIds = nodeIdQuery.ToComponentDataArray<NodeId>(Allocator.Temp);

            var map = new NativeParallelHashMap<uint, Entity>(entities.Length, Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!map.TryAdd(nodeIds[i].Value, entities[i]))
                {
                    // If duplicate, last one wins – but log once (optional; keep Burst-friendly: no string concat inside loop)
#if UNITY_EDITOR
                    // Intentionally minimal; avoid heavy logging in production
                    // UnityEngine.Debug.LogWarning($"Duplicate NodeId detected: {nodeIds[i].Value}");
#endif
                }
            }

            count = entities.Length;
            entities.Dispose();
            nodeIds.Dispose();
            return map;
        }

        private int BuildNavigationNodes(ref SystemState state)
        {
            if (_districtQuery.IsEmpty)
                return 0;

            var entities = _districtQuery.ToEntityArray(Allocator.Temp);
#if UNITY_TRANSFORMS_LOCALTRANSFORM
            var transforms = _districtQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
#endif
            var nodeIds = _districtQuery.ToComponentDataArray<NodeId>(Allocator.Temp);

            var created = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
#if UNITY_TRANSFORMS_LOCALTRANSFORM
                float3 worldPosition = transforms[i].Position;
#else
                // Fallback: derive a position from grid Coordinates if transform package not active.
                var coords = nodeIds[i].Coordinates;
                float3 worldPosition = new float3(coords.x, 0, coords.y);
#endif
                var districtNodeId = nodeIds[i].Value;

                // Extract biome data if present
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

                created++;
            }

            entities.Dispose();
#if UNITY_TRANSFORMS_LOCALTRANSFORM
            transforms.Dispose();
#endif
            nodeIds.Dispose();
            return created;
        }

        private int BuildNavigationLinks(ref SystemState state, NativeParallelHashMap<uint, Entity> nodeLookup)
        {
            if (_connectionQuery.IsEmpty)
                return 0;

            var entities = _connectionQuery.ToEntityArray(Allocator.Temp);
            var connections = _connectionQuery.ToComponentDataArray<Connection>(Allocator.Temp);

            var linkCount = 0;

            for (int i = 0; i < entities.Length; i++)
            {
                var conn = connections[i];

                if (!nodeLookup.TryGetValue(conn.FromNodeId, out var sourceEntity) ||
                    !nodeLookup.TryGetValue(conn.ToNodeId, out var destEntity))
                    continue;

                // Collect gate conditions (limited)
                var gateConditions = CollectGateConditions(ref state, sourceEntity, destEntity);

                var navLink = CreateNavLinkFromConnection(conn, gateConditions);

                if (SystemAPI.HasBuffer<NavLinkBufferElement>(sourceEntity))
                {
                    var linkBuffer = SystemAPI.GetBuffer<NavLinkBufferElement>(sourceEntity);
                    linkBuffer.Add(navLink);
                    linkCount++;
                }

                if (conn.Type == ConnectionType.Bidirectional &&
                    SystemAPI.HasBuffer<NavLinkBufferElement>(destEntity))
                {
                    var reverse = navLink;
                    reverse.FromNodeId = conn.ToNodeId;
                    reverse.ToNodeId = conn.FromNodeId;
                    var reverseBuffer = SystemAPI.GetBuffer<NavLinkBufferElement>(destEntity);
                    reverseBuffer.Add(reverse);
                    linkCount++;
                }
            }

            entities.Dispose();
            connections.Dispose();
            return linkCount;
        }

        private GateConditionCollection CollectGateConditions(ref SystemState state, Entity sourceEntity, Entity destEntity)
        {
            var gateConditions = new GateConditionCollection();

            if (SystemAPI.HasBuffer<GateConditionBufferElement>(sourceEntity))
            {
                var buf = SystemAPI.GetBuffer<GateConditionBufferElement>(sourceEntity);
                for (int i = 0; i < buf.Length && gateConditions.Count < 4; i++)
                    gateConditions.Add(buf[i].Value);
            }

            if (SystemAPI.HasBuffer<GateConditionBufferElement>(destEntity) && gateConditions.Count < 4)
            {
                var buf = SystemAPI.GetBuffer<GateConditionBufferElement>(destEntity);
                for (int i = 0; i < buf.Length && gateConditions.Count < 4; i++)
                    gateConditions.Add(buf[i].Value);
            }

            return gateConditions;
        }

        [BurstCompile]
        private NavLink CreateNavLinkFromConnection(Connection connection, GateConditionCollection gates)
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

        /// <summary>
        /// Fixed-size (0..4) collection of gate conditions to avoid dynamic allocations in Burst.
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

            public GateCondition this[int index] =>
                index switch
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
