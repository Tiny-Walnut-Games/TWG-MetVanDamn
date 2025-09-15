// Define METVD_SAMPLES_GO_CONSUMER to include this GameObject-based test.
#if METVD_SAMPLES_GO_CONSUMER
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring.Tests
{
    public class SudoActionConsumerPlayModeTests
    {
        private World _world;
        private EntityManager _em;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld_SudoConsumer");
            _em = _world.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null)
            {
                _world.Dispose();
                _world = null;
            }
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("SudoAction_")) Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SpawnBossRequest_SpawnsObject_AndDestroysRequest()
        {
            // Arrange: create request entity
            var req = _em.CreateEntity(typeof(SudoActionRequest));
            _em.SetComponentData(req, new SudoActionRequest
            {
                ActionKey = new Unity.Collections.FixedString64Bytes("spawn_boss"),
                HasTypeConstraint = false,
                TypeConstraint = 0,
                ElevationMask = 0,
                HasCenter = true,
                Center = new float3(2, 0, 3),
                HasRadius = false,
                Radius = 0,
                ResolvedPosition = new float3(2, 0, 3)
            });

            // System under test
            var sys = _world.GetOrCreateSystemManaged<SudoActionRequestConsumerSystem>();

            // Act
            sys.Update(_world.Unmanaged);

            // Assert: request should be destroyed
            var q = _em.CreateEntityQuery(typeof(SudoActionRequest));
            Assert.AreEqual(0, q.CalculateEntityCount());

            // Assert: a boss object should exist (prefab or primitive fallback)
            var objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameObject boss = null;
            foreach (var go in objects)
            {
                if (go.name == "SudoAction_spawn_boss") { boss = go; break; }
            }
            Assert.IsNotNull(boss, "Expected a spawned boss object named 'SudoAction_spawn_boss'.");

            // If prefab path is configured, DemoBossController may exist; otherwise it's okay to be primitive
            boss.TryGetComponent<DemoBossController>(out var demoBoss);
            // No strict assert on DemoBossController to allow fallback; presence is a bonus
            Assert.AreEqual(new Vector3(2, 0.5f, 3), boss.transform.position, 1e-2f);
        }
    }
}
#endif
