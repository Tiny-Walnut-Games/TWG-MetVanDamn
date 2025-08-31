using Unity.Collections;
using Unity.Entities;

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

		public void OnCreate (ref SystemState state)
			{
			this._edgeQuery = state.GetEntityQuery(ComponentType.ReadOnly<ConnectionEdge>());
			this._doneQuery = state.GetEntityQuery(ComponentType.ReadOnly<ConnectionGraphBuiltTag>());
			state.RequireForUpdate(this._edgeQuery);
			}

		public void OnUpdate (ref SystemState state)
			{
			if (this._edgeQuery.IsEmpty || !this._doneQuery.IsEmptyIgnoreFilter)
				{
				return; // nothing to do OR already built
				}

			NativeArray<ConnectionEdge> edges = this._edgeQuery.ToComponentDataArray<ConnectionEdge>(Allocator.Temp); // small one-shot copy
			EntityManager em = state.EntityManager;

			for (int i = 0; i < edges.Length; i++)
				{
				ConnectionEdge edge = edges [ i ];

				// Resolve NodeId components (optional safety: skip if missing)
				if (!em.HasComponent<NodeId>(edge.From) || !em.HasComponent<NodeId>(edge.To))
					{
					continue; // skip if NodeId missing
					}

				NodeId fromNode = em.GetComponentData<NodeId>(edge.From);
				NodeId toNode = em.GetComponentData<NodeId>(edge.To);

				// Ensure buffers exist
				if (!em.HasBuffer<ConnectionBufferElement>(edge.From))
					{
					em.AddBuffer<ConnectionBufferElement>(edge.From);
					}

				DynamicBuffer<ConnectionBufferElement> fromBuf = em.GetBuffer<ConnectionBufferElement>(edge.From);
				var forward = new Connection(fromNode._value, toNode._value, edge.Type, edge.RequiredPolarity, edge.TraversalCost);
				if (!Contains(fromBuf, forward))
					{
					fromBuf.Add(forward);
					}

				if (edge.Type == ConnectionType.Bidirectional)
					{
					if (!em.HasBuffer<ConnectionBufferElement>(edge.To))
						{
						em.AddBuffer<ConnectionBufferElement>(edge.To);
						}

					DynamicBuffer<ConnectionBufferElement> toBuf = em.GetBuffer<ConnectionBufferElement>(edge.To);
					var reverse = new Connection(toNode._value, fromNode._value, edge.Type, edge.RequiredPolarity, edge.TraversalCost);
					if (!Contains(toBuf, reverse))
						{
						toBuf.Add(reverse);
						}
					}
				}

			edges.Dispose();

			// Mark completion with a simple tag entity so we do not rebuild.
			Entity tagEntity = em.CreateEntity();
			em.AddComponent<ConnectionGraphBuiltTag>(tagEntity);
			}

		private static bool Contains (DynamicBuffer<ConnectionBufferElement> buf, in Connection c)
			{
			for (int i = 0; i < buf.Length; i++)
				{
				Connection existing = buf [ i ].Value;
				if (existing.FromNodeId == c.FromNodeId && existing.ToNodeId == c.ToNodeId && existing.Type == c.Type && existing.RequiredPolarity == c.RequiredPolarity)
					{
					return true;
					}
				}
			return false;
			}
		}

	public struct ConnectionGraphBuiltTag : IComponentData { }
	}
