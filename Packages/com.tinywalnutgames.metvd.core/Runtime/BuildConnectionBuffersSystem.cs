using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Core
{
    /// <summary>
    /// One-shot system that converts baked ConnectionEdge components into
    /// per-node ConnectionBufferElement buffers on district entities.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BuildConnectionBuffersSystem : ISystem
    {
        private EntityQuery _edgeQuery;
        private EntityQuery _doneQuery;

        public void OnCreate(ref SystemState state)
        {
            _edgeQuery = state.GetEntityQuery(ComponentType.ReadOnly<ConnectionEdge>());
            _doneQuery = state.GetEntityQuery(ComponentType.ReadOnly<ConnectionGraphBuiltTag>());
            state.RequireForUpdate(_edgeQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_edgeQuery.IsEmpty || !_doneQuery.IsEmptyIgnoreFilter)
                return; // nothing to do OR already built

            var edges = _edgeQuery.ToComponentDataArray<ConnectionEdge>(Allocator.Temp); // small one-shot copy
            var em = state.EntityManager;

            for (int i = 0; i < edges.Length; i++)
            {
                var edge = edges[i];

                // Resolve NodeId components (optional safety: skip if missing)
                if (!em.HasComponent<NodeId>(edge.From) || !em.HasComponent<NodeId>(edge.To))
                    continue; // skip if NodeId missing

                var fromNode = em.GetComponentData<NodeId>(edge.From);
                var toNode = em.GetComponentData<NodeId>(edge.To);

                // Ensure buffers exist
                if (!em.HasBuffer<ConnectionBufferElement>(edge.From))
                    em.AddBuffer<ConnectionBufferElement>(edge.From);
                var fromBuf = em.GetBuffer<ConnectionBufferElement>(edge.From);
                var forward = new Connection(fromNode.Value, toNode.Value, edge.Type, edge.RequiredPolarity, edge.TraversalCost);
                if (!Contains(fromBuf, forward))
                    fromBuf.Add(forward);

                if (edge.Type == ConnectionType.Bidirectional)
                {
                    if (!em.HasBuffer<ConnectionBufferElement>(edge.To))
                        em.AddBuffer<ConnectionBufferElement>(edge.To);
                    var toBuf = em.GetBuffer<ConnectionBufferElement>(edge.To);
                    var reverse = new Connection(toNode.Value, fromNode.Value, edge.Type, edge.RequiredPolarity, edge.TraversalCost);
                    if (!Contains(toBuf, reverse))
                        toBuf.Add(reverse);
                }
            }

            edges.Dispose();

            // Mark completion with a simple tag entity so we do not rebuild.
            var tagEntity = em.CreateEntity();
            em.AddComponent<ConnectionGraphBuiltTag>(tagEntity);
        }

        private static bool Contains(DynamicBuffer<ConnectionBufferElement> buf, in Connection c)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                var existing = buf[i].Value;
                if (existing.FromNodeId == c.FromNodeId && existing.ToNodeId == c.ToNodeId && existing.Type == c.Type && existing.RequiredPolarity == c.RequiredPolarity)
                    return true;
            }
            return false;
        }
    }

    public struct ConnectionGraphBuiltTag : IComponentData { }
}
