using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
#if UNITY_TRANSFORMS_LOCALTRANSFORM
using Unity.Transforms;
#endif
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
#if UNITY_TRANSFORMS_LOCALTRANSFORM
                .WithAll<LocalTransform>()
#endif
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
                var createdGraphEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(createdGraphEntity, new NavigationGraph());
            }

            // Skip if no districts to process
            if (_districtQuery.IsEmpty)
                return;

            var graphEntity = SystemAPI.GetSingletonEntity<NavigationGraph>();
            var graphData = SystemAPI.GetSingleton<NavigationGraph>();

            // Build navigation nodes from districts
            int nodeCount = BuildNavigationNodes(ref state);
            
            // Build navigation links from connections and gates
            int linkCount = BuildNavigationLinks(ref state);

            // Update navigation graph statistics
            graphData.NodeCount = nodeCount;
            graphData.LinkCount = linkCount;
            graphData.IsReady = true;
            graphData.LastRebuildTime = SystemAPI.Time.ElapsedTime;
            
            SystemAPI.SetSingleton(graphData);
        }

        [BurstCompile]
        private int BuildNavigationNodes(ref SystemState state)
        {
            int count = 0;
#if UNITY_TRANSFORMS_LOCALTRANSFORM
            // Convert districts to navigation nodes
            foreach (var (transform, nodeId, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<NodeId>>().WithEntityAccess().WithNone<NavNode>())
            {
                var pos = transform.ValueRO.Position;
                var id = nodeId.ValueRO.Value;

                // Determine biome type and polarity from existing components
                var biomeType = BiomeType.Unknown;
                var primary = Polarity.None;
                
                if (SystemAPI.HasComponent<TinyWalnutGames.MetVD.Core.Biome>(entity))
                {
                    var biome = SystemAPI.GetComponent<TinyWalnutGames.MetVD.Core.Biome>(entity);
                    biomeType = biome.Type;
                    primary = biome.PrimaryPolarity;
                }

                // Create navigation node
                state.EntityManager.AddComponentData(entity, new NavNode(id, pos, biomeType, primary));

                // Add navigation link buffer for outgoing connections
                if (!SystemAPI.HasBuffer<NavLinkBufferElement>(entity))
                {
                    state.EntityManager.AddBuffer<NavLinkBufferElement>(entity);
                }

                count++;
            }
#endif
            return count;
        }

        [BurstCompile]
        private int BuildNavigationLinks(ref SystemState state)
        {
            int count = 0;
            // Process all connections to create navigation links
            foreach (var (connection, entity) in SystemAPI.Query<RefRO<Connection>>().WithEntityAccess())
            {
                var conn = connection.ValueRO;
                
                // Find source and destination entities
                var source = FindEntityByNodeId(ref state, conn.FromNodeId);
                var dest = FindEntityByNodeId(ref state, conn.ToNodeId);
                
                if (source == Entity.Null || dest == Entity.Null)
                    continue;

                // Check for gate conditions on source or destination
                var gates = CollectGateConditions(ref state, source, dest);
                
                // Create navigation link with gate conditions
                var link = CreateNavLinkFromConnection(conn, gates);
                
                // Add link to source entity's buffer
                if (SystemAPI.HasBuffer<NavLinkBufferElement>(source))
                {
                    var buf = SystemAPI.GetBuffer<NavLinkBufferElement>(source);
                    buf.Add(link);
                    count++;
                }

                // For bidirectional connections, add reverse link
                if (conn.Type == ConnectionType.Bidirectional && SystemAPI.HasBuffer<NavLinkBufferElement>(dest))
                {
                    var rev = link; rev.FromNodeId = conn.ToNodeId; rev.ToNodeId = conn.FromNodeId; SystemAPI.GetBuffer<NavLinkBufferElement>(dest).Add(rev); count++; }
            }
            return count;
        }

        [BurstCompile]
        private Entity FindEntityByNodeId(ref SystemState state, uint nodeId)
        {
            foreach (var (id, entity) in SystemAPI.Query<RefRO<NodeId>>().WithEntityAccess())
                if (id.ValueRO.Value == nodeId) return entity;
            return Entity.Null;
        }

        [BurstCompile]
        private GateConditionCollection CollectGateConditions(ref SystemState state, Entity source, Entity dest)
        {
            var result = new GateConditionCollection();
            // Collect gate conditions from source entity
            if (SystemAPI.HasBuffer<GateConditionBufferElement>(source))
            {
                var buf = SystemAPI.GetBuffer<GateConditionBufferElement>(source);
                for (int i = 0; i < buf.Length && i < 4; i++) result.Add(buf[i].Value);
            }
            
            // Collect gate conditions from destination entity
            if (SystemAPI.HasBuffer<GateConditionBufferElement>(dest))
            {
                var buf = SystemAPI.GetBuffer<GateConditionBufferElement>(dest);
                for (int i = 0; i < buf.Length && i < (4 - result.Count); i++) result.Add(buf[i].Value);
            }

            return result;
        }

        [BurstCompile]
        private NavLink CreateNavLinkFromConnection(Connection connection, GateConditionCollection gates)
        {
            // Determine combined requirements from all gate conditions
            var combinedPolarity = Polarity.None;
            var combinedAbilities = Ability.None;
            var strictest = GateSoftness.Trivial;
            float maxCost = connection.TraversalCost;

            for (int i = 0; i < gates.Count; i++)
            {
                var g = gates[i]; combinedPolarity |= g.RequiredPolarity; combinedAbilities |= g.RequiredAbilities; if (g.Softness < strictest) strictest = g.Softness; float mult = (int)g.Softness switch { 0 => 5f, 1 => 4f, 2 => 3f, 3 => 2f, 4 => 1.5f, 5 => 1.1f, _ => 1f }; maxCost = math.max(maxCost, connection.TraversalCost * mult);
            }
            // Override connection polarity with gate requirements if more restrictive
            var effPolarity = combinedPolarity != Polarity.None ? combinedPolarity : connection.RequiredPolarity;

            return new NavLink(connection.FromNodeId, connection.ToNodeId, connection.Type, effPolarity, combinedAbilities, maxCost, 5f, strictest, $"Link_{connection.FromNodeId}_{connection.ToNodeId}");
        }

        /// <summary>
        /// Helper struct for collecting gate conditions with fixed size
        /// </summary>
        private struct GateConditionCollection
        {
            private GateCondition _g0, _g1, _g2, _g3; private int _count; public int Count => _count;
            public void Add(GateCondition g) { switch (_count) { case 0: _g0 = g; break; case 1: _g1 = g; break; case 2: _g2 = g; break; case 3: _g3 = g; break; } if (_count < 4) _count++; }
            public GateCondition this[int i] => i switch { 0 => _g0, 1 => _g1, 2 => _g2, 3 => _g3, _ => default };
        }
    }
}
