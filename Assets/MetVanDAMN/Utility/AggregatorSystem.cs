using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Utility
	{
	internal static class AggregatorDiagnostics<T> where T : unmanaged, IComponentData
		{
		public static int LastCount;
		}

	/// <summary>
	/// Generic ECS aggregator for collecting and reducing data across entities.
	/// Example uses: metrics, validation results, scoring, debug summaries.
	/// NOTE: Uses direct query extraction to avoid generic IJobEntity constraints.
	/// </summary>
	/// <typeparam name="T">The component type to aggregate (must be unmanaged).</typeparam>
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public partial struct AggregatorSystem<T> : ISystem where T : unmanaged, IComponentData
		{
		private NativeList<T> _results;
		private EntityQuery _query;

		[BurstCompile]
		public void OnCreate (ref SystemState state)
			{
			this._results = new NativeList<T>(Allocator.Persistent);
			// Build query for all entities with T (read-only extraction)
			this._query = state.GetEntityQuery(ComponentType.ReadOnly<T>());
			state.RequireForUpdate(this._query);
			AggregatorDiagnostics<T>.LastCount = 0;
			}

		[BurstCompile]
		public void OnDestroy (ref SystemState state)
			{
			if (this._results.IsCreated)
				{
				this._results.Dispose();
				}
			}

		[BurstCompile]
		public void OnUpdate (ref SystemState state)
			{
			this._results.Clear();
			// Extract components in a temp array then append (no safety handle issues)
			NativeArray<T> components = this._query.ToComponentDataArray<T>(Allocator.Temp);
			this._results.AddRange(components);
			components.Dispose();
			AggregatorDiagnostics<T>.LastCount = this._results.Length;
			// UnityEngine.Debug.Log($"[Aggregator<{typeof(T).Name}>] Collected {_results.Length} entries."); // REMOVED: Debug.Log not allowed in Burst jobs
			// Metrics available via AggregatorDiagnostics<T>.LastCount for debug inspection
			}

		/// <summary>
		/// Returns a copy of the aggregated results for this frame.
		/// </summary>
		public NativeArray<T> GetResults (Allocator allocator)
			{
			return new(this._results.AsArray(), allocator);
			}

		public static int GetLastCount ()
			{
			return AggregatorDiagnostics<T>.LastCount;
			}
		}
	}
