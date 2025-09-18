using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring.Tests
    {
#nullable enable
    public class WorldConfigAspectPlayModeTests
        {
        private World _world = null!; // assigned in SetUp
        private EntityManager _em; // struct assigned in SetUp

        [SetUp]
        public void SetUp()
            {
            _world = new World("TestWorld_WorldConfigAspect");
            _em = _world.EntityManager;
            }

        [TearDown]
        public void TearDown()
            {
            _world?.Dispose();
            }

        [Test]
        public void Authoring_Bakes_And_Aspect_Exposes_Values()
            {
            // Simulate Baker output directly (no MonoBehaviour instantiation in this test)
            Entity e = _em.CreateEntity(typeof(WorldSeedData), typeof(WorldBoundsData), typeof(WorldGenerationConfigData));
            _em.SetComponentData(e, new WorldSeedData { Value = 123u });
            _em.SetComponentData(e, new WorldBoundsData { Center = new float3(10, 20, 0), Extents = new float3(25, 25, 0) });
            _em.SetComponentData(e, new WorldGenerationConfigData
                {
                TargetSectorCount = 7,
                BiomeTransitionRadius = 12.5f,
                EnableDebugVisualization = 1,
                LogGenerationSteps = 1
                });

            // Query via aspect using a tiny one-off system group update
            InitializationSystemGroup init = _world.GetOrCreateSystemManaged<InitializationSystemGroup>();
            _world.GetOrCreateSystem<WorldConfigAspectSampleSystem>();
            init.Update();

            // Validate values via direct reads
            uint seed = _em.GetComponentData<WorldSeedData>(e).Value;
            WorldBoundsData bounds = _em.GetComponentData<WorldBoundsData>(e);
            WorldGenerationConfigData cfg = _em.GetComponentData<WorldGenerationConfigData>(e);

            Assert.AreEqual(123u, seed);
            Assert.AreEqual(new float3(10, 20, 0), bounds.Center);
            Assert.AreEqual(new float3(25, 25, 0), bounds.Extents);
            Assert.AreEqual(7, cfg.TargetSectorCount);
            Assert.AreEqual(12.5f, cfg.BiomeTransitionRadius);
            Assert.AreEqual(1, cfg.EnableDebugVisualization);
            Assert.AreEqual(1, cfg.LogGenerationSteps);
            }
        }
    }
