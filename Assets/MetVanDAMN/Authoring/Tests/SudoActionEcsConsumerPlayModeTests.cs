using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Tests
    {
#nullable enable
    public class SudoActionEcsConsumerPlayModeTests
        {
        private World _world = null!; // assigned in SetUp
        private EntityManager _em; // struct assigned in SetUp

        [SetUp]
        public void SetUp()
            {
            _world = new World("TestWorld_ECS_Consumer");
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
        public void EcsConsumer_Instantiates_BossPrefab_And_Destroys_Request()
            {
            // Create a simple prefab entity with BossTag
            Entity prefab = _em.CreateEntity(typeof(BossTag));
            _em.AddComponent<Prefab>(prefab);

            // Create registry singleton with one entry
            Entity reg = _em.CreateEntity(typeof(EcsPrefabRegistry));
            DynamicBuffer<EcsPrefabEntry> buf = _em.AddBuffer<EcsPrefabEntry>(reg);
            buf.Add(new EcsPrefabEntry { Key = new FixedString64Bytes("spawn_boss"), Prefab = prefab });

            // Create a request
            Entity req = _em.CreateEntity(typeof(SudoActionRequest));
            _em.SetComponentData(req, new SudoActionRequest
                {
                ActionKey = new FixedString64Bytes("spawn_boss"),
                ResolvedPosition = new float3(5, 0, 7)
                });

            // Drive ISystem via sim group
            SimulationSystemGroup sim = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            _world.GetOrCreateSystem<SudoActionEcsConsumerSystem>();
            sim.Update();

            // Request must be consumed
            Assert.AreEqual(0, _em.CreateEntityQuery(typeof(SudoActionRequest)).CalculateEntityCount());
            // One non-prefab BossTag entity should be present
            EntityQuery q = _em.CreateEntityQuery(ComponentType.ReadOnly<BossTag>());
            NativeArray<Entity> entities = q.ToEntityArray(Allocator.Temp);
            Assert.GreaterOrEqual(entities.Length, 1);
            // Ensure spawned entity is not the prefab
            bool foundInstance = false;
            for (int i = 0; i < entities.Length; i++)
                {
                if (!_em.HasComponent<Prefab>(entities[i])) { foundInstance = true; break; }
                }
            Assert.IsTrue(foundInstance, "Expected a spawned (non-prefab) boss entity.");
            entities.Dispose();
            }
        }
    }
