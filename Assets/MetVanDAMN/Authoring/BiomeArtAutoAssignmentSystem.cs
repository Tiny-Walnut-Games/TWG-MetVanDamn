#nullable enable
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using CoreBiome = TinyWalnutGames.MetVD.Core.Biome;

#nullable enable

namespace TinyWalnutGames.MetVD.Authoring
	{
	/// <summary>
	/// Automatically assigns a BiomeArtProfile from the central library to biome entities
	/// that have no profile yet. Deterministically seeds by NodeId/biome type/world seed.
	/// </summary>
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	public partial struct BiomeArtAutoAssignmentSystem : ISystem
		{
		private EntityQuery _libraryQ;
		private EntityQuery _targetsQ;
		private EntityQuery _configQ;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
			{
			_libraryQ = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<BiomeArtProfileLibraryRef>()
				.Build(ref state);
			_targetsQ = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<CoreBiome, NodeId, BiomeArtProfileReference>()
				.Build(ref state);
			_configQ = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<WorldConfiguration>()
				.Build(ref state);
			}

		public void OnUpdate(ref SystemState state)
			{
			if (_libraryQ.IsEmptyIgnoreFilter) return;
			if (_targetsQ.IsEmptyIgnoreFilter) return;
			WorldConfiguration config =
				_configQ.IsEmptyIgnoreFilter ? default : _configQ.GetSingleton<WorldConfiguration>();

			Entity libEntity = _libraryQ.GetSingletonEntity();
			BiomeArtProfileLibraryRef libRef =
				state.EntityManager.GetComponentData<BiomeArtProfileLibraryRef>(libEntity);
			if (!libRef.Library.IsValid()) return;
			BiomeArtProfileLibrary? lib = libRef.Library.Value;
			if (lib == null) return;

			NativeArray<Entity> ents = _targetsQ.ToEntityArray(Allocator.Temp);
			NativeArray<BiomeArtProfileReference> refs =
				_targetsQ.ToComponentDataArray<BiomeArtProfileReference>(Allocator.Temp);
			NativeArray<CoreBiome> biomes = _targetsQ.ToComponentDataArray<CoreBiome>(Allocator.Temp);
			NativeArray<NodeId> nodeIds = _targetsQ.ToComponentDataArray<NodeId>(Allocator.Temp);

			try
				{
				for (int i = 0; i < ents.Length; i++)
					{
					BiomeArtProfileReference r = refs[i];
					if (r.ProfileRef.IsValid())
						continue; // already has a profile

					CoreBiome biome = biomes[i];
					NodeId node = nodeIds[i];
					uint seed = (uint)math.max(1,
						config.Seed ^ (uint)biome.Type * 2654435761u ^ (uint)node.Value * 374761393u);
					var rng = new Random(seed);
					BiomeElevation elevation = state.EntityManager.HasComponent<BiomeElevationMask>(ents[i])
						? state.EntityManager.GetComponentData<BiomeElevationMask>(ents[i]).Value
						: BiomeElevation.Any;
					// If elevation is specified, fold into the seed to vary selection across layers deterministically
					if (elevation != BiomeElevation.Any && elevation != BiomeElevation.None)
						{
						uint e = (uint)elevation;
						seed ^= e * 2246822519u; // mix elevation into RNG
						rng = new Random(seed);
						}

					BiomeArtProfile? chosen = SelectProfileForTypeAndElevation(lib, biome.Type, elevation, ref rng);
					if (chosen == null) continue;

					// write back chosen profile; keep projection type as-is (from authoring if present)
					r.ProfileRef = new UnityObjectRef<BiomeArtProfile> { Value = chosen };
					state.EntityManager.SetComponentData(ents[i], r);
					}
				}
			finally
				{
				if (ents.IsCreated) ents.Dispose();
				if (refs.IsCreated) refs.Dispose();
				if (biomes.IsCreated) biomes.Dispose();
				if (nodeIds.IsCreated) nodeIds.Dispose();
				}
			}

		private static BiomeArtProfile? SelectProfileForTypeAndElevation(BiomeArtProfileLibrary lib, BiomeType type,
			BiomeElevation elevation, ref Random rng)
			{
			// Ask library for best matching profiles: type+elevation -> type -> global
			BiomeArtProfile[] pool = lib.GetProfiles(type, elevation);
			if (pool != null && pool.Length > 0)
				{
				int idx = rng.NextInt(0, pool.Length);
				return pool[idx];
				}

			return null;
			}
		}
	}
