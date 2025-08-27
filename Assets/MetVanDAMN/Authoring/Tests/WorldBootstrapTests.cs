using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Shared;
using System;
using System.Reflection;
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
            // Use positional args to avoid named parameter mismatch (roomsPerSectorRange name changed in API)
            var sectorSettings = new CoreSectorSettings(
                new int2(2, 8),          // sectorsPerDistrictRange
                new int2(6, 6)           // sectorGridSize
            );
            // RoomGenerationSettings API changed - use correct parameters
            var roomSettings = new TinyWalnutGames.MetVD.Core.RoomGenerationSettings(
                new int2(3, 12),         // roomsPerSectorRange
                0.3f,                    // targetLoopDensity
                2                        // corridorWidth
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

            // Adapt to flattened API (original top-level properties may have moved into nested settings)
            Assert.AreEqual(new int2(3, 6), WorldBootstrapConfigAccess.GetBiomeCountRange(config));
            Assert.AreEqual(new int2(4, 12), WorldBootstrapConfigAccess.GetDistrictCountRange(config));
            Assert.AreEqual(15f, WorldBootstrapConfigAccess.GetDistrictMinDistance(config), 0.001f);
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
            // Updated to pass all required settings explicitly (constructor overload mismatch fix)
            var config = new CoreBootstrap(
                seed: (int)worldSettings.Seed,
                worldSize: worldSettings.WorldSize,
                randomizationMode: worldSettings.RandomizationMode,
                biomeSettings: new CoreBiomeSettings(biomeSettings.BiomeCountRange, biomeSettings.BiomeWeight),
                districtSettings: new CoreDistrictSettings(districtSettings.DistrictCountRange, districtSettings.DistrictMinDistance, districtSettings.DistrictWeight),
                sectorSettings: new CoreSectorSettings(
                    districtSettings.SectorsPerDistrictRange,
                    districtSettings.SectorGridSize
                ),
                roomSettings: new TinyWalnutGames.MetVD.Core.RoomGenerationSettings(
                    districtSettings.RoomsPerSectorRange,
                    districtSettings.TargetLoopDensity,
                    2 // corridorWidth
                ),
                enableDebugVisualization: debugSettings.EnableDebugVisualization,
                logGenerationSteps: debugSettings.LogGenerationSteps
            );

            entityManager.AddComponentData(entity, config);

            Assert.IsTrue(entityManager.HasComponent<CoreBootstrap>(entity));

            var retrievedConfig = entityManager.GetComponentData<CoreBootstrap>(entity);
            Assert.AreEqual(12345, retrievedConfig.Seed);
            Assert.AreEqual(new int2(32, 32), retrievedConfig.WorldSize);
            Assert.AreEqual(RandomizationMode.Full, retrievedConfig.RandomizationMode);
            Assert.AreEqual(0.8f, WorldBootstrapConfigAccess.GetBiomeWeight(retrievedConfig), 0.001f);
            Assert.IsFalse(retrievedConfig.EnableDebugVisualization);
        }

        [Test]
        public void WorldBootstrapTags_CanBeAddedToEntity()
        {
            var entity = entityManager.CreateEntity();

            entityManager.AddComponentData(entity, new CoreInProgress());
            Assert.IsTrue(entityManager.HasComponent<CoreInProgress>(entity));

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
                DistrictWeight = 1.0f
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
                (
                    sectorSettings.SectorsPerDistrictRange,
                    sectorSettings.GridSize
                ),
                roomSettings: new TinyWalnutGames.MetVD.Core.RoomGenerationSettings
                (
                    sectorSettings.RoomsPerSectorRange,
                    sectorSettings.TargetLoopDensity,
                    2
                ),
                enableDebugVisualization: true,
                logGenerationSteps: true
            );

            entityManager.AddComponentData(bootstrapEntity, config);

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
                SectorGridSize = new int2(10, 10),
                RoomsPerSectorRange = new int2(4, 12),
                TargetLoopDensity = 0.4f
            };
            
            var debugSettings = new DebugSettings
            {
                EnableDebugVisualization = false,
                LogGenerationSteps = true
            };

            var bootstrapConfig = new CoreBootstrap(
                seed: (int)worldSettings.Seed,
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
                (
                    districtSettings.SectorsPerDistrictRange,
                    districtSettings.SectorGridSize
                ),
                roomSettings: new Core.RoomGenerationSettings
                (
                    districtSettings.RoomsPerSectorRange,
                    districtSettings.TargetLoopDensity,
                    2
                ),
                enableDebugVisualization: debugSettings.EnableDebugVisualization,
                logGenerationSteps: debugSettings.LogGenerationSteps
            );
            var worldConfig = new WorldConfiguration
            {
                Seed = 777,
                WorldSize = new int2(80, 80),
                TargetSectors = 150,
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

    /// <summary>
    /// Reflection-based accessors to maintain backward compatibility with renamed / flattened properties
    /// ADDITIVE ONLY – does not modify production code.
    /// </summary>
    internal static class WorldBootstrapConfigAccess
    {
        private static BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static int2 GetBiomeCountRange(CoreBootstrap cfg) =>
            TryGet<int2>(cfg, "BiomeCountRange") ??
            TryNested<int2>(cfg, "BiomeSettings", "BiomeCountRange") ??
            default;

        public static int2 GetDistrictCountRange(CoreBootstrap cfg) =>
            TryGet<int2>(cfg, "DistrictCountRange") ??
            TryNested<int2>(cfg, "DistrictSettings", "DistrictCountRange") ??
            default;

        public static float GetDistrictMinDistance(CoreBootstrap cfg) =>
            TryGet<float>(cfg, "DistrictMinDistance") ??
            TryNested<float>(cfg, "DistrictSettings", "DistrictMinDistance") ??
            0f;

        public static float GetBiomeWeight(CoreBootstrap cfg) =>
            TryGet<float>(cfg, "BiomeWeight") ??
            TryNested<float>(cfg, "BiomeSettings", "BiomeWeight") ??
            0f;

        private static T? TryGet<T>(CoreBootstrap cfg, string name) where T : struct
        {
            var type = cfg.GetType();
            var f = type.GetField(name, Flags);
            if (f != null && f.FieldType == typeof(T)) return (T)f.GetValue(cfg);
            var p = type.GetProperty(name, Flags);
            if (p != null && p.PropertyType == typeof(T)) return (T)p.GetValue(cfg);
            return null;
        }

        private static T? TryNested<T>(CoreBootstrap cfg, string containerName, string memberName) where T : struct
        {
            object container = GetObject(cfg, containerName);
            if (container == null) return null;
            var type = container.GetType();
            var f = type.GetField(memberName, Flags);
            if (f != null && f.FieldType == typeof(T)) return (T)f.GetValue(container);
            var p = type.GetProperty(memberName, Flags);
            if (p != null && p.PropertyType == typeof(T)) return (T)p.GetValue(container);
            return null;
        }

        private static object GetObject(CoreBootstrap cfg, string name)
        {
            var type = cfg.GetType();
            var f = type.GetField(name, Flags);
            if (f != null) return f.GetValue(cfg);
            var p = type.GetProperty(name, Flags);
            if (p != null) return p.GetValue(cfg);
            return null;
        }
    }
}
