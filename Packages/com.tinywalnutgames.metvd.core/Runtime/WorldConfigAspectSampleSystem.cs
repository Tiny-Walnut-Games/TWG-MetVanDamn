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
            _worldQ = state.GetEntityQuery(
                ComponentType.ReadOnly<WorldSeedData>(), 
                ComponentType.ReadOnly<WorldBoundsData>(), 
                ComponentType.ReadOnly<WorldGenerationConfigData>());
            state.RequireForUpdate(_worldQ);
            }

        public void OnUpdate(ref SystemState state)
            {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

			// Use more compatible direct component access instead of aspects
			NativeArray<Entity> entities = _worldQ.ToEntityArray(Allocator.Temp);
			NativeArray<WorldSeedData> seedComponents = _worldQ.ToComponentDataArray<WorldSeedData>(Allocator.Temp);
			NativeArray<WorldBoundsData> boundsComponents = _worldQ.ToComponentDataArray<WorldBoundsData>(Allocator.Temp);
			NativeArray<WorldGenerationConfigData> configComponents = _worldQ.ToComponentDataArray<WorldGenerationConfigData>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
                {
				Entity entity = entities[i];
                if (!state.EntityManager.HasComponent<WorldConfigInitialized>(entity))
                    {
					WorldSeedData seed = seedComponents[i];
					WorldBoundsData bounds = boundsComponents[i];
					WorldGenerationConfigData config = configComponents[i];
                    
                    Debug.Log($"[WorldConfig] Seed={seed.Value} Center={bounds.Center} Extents={bounds.Extents} Sectors={config.TargetSectorCount} BiomeRadius={config.BiomeTransitionRadius}");
                    ecb.AddComponent<WorldConfigInitialized>(entity);
                    }
                }

            entities.Dispose();
            seedComponents.Dispose();
            boundsComponents.Dispose();
            configComponents.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            }
        }
    }
