using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Core
    {
    public struct WorldConfigInitialized : IComponentData { }

    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct WorldConfigAspectSampleSystem : ISystem
        {
        private EntityQuery _worldQ;

        public void OnCreate(ref SystemState state)
            {
            _worldQ = state.GetEntityQuery(ComponentType.ReadOnly<WorldSeedData>(), ComponentType.ReadOnly<WorldBoundsData>(), ComponentType.ReadOnly<WorldGenerationConfigData>());
            state.RequireForUpdate(_worldQ);
            }

        public void OnUpdate(ref SystemState state)
            {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach ((WorldConfigAspect world, Entity entity) in SystemAPI.Query<WorldConfigAspect>().WithEntityAccess())
                {
                if (!state.EntityManager.HasComponent<WorldConfigInitialized>(entity))
                    {
                    Debug.Log($"[WorldConfig] Seed={world.WorldSeed} Center={world.Center} Extents={world.Extents} Sectors={world.TargetSectors} BiomeRadius={world.BiomeTransitionRadius}");
                    ecb.AddComponent<WorldConfigInitialized>(entity);
                    }
                }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            }
        }
    }
