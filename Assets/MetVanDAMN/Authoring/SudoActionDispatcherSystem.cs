#nullable enable
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVanDAMN.Authoring
	{
	/// <summary>
	/// Converts SudoActionHint into a one-time SudoActionRequest for downstream systems.
	/// If OneOff is true, the hint is tagged with SudoActionDispatched to prevent re-emission.
	/// Determinism: seed is derived from WorldConfiguration.Seed and the hint ActionKey/Seed.
	/// Consumers should listen for SudoActionRequest, act on matching ActionKey, and then
	/// destroy the request entity to avoid re-processing.
	/// </summary>
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	public partial struct SudoActionDispatcherSystem : ISystem
		{
		private EntityQuery _hintsQ;
		private EntityArchetype _requestArch;

		private static uint Fnv1a32(in Unity.Collections.FixedString64Bytes key)
			{
			const uint FNV_OFFSET = 2166136261u;
			const uint FNV_PRIME = 16777619u;
			uint hash = FNV_OFFSET;
			int len = key.Length;
			for (int i = 0; i < len; i++)
				{
				byte b = key[i];
				hash ^= b;
				hash *= FNV_PRIME;
				}

			return hash == 0 ? 1u : hash;
			}

		// Do not Burst-compile OnCreate: it can trigger managed allocations (e.g., ComponentType[])
		public void OnCreate(ref SystemState state)
			{
			_hintsQ = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<SudoActionHint>()
				.WithNone<SudoActionDispatched>()
				.Build(ref state);

			_requestArch = state.EntityManager.CreateArchetype(typeof(SudoActionRequest));

			// Auto-register into the Initialization group for manually created worlds in tests (Editor only)
#if UNITY_EDITOR
			InitializationSystemGroup initGroup = state.World.GetOrCreateSystemManaged<InitializationSystemGroup>();
			initGroup.AddSystemToUpdateList(state.SystemHandle);
#endif
			}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
			{
			if (_hintsQ.IsEmptyIgnoreFilter) return;
			uint worldSeed = 0u;
			if (SystemAPI.TryGetSingleton<WorldConfiguration>(out WorldConfiguration config))
				{
				worldSeed = (uint)config.Seed;
				}

			// Debug logging removed after stabilization to keep console output clean.

			NativeArray<Entity> ents = _hintsQ.ToEntityArray(Allocator.Temp);
			NativeArray<SudoActionHint> hints = _hintsQ.ToComponentDataArray<SudoActionHint>(Allocator.Temp);
			try
				{
				for (int i = 0; i < ents.Length; i++)
					{
					Entity e = ents[i];
					SudoActionHint h = hints[i];
					// Guard: empty action keys are skipped (optionally mark dispatched if OneOff)
					if (h.ActionKey.Length == 0)
						{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
						UnityEngine.Debug.LogWarning("SudoActionDispatcher: Skipping hint with empty ActionKey.");
#endif
						if (h.OneOff)
							{
							state.EntityManager.AddComponent<SudoActionDispatched>(e);
							}

						continue;
						}

					// Deterministic seed: worldSeed ^ FNV1a(ActionKey) ^ folded constraints, unless explicit Seed provided
					uint keyHash = Fnv1a32(h.ActionKey);
					uint constraintFold = 0u;
					constraintFold ^= (uint)h.ElevationMask * 0x9E3779B1u;
					if (h.HasTypeConstraint != 0)
						constraintFold ^= ((uint)h.TypeConstraint + 1u) * 0x85EBCA6Bu;

					uint seed = h.Seed != 0 ? h.Seed : math.max(1u, worldSeed ^ keyHash ^ constraintFold);
					var rng = new Random(seed);
					float3 resolved;
					if (h.Radius <= 0f)
						{
						resolved = h.Center;
						}
					else
						{
						float2 offset = math.normalizesafe(rng.NextFloat2Direction()) * rng.NextFloat(0f, h.Radius);
						resolved = h.Center + new float3(offset.x, 0f, offset.y);
						}

					Entity reqEntity = state.EntityManager.CreateEntity(_requestArch);
					state.EntityManager.SetComponentData(reqEntity, new SudoActionRequest
						{
						ActionKey = h.ActionKey,
						ResolvedPosition = resolved,
						ElevationMask = h.ElevationMask,
						TypeConstraint = h.TypeConstraint,
						HasTypeConstraint = h.HasTypeConstraint,
						Seed = seed,
						SourceHint = e
						});

					// Debug logging removed after stabilization to keep console output clean.

					if (h.OneOff)
						{
						state.EntityManager.AddComponent<SudoActionDispatched>(e);
						}
					}
				}
			finally
				{
				if (ents.IsCreated) ents.Dispose();
				if (hints.IsCreated) hints.Dispose();
				}
			}
		}
	}
