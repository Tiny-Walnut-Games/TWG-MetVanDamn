using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
            }

        public void OnUpdate(ref SystemState state)
            {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var registryEntity = _registry.GetSingletonEntity();
            var entries = state.EntityManager.GetBuffer<EcsPrefabEntry>(registryEntity);

            foreach (var (reqRO, reqEntity) in SystemAPI.Query<RefRO<SudoActionRequest>>().WithEntityAccess())
                {
                var req = reqRO.ValueRO;
                if (TryLookupPrefab(entries, req.ActionKey, out var prefab))
                    {
                    var spawned = ecb.Instantiate(prefab);
                    ecb.SetComponent(spawned, new LocalTransform
                        {
                        Position = new float3(req.ResolvedPosition.x, req.ResolvedPosition.y, req.ResolvedPosition.z),
                        Rotation = quaternion.identity,
                        Scale = 1f
                        });
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
