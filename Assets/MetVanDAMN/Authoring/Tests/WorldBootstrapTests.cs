using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Authoring.Tests
{
    /// <summary>
    /// Tests for WorldBootstrapSystem functionality
    /// </summary>
    public class WorldBootstrapTests
    {
        private World testWorld;
        private EntityManager entityManager;

        [SetUp]
        public void SetUp()
        {
            testWorld = new World("World Bootstrap Test World");
            entityManager = testWorld.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            testWorld?.Dispose();
        }

        [Test]
        public void WorldBootstrapConfiguration_CanBeCreated()
        {
            var biomeSettings = new BiomeGenerationSettings
            {
                BiomeCountRange = new int2(3, 6),
                BiomeWeight = 1.0f
            };
            var districtSettings = new DistrictGenerationSettings
            {
                DistrictCountRange = new int2(4, 12),
                DistrictMinDistance = 15f,
                DistrictWeight = 1.0f
            };
            var sectorSettings = new SectorGenerationSettings
            {
                SectorsPerDistrictRange = new int2(2, 8),
                SectorGridSize = new int2(6, 6)
            };
            var roomSettings = new RoomGenerationSettings
            {
                RoomsPerSectorRange = new int2(3, 12),
                TargetLoopDensity = 0.3f
            };
            var config = new WorldBootstrapConfiguration(
                seed: 42,
                worldSize: new int2(64, 64),
                randomizationMode: RandomizationMode.Partial,
                biomeSettings: biomeSettings,
                districtSettings: districtSettings,
                sectorGenerationSettings: sectorSettings,
                roomSettings: roomSettings,
                enableDebugVisualization: true,
                logGenerationSteps: true
            );

            Assert.AreEqual(42, config.Seed);
            Assert.AreEqual(new int2(64, 64), config.WorldSize);
            Assert.AreEqual(RandomizationMode.Partial, config.RandomizationMode);
            Assert.AreEqual(new int2(3, 6), config.BiomeCountRange);
            Assert.AreEqual(new int2(4, 12), config.DistrictCountRange);
            Assert.AreEqual(15f, config.DistrictMinDistance, 0.001f);
        }

        [Test]
        public void WorldBootstrapConfiguration_CanBeAddedToEntity()
        {
            var entity = entityManager.CreateEntity();
            var worldSettings = new WorldSettings(
                seed: 12345,
                worldSize: new int2(32, 32),
                randomizationMode: RandomizationMode.Full
            );
            var biomeSettings = new BiomeSettings(
                biomeCountRange: new int2(2, 4),
                biomeWeight: 0.8f
            );
            var districtSettings = new DistrictSettings(
                districtCountRange: new int2(3, 8),
                districtMinDistance: 10f,
                districtWeight: 1.2f,
                sectorsPerDistrictRange: new int2(1, 6),
                sectorGridSize: new int2(4, 4),
                roomsPerSectorRange: new int2(2, 10),
                targetLoopDensity: 0.5f
            );
            var debugSettings = new DebugSettings(
                enableDebugVisualization: false,
                logGenerationSteps: false
            );
            var config = new WorldBootstrapConfiguration(
                worldSettings,
                biomeSettings,
                districtSettings,
                debugSettings
            );

            entityManager.AddComponentData(entity, config);

            Assert.IsTrue(entityManager.HasComponent<WorldBootstrapConfiguration>(entity));

            var retrievedConfig = entityManager.GetComponentData<WorldBootstrapConfiguration>(entity);
            Assert.AreEqual(12345, retrievedConfig.Seed);
            Assert.AreEqual(new int2(32, 32), retrievedConfig.WorldSize);
            Assert.AreEqual(RandomizationMode.Full, retrievedConfig.RandomizationMode);
            Assert.AreEqual(0.8f, retrievedConfig.BiomeSettings.BiomeWeight, 0.001f);
            Assert.IsFalse(retrievedConfig.EnableDebugVisualization);
        }

        [Test]
        public void WorldBootstrapTags_CanBeAddedToEntity()
        {
            var entity = entityManager.CreateEntity();

            // Test in-progress tag
            entityManager.AddComponentData(entity, new WorldBootstrapInProgressTag());
            Assert.IsTrue(entityManager.HasComponent<WorldBootstrapInProgressTag>(entity));

            // Test complete tag
            entityManager.RemoveComponent<WorldBootstrapInProgressTag>(entity);
            var completeTag = new WorldBootstrapCompleteTag(biomes: 4, districts: 8, sectors: 24, rooms: 120);
            entityManager.AddComponentData(entity, completeTag);

            Assert.IsTrue(entityManager.HasComponent<WorldBootstrapCompleteTag>(entity));
            var retrievedComplete = entityManager.GetComponentData<WorldBootstrapCompleteTag>(entity);
            Assert.AreEqual(4, retrievedComplete.BiomesGenerated);
            Assert.AreEqual(8, retrievedComplete.DistrictsGenerated);
            Assert.AreEqual(24, retrievedComplete.SectorsGenerated);
            Assert.AreEqual(120, retrievedComplete.RoomsGenerated);
        }

        [Test]
        public void WorldBootstrapSystem_RequiresCorrectComponents()
        {
            var bootstrapEntity = entityManager.CreateEntity();
            var biomeSettings = new BiomeSettings
            {
                BiomeCountRange = new int2(1, 3),
                BiomeWeight = 1.0f
            };
            var districtSettings = new DistrictSettings
            {
                DistrictCountRange = new int2(2, 5),
                DistrictMinDistance = 20f,
                DistrictWeight = 1.0f,
                SectorsPerDistrictRange = new int2(1, 4),
                SectorGridSize = new int2(8, 8),
                RoomsPerSectorRange = new int2(1, 8),
                TargetLoopDensity = 0.2f
            };
            var debugSettings = new DebugSettings(true, true);
            var config = new WorldBootstrapConfiguration(
                new WorldSettings(999u, new int2(50, 50), RandomizationMode.None),
                biomeSettings,
                districtSettings,
                debugSettings
            );

            entityManager.AddComponentData(bootstrapEntity, config);

            Assert.IsTrue(entityManager.HasComponent<WorldBootstrapConfiguration>(bootstrapEntity));
            Assert.IsFalse(entityManager.HasComponent<WorldBootstrapInProgressTag>(bootstrapEntity));
            Assert.IsFalse(entityManager.HasComponent<WorldBootstrapCompleteTag>(bootstrapEntity));

            var storedConfig = entityManager.GetComponentData<WorldBootstrapConfiguration>(bootstrapEntity);
            Assert.AreEqual(999, storedConfig.Seed);
            Assert.AreEqual(new int2(50, 50), storedConfig.WorldSize);
        }

        [Test]
        public void WorldConfiguration_IsCompatibleWithBootstrap()
        {
            var entity = entityManager.CreateEntity();

            var bootstrapSettings = new WorldBootstrapSettings
            {
                Seed = 777,
                WorldSize = new int2(80, 80),
                RandomizationMode = RandomizationMode.Partial,
                BiomeCountRange = new int2(3, 7),
                BiomeWeight = 1.5f,
                DistrictCountRange = new int2(5, 15),
                DistrictMinDistance = 25f,
                DistrictWeight = 0.9f,
                SectorsPerDistrictRange = new int2(3, 10),
                SectorGridSize = new int2(10, 10),
                RoomsPerSectorRange = new int2(4, 15),
                TargetLoopDensity = 0.7f,
                EnableDebugVisualization = false,
                LogGenerationSteps = true
            };

            var bootstrapConfig = new WorldBootstrapConfiguration(bootstrapSettings);
            var worldConfig = new WorldConfiguration
            {
                Seed = 777,
                WorldSize = new int2(80, 80),
                TargetSectors = 150,
                RandomizationMode = RandomizationMode.Partial
            };

            entityManager.AddComponentData(entity, bootstrapConfig);
            entityManager.AddComponentData(entity, worldConfig);

            Assert.IsTrue(entityManager.HasComponent<WorldBootstrapConfiguration>(entity));
            Assert.IsTrue(entityManager.HasComponent<WorldConfiguration>(entity));

            var retrievedBootstrap = entityManager.GetComponentData<WorldBootstrapConfiguration>(entity);
            var retrievedWorld = entityManager.GetComponentData<WorldConfiguration>(entity);

            Assert.AreEqual(retrievedBootstrap.Seed, retrievedWorld.Seed);
            Assert.AreEqual(retrievedBootstrap.WorldSize, retrievedWorld.WorldSize);
            Assert.AreEqual(retrievedBootstrap.RandomizationMode, retrievedWorld.RandomizationMode);
        }
    }
}
