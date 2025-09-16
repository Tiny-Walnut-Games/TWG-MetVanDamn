using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
#if UNITY_TRANSFORMS_LOCALTRANSFORM
using TransformT = Unity.Transforms.LocalTransform;
#else
using TransformT = TinyWalnutGames.MetVD.Core.Compat.LocalTransformCompat;
#endif

namespace TinyWalnutGames.MetVD.Core
    {
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct SudoActionEcsConsumerSystem : ISystem
        {
        private EntityQuery _requests;
        private EntityQuery _registry;

        public void OnCreate(ref SystemState state)
            {
            _requests = state.GetEntityQuery(ComponentType.ReadOnly<SudoActionRequest>());
            _registry = state.GetEntityQuery(ComponentType.ReadOnly<EcsPrefabRegistry>(), ComponentType.ReadOnly<EcsPrefabEntry>());
            state.RequireForUpdate(_requests);
            state.RequireForUpdate(_registry);

            // Auto-register into Simulation group for manually created worlds used in tests (Editor only)
#if UNITY_EDITOR
            SimulationSystemGroup simGroup = state.World.GetOrCreateSystemManaged<SimulationSystemGroup>();
            simGroup.AddSystemToUpdateList(state.SystemHandle);
#endif
            }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
            {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            Entity registryEntity = _registry.GetSingletonEntity();
            DynamicBuffer<EcsPrefabEntry> entries = state.EntityManager.GetBuffer<EcsPrefabEntry>(registryEntity);

            // Use more compatible query approach for Unity Entities 1.3.14
            NativeArray<Entity> requestEntities = _requests.ToEntityArray(Allocator.Temp);
            NativeArray<SudoActionRequest> requestComponents = _requests.ToComponentDataArray<SudoActionRequest>(Allocator.Temp);

            for (int i = 0; i < requestEntities.Length; i++)
                {
                Entity reqEntity = requestEntities[i];
                SudoActionRequest req = requestComponents[i];

                if (TryLookupPrefab(entries, req.ActionKey, out Entity prefab))
                    {
                    Entity spawned = ecb.Instantiate(prefab);
                    var t = new TransformT
                        {
                        Position = new float3(req.ResolvedPosition.x, req.ResolvedPosition.y, req.ResolvedPosition.z),
                        Rotation = quaternion.identity,
                        Scale = 1f
                        };
                    if (state.EntityManager.HasComponent<TransformT>(prefab))
                        ecb.SetComponent(spawned, t);
                    else
                        ecb.AddComponent(spawned, t);
                    }
                // Always destroy the request to avoid reprocessing (even if no prefab found)
                ecb.DestroyEntity(reqEntity);
                }

            requestEntities.Dispose();
            requestComponents.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            }

        private static bool TryLookupPrefab(DynamicBuffer<EcsPrefabEntry> entries, Unity.Collections.FixedString64Bytes key, out Entity prefab)
            {
            prefab = Entity.Null;
            for (int i = 0; i < entries.Length; i++)
                {
                if (entries[i].Key.Equals(key))
                    {
                    prefab = entries[i].Prefab;
                    return prefab != Entity.Null;
                    }
                }
            return false;
            }
        }
    }
