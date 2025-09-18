using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Tests
    {
#nullable enable
    public class SpawnableKeysEcsPlayModeTests
        {
        private World _world = null!; // assigned in SetUp
        private EntityManager _em; // struct assigned in SetUp

        [SetUp]
        public void SetUp()
            {
            _world = new World("TestWorld_SpawnableKeys");
            _em = _world.EntityManager;
            World.DefaultGameObjectInjectionWorld = _world;
            }

        [TearDown]
        public void TearDown()
            {
            if (World.DefaultGameObjectInjectionWorld == _world)
                World.DefaultGameObjectInjectionWorld = null;
            _world?.Dispose();
            }

        [Test]
        public void Registry_Spawns_Prefabs_For_Common_Keys()
            {
            // Create simple prefab entities
            Entity enemyMeleePrefab = MakePrefab<EnemyMeleeTag>();
            Entity pickupHealthPrefab = MakePrefab<PickupHealthTag>();
            Entity doorLockedPrefab = MakePrefab<DoorLockedTag>();

            // Create registry
            Entity reg = _em.CreateEntity(typeof(EcsPrefabRegistry));
            DynamicBuffer<EcsPrefabEntry> buf = _em.AddBuffer<EcsPrefabEntry>(reg);
            buf.Add(new EcsPrefabEntry { Key = new FixedString64Bytes("spawn_enemy_melee"), Prefab = enemyMeleePrefab });
            buf.Add(new EcsPrefabEntry { Key = new FixedString64Bytes("pickup_health"), Prefab = pickupHealthPrefab });
            buf.Add(new EcsPrefabEntry { Key = new FixedString64Bytes("spawn_door_locked"), Prefab = doorLockedPrefab });

            // Requests
            EmitRequest("spawn_enemy_melee", new float3(1, 0, 0));
            EmitRequest("pickup_health", new float3(2, 0, 0));
            EmitRequest("spawn_door_locked", new float3(3, 0, 0));

            SimulationSystemGroup sim = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            _world.GetOrCreateSystem<SudoActionEcsConsumerSystem>();
            sim.Update();

            // All requests processed
            Assert.AreEqual(0, _em.CreateEntityQuery(typeof(SudoActionRequest)).CalculateEntityCount());

            // Validate each spawned kind has at least one non-prefab instance
            AssertSpawned<EnemyMeleeTag>();
            AssertSpawned<PickupHealthTag>();
            AssertSpawned<DoorLockedTag>();
            }

        private Entity MakePrefab<T>() where T : unmanaged, IComponentData
            {
            Entity e = _em.CreateEntity(typeof(T));
            _em.AddComponent<Prefab>(e);
            return e;
            }

        private void EmitRequest(string key, float3 pos)
            {
            Entity r = _em.CreateEntity(typeof(SudoActionRequest));
            _em.SetComponentData(r, new SudoActionRequest
                {
                ActionKey = new FixedString64Bytes(key),
                ResolvedPosition = pos
                });
            }

        private void AssertSpawned<T>() where T : unmanaged, IComponentData
            {
            EntityQuery q = _em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            NativeArray<Entity> entities = q.ToEntityArray(Allocator.Temp);
            bool foundInstance = false;
            for (int i = 0; i < entities.Length; i++)
                {
                if (!_em.HasComponent<Prefab>(entities[i])) { foundInstance = true; break; }
                }
            entities.Dispose();
            Assert.IsTrue(foundInstance, $"Expected a spawned non-prefab instance for {typeof(T).Name}");
            }
        }
    }
