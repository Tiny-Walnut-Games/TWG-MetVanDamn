using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
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
            var biomeSettings = new BiomeGenerationSettings(
                biomeCountRange: new int2(3, 6),
                biomeWeight: 1.0f
            );
            var districtSettings = new DistrictGenerationSettings(
                districtCountRange: new int2(4, 12),
                districtMinDistance: 15f,
                districtWeight: 1.0f
            );
            var sectorSettings = new SectorGenerationSettings(
                sectorsPerDistrictRange: new int2(2, 8),
                sectorGridSize: new int2(6, 6),
                roomsPerSectorRange: new int2(3, 12),
                targetLoopDensity: 0.3f
            );
            var config = new WorldBootstrapConfiguration(
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
            var entity = entityManager.CreateEntity();
            
            // Create proper generation settings
            var biomeSettings = new BiomeGenerationSettings(
                biomeCountRange: new int2(2, 4),
                biomeWeight: 0.8f
            );
            var districtSettings = new DistrictGenerationSettings(
                districtCountRange: new int2(3, 8),
                districtMinDistance: 10f,
                districtWeight: 1.2f
            );
            var sectorSettings = new SectorGenerationSettings(
                sectorsPerDistrictRange: new int2(1, 6),
                sectorGridSize: new int2(4, 4)
            );
            var roomSettings = new RoomGenerationSettings(
                roomsPerSectorRange: new int2(2, 10),
                targetLoopDensity: 0.5f
            );
            
            var config = new WorldBootstrapConfiguration(
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
            // This test validates that the system setup would work correctly
            // without running the actual system update (which requires Unity runtime)
            
            var bootstrapEntity = entityManager.CreateEntity();
            var biomeSettings = new BiomeGenerationSettings(
                biomeCountRange: new int2(1, 3),
                biomeWeight: 1.0f
            );
            var districtSettings = new DistrictGenerationSettings(
                districtCountRange: new int2(2, 5),
                districtMinDistance: 20f,
                districtWeight: 1.0f
            );
            var sectorSettings = new SectorGenerationSettings(
                sectorsPerDistrictRange: new int2(1, 4),
                sectorGridSize: new int2(8, 8)
            );
            var roomSettings = new RoomGenerationSettings(
                roomsPerSectorRange: new int2(1, 8),
                targetLoopDensity: 0.2f
            );
            var config = new WorldBootstrapConfiguration(
                seed: 999,
                worldSize: new int2(50, 50),
                randomizationMode: RandomizationMode.None,
                biomeSettings: biomeSettings,
                districtSettings: districtSettings,
                sectorSettings: sectorSettings,
                roomSettings: roomSettings,
                enableDebugVisualization: true,
                logGenerationSteps: true
            );

            entityManager.AddComponentData(bootstrapEntity, config);

            // Verify the bootstrap entity has the required configuration
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
            // Test that WorldBootstrapConfiguration can coexist with WorldConfiguration
            var entity = entityManager.CreateEntity();

            // Create proper generation settings
            var biomeSettings = new BiomeGenerationSettings(
                biomeCountRange: new int2(3, 7),
                biomeWeight: 1.5f
            );
            var districtSettings = new DistrictGenerationSettings(
                districtCountRange: new int2(5, 15),
                districtMinDistance: 25f,
                districtWeight: 0.9f
            );
            var sectorSettings = new SectorGenerationSettings(
                sectorsPerDistrictRange: new int2(3, 10),
                sectorGridSize: new int2(10, 10)
            );
            var roomSettings = new RoomGenerationSettings(
                roomsPerSectorRange: new int2(4, 15),
                targetLoopDensity: 0.7f
            );

            var bootstrapConfig = new WorldBootstrapConfiguration(
                seed: 777,
                worldSize: new int2(80, 80),
                randomizationMode: RandomizationMode.Partial,
                biomeSettings: biomeSettings,
                districtSettings: districtSettings,
                sectorSettings: sectorSettings,
                roomSettings: roomSettings,
                enableDebugVisualization: false,
                logGenerationSteps: true
            );
            var worldConfig = new WorldConfiguration(
                seed: 777,
                size: new int2(80, 80),
                mode: RandomizationMode.Partial,
                biome: biomeSettings,
                district: districtSettings,
                sector: sectorSettings,
                room: roomSettings,
                debug: false,
                logging: true
            );

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