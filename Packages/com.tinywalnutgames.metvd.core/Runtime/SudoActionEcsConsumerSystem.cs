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
    [BurstCompile]
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
            var simGroup = state.World.GetOrCreateSystemManaged<SimulationSystemGroup>();
            simGroup.AddSystemToUpdateList(state.SystemHandle);
#endif
            }

        public void OnUpdate(ref SystemState state)
            {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            Entity registryEntity = _registry.GetSingletonEntity();
            DynamicBuffer<EcsPrefabEntry> entries = state.EntityManager.GetBuffer<EcsPrefabEntry>(registryEntity);

            foreach ((RefRO<SudoActionRequest> reqRO, Entity reqEntity) in SystemAPI.Query<RefRO<SudoActionRequest>>().WithEntityAccess())
                {
                SudoActionRequest req = reqRO.ValueRO;
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
