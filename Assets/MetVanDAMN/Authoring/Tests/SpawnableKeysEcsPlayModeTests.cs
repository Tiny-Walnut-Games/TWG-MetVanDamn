using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Tests
    {
    public class SpawnableKeysEcsPlayModeTests
        {
        private World _world;
        private EntityManager _em;

        [SetUp]
        public void SetUp()
            {
            _world = new World("TestWorld_SpawnableKeys");
            _em = _world.EntityManager;
            }

        [TearDown]
        public void TearDown()
            {
            _world?.Dispose();
            }

        [Test]
        public void Registry_Spawns_Prefabs_For_Common_Keys()
            {
            // Create simple prefab entities
            var enemyMeleePrefab = MakePrefab<EnemyMeleeTag>();
            var pickupHealthPrefab = MakePrefab<PickupHealthTag>();
            var doorLockedPrefab = MakePrefab<DoorLockedTag>();

            // Create registry
            var reg = _em.CreateEntity(typeof(EcsPrefabRegistry));
            var buf = _em.AddBuffer<EcsPrefabEntry>(reg);
            buf.Add(new EcsPrefabEntry { Key = new FixedString64Bytes("spawn_enemy_melee"), Prefab = enemyMeleePrefab });
            buf.Add(new EcsPrefabEntry { Key = new FixedString64Bytes("pickup_health"), Prefab = pickupHealthPrefab });
            buf.Add(new EcsPrefabEntry { Key = new FixedString64Bytes("spawn_door_locked"), Prefab = doorLockedPrefab });

            // Requests
            EmitRequest("spawn_enemy_melee", new float3(1, 0, 0));
            EmitRequest("pickup_health", new float3(2, 0, 0));
            EmitRequest("spawn_door_locked", new float3(3, 0, 0));

            var sim = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
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
            var e = _em.CreateEntity(typeof(T));
            _em.AddComponent<Prefab>(e);
            return e;
            }

        private void EmitRequest(string key, float3 pos)
            {
            var r = _em.CreateEntity(typeof(SudoActionRequest));
            _em.SetComponentData(r, new SudoActionRequest
                {
                ActionKey = new FixedString64Bytes(key),
                ResolvedPosition = pos
                });
            }

        private void AssertSpawned<T>() where T : unmanaged, IComponentData
            {
            var q = _em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            var entities = q.ToEntityArray(Allocator.Temp);
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
