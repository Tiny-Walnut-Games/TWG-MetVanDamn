using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Tests
    {
    public class SpawnableKeysEquipmentAndSetpiecesEcsPlayModeTests
        {
        private World _world;
        private EntityManager _em;

        [SetUp]
        public void SetUp()
            {
            _world = new World("TestWorld_SpawnableKeys_Equip_Setpiece");
            _em = _world.EntityManager;
            }

        [TearDown]
        public void TearDown()
            {
            _world?.Dispose();
            }

        [Test]
        public void Registry_Spawns_Prefabs_For_Equipment_And_Setpiece_Keys()
            {
            // Create prefab entities with appropriate tags
            var weaponPrefab = MakePrefab<PickupWeaponTag>();
            var setpieceShipPrefab = MakePrefab<SetpieceCrashedShipTag>();

            // Create registry
            var reg = _em.CreateEntity(typeof(EcsPrefabRegistry));
            var buf = _em.AddBuffer<EcsPrefabEntry>(reg);
            buf.Add(new EcsPrefabEntry { Key = new FixedString64Bytes("pickup_weapon"), Prefab = weaponPrefab });
            buf.Add(new EcsPrefabEntry { Key = new FixedString64Bytes("setpiece_crashed_ship"), Prefab = setpieceShipPrefab });

            // Emit requests
            EmitRequest("pickup_weapon", new float3(10, 0, 0));
            EmitRequest("setpiece_crashed_ship", new float3(20, 0, 0));

            var sim = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            _world.GetOrCreateSystem<SudoActionEcsConsumerSystem>();
            sim.Update();

            // Requests consumed
            Assert.AreEqual(0, _em.CreateEntityQuery(typeof(SudoActionRequest)).CalculateEntityCount());

            // Instances spawned
            AssertSpawned<PickupWeaponTag>();
            AssertSpawned<SetpieceCrashedShipTag>();
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
