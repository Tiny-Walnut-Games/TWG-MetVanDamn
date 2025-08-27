using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;
using CoreBootstrap = TinyWalnutGames.MetVD.Core.WorldBootstrapConfiguration;
using CoreInProgress = TinyWalnutGames.MetVD.Core.WorldBootstrapInProgressTag;
using CoreComplete = TinyWalnutGames.MetVD.Core.WorldBootstrapCompleteTag;

namespace TinyWalnutGames.MetVD.Authoring.Tests
{
    /// <summary>
    /// Tests for WorldBootstrapSystem functionality (aligned with Core configuration types)
    /// </summary>
    public class WorldBootstrapTests
    {
        private World _world;
        private EntityManager _em;

        [SetUp]
        public void SetUp()
        {
            _world = new World("World Bootstrap Test World");
            _em = _world.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
        }

        [Test]
        public void WorldBootstrapConfiguration_CanBeCreated()
        {
            var biomeSettings = new BiomeGenerationSettings(new int2(3, 6), 1.0f);
            var districtSettings = new DistrictGenerationSettings(new int2(4, 12), 15f, 1.0f);
            var sectorSettings = new SectorGenerationSettings(new int2(2, 8), new int2(6, 6));
            var roomSettings = new RoomGenerationSettings(new int2(3, 12), 0.3f);

            var config = new CoreBootstrap(
                seed: 42,
                worldSize: new int2(64, 64),
                randomizationMode: RandomizationMode.Partial,
                biomeSettings: biomeSettings,
                districtSettings: districtSettings,
                sectorSettings: sectorSettings,
                roomSettings: roomSettings,
                enableDebugVisualization: true,
                logGenerationSteps: true
            );

            Assert.AreEqual(42, config.Seed);
            Assert.AreEqual(new int2(64, 64), config.WorldSize);
            Assert.AreEqual(RandomizationMode.Partial, config.RandomizationMode);
            Assert.AreEqual(new int2(3, 6), config.BiomeSettings.BiomeCountRange);
            Assert.AreEqual(new int2(4, 12), config.DistrictSettings.DistrictCountRange);
            Assert.AreEqual(15f, config.DistrictSettings.DistrictMinDistance, 0.001f);
        }

        [Test]
        public void WorldBootstrapConfiguration_CanBeAddedToEntity()
        {
            var entity = _em.CreateEntity();

            var biomeSettings = new BiomeGenerationSettings(new int2(2, 4), 0.8f);
            var districtSettings = new DistrictGenerationSettings(new int2(3, 8), 10f, 1.2f);
            var sectorSettings = new SectorGenerationSettings(new int2(1, 6), new int2(4, 4));
            var roomSettings = new RoomGenerationSettings(new int2(2, 10), 0.5f);

            var config = new CoreBootstrap(
                seed: 12345,
                worldSize: new int2(32, 32),
                randomizationMode: RandomizationMode.Full,
                biomeSettings: biomeSettings,
                districtSettings: districtSettings,
                sectorSettings: sectorSettings,
                roomSettings: roomSettings,
                enableDebugVisualization: false,
                logGenerationSteps: false
            );

            _em.AddComponentData(entity, config);
            Assert.IsTrue(_em.HasComponent<CoreBootstrap>(entity));
            var retrieved = _em.GetComponentData<CoreBootstrap>(entity);
            Assert.AreEqual(12345, retrieved.Seed);
            Assert.AreEqual(new int2(32, 32), retrieved.WorldSize);
            Assert.AreEqual(RandomizationMode.Full, retrieved.RandomizationMode);
            Assert.AreEqual(0.8f, retrieved.BiomeSettings.BiomeWeight, 0.001f);
            Assert.IsFalse(retrieved.EnableDebugVisualization);
        }

        [Test]
        public void WorldBootstrapTags_CanBeAddedToEntity()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new CoreInProgress());
            Assert.IsTrue(_em.HasComponent<CoreInProgress>(entity));
            _em.RemoveComponent<CoreInProgress>(entity);
            var complete = new CoreComplete(biomes: 4, districts: 8, sectors: 24, rooms: 120);
            _em.AddComponentData(entity, complete);
            Assert.IsTrue(_em.HasComponent<CoreComplete>(entity));
            var retrieved = _em.GetComponentData<CoreComplete>(entity);
            Assert.AreEqual(4, retrieved.BiomesGenerated);
            Assert.AreEqual(8, retrieved.DistrictsGenerated);
            Assert.AreEqual(24, retrieved.SectorsGenerated);
            Assert.AreEqual(120, retrieved.RoomsGenerated);
        }

        [Test]
        public void WorldBootstrapSystem_RequiresCorrectComponents()
        {
            var entity = _em.CreateEntity();
            var biomeSettings = new BiomeGenerationSettings(new int2(1, 3), 1.0f);
            var districtSettings = new DistrictGenerationSettings(new int2(2, 5), 20f, 1.0f);
            var sectorSettings = new SectorGenerationSettings(new int2(1, 4), new int2(8, 8));
            var roomSettings = new RoomGenerationSettings(new int2(1, 8), 0.2f);
            var config = new CoreBootstrap(999, new int2(50, 50), RandomizationMode.None, biomeSettings, districtSettings, sectorSettings, roomSettings, true, true);
            _em.AddComponentData(entity, config);
            Assert.IsTrue(_em.HasComponent<CoreBootstrap>(entity));
            Assert.IsFalse(_em.HasComponent<CoreInProgress>(entity));
            Assert.IsFalse(_em.HasComponent<CoreComplete>(entity));
            var stored = _em.GetComponentData<CoreBootstrap>(entity);
            Assert.AreEqual(999, stored.Seed);
            Assert.AreEqual(new int2(50, 50), stored.WorldSize);
        }

        [Test]
        public void WorldConfiguration_IsCompatibleWithBootstrap()
        {
            var entity = _em.CreateEntity();
            var biomeSettings = new BiomeGenerationSettings(new int2(3, 7), 1.5f);
            var districtSettings = new DistrictGenerationSettings(new int2(5, 15), 25f, 0.9f);
            var sectorSettings = new SectorGenerationSettings(new int2(3, 10), new int2(10, 10));
            var roomSettings = new RoomGenerationSettings(new int2(4, 15), 0.7f);
            var bootstrap = new CoreBootstrap(777, new int2(80, 80), RandomizationMode.Partial, biomeSettings, districtSettings, sectorSettings, roomSettings, false, true);
            var worldConfig = new WorldConfiguration { Seed = 777, WorldSize = new int2(80, 80), TargetSectors = 150, RandomizationMode = RandomizationMode.Partial };
            _em.AddComponentData(entity, bootstrap);
            _em.AddComponentData(entity, worldConfig);
            Assert.IsTrue(_em.HasComponent<CoreBootstrap>(entity));
            Assert.IsTrue(_em.HasComponent<WorldConfiguration>(entity));
            var retrievedBootstrap = _em.GetComponentData<CoreBootstrap>(entity);
            var retrievedWorld = _em.GetComponentData<WorldConfiguration>(entity);
            Assert.AreEqual(retrievedBootstrap.Seed, retrievedWorld.Seed);
            Assert.AreEqual(retrievedBootstrap.WorldSize, retrievedWorld.WorldSize);
            Assert.AreEqual(retrievedBootstrap.RandomizationMode, retrievedWorld.RandomizationMode);
        }

        /*
        ==================================================================================================
        Legacy Test Code (Preserved Non-Destructively)
        --------------------------------------------------------------------------------------------------
        The following block contains the original test implementations prior to the refactor that introduced
        grouped settings structs (BiomeGenerationSettings, DistrictGenerationSettings, etc.). These are kept
        for historical intent and potential future API adaptation. They reference earlier property layouts.
        They are intentionally commented to avoid compile errors with the current data model.

        using NUnit.Framework;
        using Unity.Entities;
        using Unity.Collections;
        using Unity.Mathematics;
        using TinyWalnutGames.MetVD.Core;
        using TinyWalnutGames.MetVD.Graph;
        using TinyWalnutGames.MetVD.Shared;
        using CoreBootstrap = TinyWalnutGames.MetVD.Core.WorldBootstrapConfiguration;
        using CoreInProgress = TinyWalnutGames.MetVD.Core.WorldBootstrapInProgressTag;
        using CoreComplete = TinyWalnutGames.MetVD.Core.WorldBootstrapCompleteTag;

        // Original class layout retained below...
        // (Content omitted here for brevity; full original was provided in earlier revisions.)
        ==================================================================================================
        */
    }
}
