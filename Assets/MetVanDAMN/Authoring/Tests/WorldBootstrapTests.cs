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
using CoreBiomeSettings = TinyWalnutGames.MetVD.Core.BiomeGenerationSettings;
using CoreDistrictSettings = TinyWalnutGames.MetVD.Core.DistrictGenerationSettings;
using CoreSectorSettings = TinyWalnutGames.MetVD.Core.SectorGenerationSettings;

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
            var biomeSettings = new CoreBiomeSettings(
                biomeCountRange: new int2(3, 6),
                biomeWeight: 1.0f
            );
            var districtSettings = new CoreDistrictSettings(
                districtCountRange: new int2(4, 12),
                districtMinDistance: 15f,
                districtWeight: 1.0f
            );
            var sectorSettings = new CoreSectorSettings(
                sectorsPerDistrictRange: new int2(2, 8),
                sectorGridSize: new int2(6, 6),
                roomsPerSectorRange: new int2(3, 12),
                targetLoopDensity: 0.3f
            );
            var roomSettings = new TinyWalnutGames.MetVD.Core.RoomGenerationSettings(
                maxAttempts: 50,
                minRoomSize: new int2(4, 4),
                maxRoomSize: new int2(12, 12),
                corridorWidth: 2
            );
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
            var config = new CoreBootstrap(
                worldSettings,
                biomeSettings,
                districtSettings,
                debugSettings
            );

            entityManager.AddComponentData(entity, config);

            Assert.IsTrue(entityManager.HasComponent<CoreBootstrap>(entity));

            var retrievedConfig = entityManager.GetComponentData<CoreBootstrap>(entity);
            Assert.AreEqual(12345, retrievedConfig.Seed);
            Assert.AreEqual(new int2(32, 32), retrievedConfig.WorldSize);
            Assert.AreEqual(RandomizationMode.Full, retrievedConfig.RandomizationMode);
            Assert.AreEqual(0.8f, retrievedConfig.BiomeWeight, 0.001f);
            Assert.IsFalse(retrievedConfig.EnableDebugVisualization);
        }

        [Test]
        public void WorldBootstrapTags_CanBeAddedToEntity()
        {
            var entity = entityManager.CreateEntity();

            // Test in-progress tag
            entityManager.AddComponentData(entity, new CoreInProgress());
            Assert.IsTrue(entityManager.HasComponent<CoreInProgress>(entity));

            // Test complete tag
            entityManager.RemoveComponent<CoreInProgress>(entity);
            var completeTag = new CoreComplete(biomes: 4, districts: 8, sectors: 24, rooms: 120);
            entityManager.AddComponentData(entity, completeTag);

            Assert.IsTrue(entityManager.HasComponent<CoreComplete>(entity));
            var retrievedComplete = entityManager.GetComponentData<CoreComplete>(entity);
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
            var biomeSettings = new BiomeSettings
            {
                CountRange = new int2(1, 3),
                Weight = 1.0f
            };
            var districtSettings = new DistrictSettings
            {
                CountRange = new int2(2, 5),
                MinDistance = 20f,
                Weight = 1.0f
            };
            var sectorSettings = new SectorSettings
            {
                SectorsPerDistrictRange = new int2(1, 4),
                GridSize = new int2(8, 8),
                RoomsPerSectorRange = new int2(1, 8),
                TargetLoopDensity = 0.2f
            };
            var config = new CoreBootstrap(
                seed: 999,
                worldSize: new int2(50, 50),
                randomizationMode: RandomizationMode.None,
                biomeSettings: biomeSettings,
                districtSettings: districtSettings,
                sectorSettings: sectorSettings,
                enableDebugVisualization: true,
                logGenerationSteps: true
            );

            entityManager.AddComponentData(bootstrapEntity, config);

            // Verify the bootstrap entity has the required configuration
            Assert.IsTrue(entityManager.HasComponent<CoreBootstrap>(bootstrapEntity));
            Assert.IsFalse(entityManager.HasComponent<CoreInProgress>(bootstrapEntity));
            Assert.IsFalse(entityManager.HasComponent<CoreComplete>(bootstrapEntity));

            var storedConfig = entityManager.GetComponentData<CoreBootstrap>(bootstrapEntity);
            Assert.AreEqual(999, storedConfig.Seed);
            Assert.AreEqual(new int2(50, 50), storedConfig.WorldSize);
        }

        [Test]
        public void WorldConfiguration_IsCompatibleWithBootstrap()
        {
            // Test that WorldBootstrapConfiguration can coexist with WorldConfiguration
            var entity = entityManager.CreateEntity();

            var worldSettings = new WorldSettings
            {
                Seed = 777,
                WorldSize = new int2(80, 80),
                RandomizationMode = RandomizationMode.Partial
            };
            
            var biomeSettings = new BiomeSettings
            {
                BiomeCountRange = new int2(3, 7),
                BiomeWeight = 1.5f
            };
            
            var districtSettings = new DistrictSettings
            {
                DistrictCountRange = new int2(5, 15),
                DistrictMinDistance = 25f,
                DistrictWeight = 0.9f,
                SectorsPerDistrictRange = new int2(3, 10),
                SectorGridSize = new int2(10, 10)
            };
            
            var debugSettings = new DebugSettings
            {
                EnableDebugVisualization = false,
                LogGenerationSteps = true
            };

            var bootstrapConfig = new CoreBootstrap(
                seed: worldSettings.Seed,
                worldSize: worldSettings.WorldSize,
                randomizationMode: worldSettings.RandomizationMode,
                biomeSettings: new CoreBiomeSettings
                {
                    BiomeCountRange = biomeSettings.BiomeCountRange,
                    BiomeWeight = biomeSettings.BiomeWeight
                },
                districtSettings: new CoreDistrictSettings
                {
                    DistrictCountRange = districtSettings.DistrictCountRange,
                    DistrictMinDistance = districtSettings.DistrictMinDistance,
                    DistrictWeight = districtSettings.DistrictWeight
                },
                sectorSettings: new CoreSectorSettings
                {
                    SectorsPerDistrictRange = districtSettings.SectorsPerDistrictRange,
                    SectorGridSize = districtSettings.SectorGridSize
                },
                roomSettings: new RoomGenerationSettings
                {
                    RoomsPerSectorRange = new int2(4, 15),
                    TargetLoopDensity = 0.7f
                },
                enableDebugVisualization: debugSettings.EnableDebugVisualization,
                logGenerationSteps: debugSettings.LogGenerationSteps
            );
            var worldConfig = new WorldConfiguration
            {
                Seed = 777,
                WorldSize = new int2(80, 80),
                TargetSectors = 150, // Max possible: 15 districts * 10 sectors
                RandomizationMode = RandomizationMode.Partial
            };

            entityManager.AddComponentData(entity, bootstrapConfig);
            entityManager.AddComponentData(entity, worldConfig);

            Assert.IsTrue(entityManager.HasComponent<CoreBootstrap>(entity));
            Assert.IsTrue(entityManager.HasComponent<WorldConfiguration>(entity));

            var retrievedBootstrap = entityManager.GetComponentData<CoreBootstrap>(entity);
            var retrievedWorld = entityManager.GetComponentData<WorldConfiguration>(entity);

            Assert.AreEqual(retrievedBootstrap.Seed, retrievedWorld.Seed);
            Assert.AreEqual(retrievedBootstrap.WorldSize, retrievedWorld.WorldSize);
            Assert.AreEqual(retrievedBootstrap.RandomizationMode, retrievedWorld.RandomizationMode);
        }
    }
}
