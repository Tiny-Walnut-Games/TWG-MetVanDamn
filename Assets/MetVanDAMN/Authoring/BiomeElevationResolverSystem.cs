using TinyWalnutGames.MetVD.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
// Alias core biome component to avoid namespace ambiguity with TinyWalnutGames.MetVD.Biome
using CoreBiome = TinyWalnutGames.MetVD.Core.Biome;

namespace TinyWalnutGames.MetVD.Authoring
    {
    /// <summary>
    /// Applies elevation masks to biome entities based on available BiomeElevationHint components.
    /// Runs before art auto-assignment so selection can consider elevation.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(BiomeArtAutoAssignmentSystem))]
    public partial struct BiomeElevationResolverSystem : ISystem
        {
        private EntityQuery _hintQ;
        private EntityQuery _biomesQ;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
            {
            _hintQ = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<BiomeElevationHint>()
                .Build(ref state);
            _biomesQ = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CoreBiome, NodeId>()
                .WithNone<BiomeElevationMask>()
                .Build(ref state);

            // Auto-register into Initialization group for manually created worlds used in tests (Editor only)
#if UNITY_EDITOR
            var initGroup = state.World.GetOrCreateSystemManaged<InitializationSystemGroup>();
            initGroup.AddSystemToUpdateList(state.SystemHandle);
#endif
            }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
            {
            if (_hintQ.IsEmptyIgnoreFilter) return;
            if (_biomesQ.IsEmptyIgnoreFilter) return;

            // Aggregate hints by type
            NativeArray<BiomeElevationHint> hints = _hintQ.ToComponentDataArray<BiomeElevationHint>(Allocator.Temp);
            try
                {
                var typeToMask = new NativeParallelHashMap<byte, BiomeElevation>(hints.Length, Allocator.Temp);
                try
                    {
                    foreach (var h in hints)
                        {
                        byte key = (byte)h.Type;
                        if (typeToMask.TryGetValue(key, out var existing))
                            {
                            typeToMask[key] = existing | h.Mask;
                            }
                        else
                            {
                            typeToMask.TryAdd(key, h.Mask);
                            }
                        }

                    NativeArray<Entity> ents = _biomesQ.ToEntityArray(Allocator.Temp);
                    NativeArray<CoreBiome> biomes = _biomesQ.ToComponentDataArray<CoreBiome>(Allocator.Temp);
                    try
                        {
                        for (int i = 0; i < ents.Length; i++)
                            {
                            byte key = (byte)biomes[i].Type;
                            if (typeToMask.TryGetValue(key, out var mask) && mask != BiomeElevation.None)
                                {
                                state.EntityManager.AddComponentData(ents[i], new BiomeElevationMask(mask));
                                }
                            }
                        }
                    finally
                        {
                        if (ents.IsCreated) ents.Dispose();
                        if (biomes.IsCreated) biomes.Dispose();
                        }
                    }
                finally
                    {
                    if (typeToMask.IsCreated) typeToMask.Dispose();
                    }
                }
            finally
                {
                if (hints.IsCreated) hints.Dispose();
                }
            }
        }
    }
