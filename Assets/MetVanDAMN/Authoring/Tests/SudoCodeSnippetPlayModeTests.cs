using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Tests
    {
    public class SudoCodeSnippetPlayModeTests
        {
        private World _world;
        private EntityManager _em;

        [SetUp]
        public void SetUp()
            {
            _world = new World("TestWorld_Snippet");
            _em = _world.EntityManager;
            }

        [TearDown]
        public void TearDown()
            {
            _world?.Dispose();
            }

        [Test]
        public void Snippet_Spawn_Emits_Request_And_EcsConsumer_Spawns()
            {
            // Prefab with MarkerWaypointTag to validate spawn
            var prefab = _em.CreateEntity(typeof(MarkerWaypointTag));
            _em.AddComponent<Prefab>(prefab);

            // ECS registry with key -> prefab
            var reg = _em.CreateEntity(typeof(EcsPrefabRegistry));
            var buf = _em.AddBuffer<EcsPrefabEntry>(reg);
            buf.Add(new EcsPrefabEntry { Key = new FixedString64Bytes("spawn_marker_waypoint"), Prefab = prefab });

            // Add snippet
            var snippet = _em.CreateEntity(typeof(SudoCodeSnippet));
            _em.SetComponentData(snippet, new SudoCodeSnippet
                {
                RunOnce = true,
                HasExecuted = false,
                Code = new FixedString512Bytes("log Start;\nspawn spawn_marker_waypoint 1 2 3")
                });

            // Drive init (snippet executor) then sim (ECS consumer)
            var init = _world.GetOrCreateSystemManaged<InitializationSystemGroup>();
            var sim = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            _world.GetOrCreateSystem<SudoCodeSnippetExecutorSystem>();
            _world.GetOrCreateSystem<SudoActionEcsConsumerSystem>();

            init.Update();
            sim.Update();

            // Request should be consumed
            Assert.AreEqual(0, _em.CreateEntityQuery(typeof(SudoActionRequest)).CalculateEntityCount());

            // Spawned instance with tag should exist (non-prefab)
            var q = _em.CreateEntityQuery(typeof(MarkerWaypointTag));
            var ents = q.ToEntityArray(Allocator.Temp);
            bool foundInstance = false;
            for (int i = 0; i < ents.Length; i++)
                {
                if (!_em.HasComponent<Prefab>(ents[i])) { foundInstance = true; break; }
                }
            Assert.IsTrue(foundInstance, "Expected spawned marker waypoint instance.");
            ents.Dispose();
            }
        }
    }
