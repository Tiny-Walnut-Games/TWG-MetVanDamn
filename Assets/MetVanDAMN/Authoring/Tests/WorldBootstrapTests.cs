using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;
using Unity.Entities;
using Unity.Mathematics;

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
			this.testWorld = new World("World Bootstrap Test World");
			this.entityManager = this.testWorld.EntityManager;
			}

		[TearDown]
		public void TearDown()
			{
			this.testWorld?.Dispose();
			}

		[Test]
		public void WorldBootstrapConfiguration_CanBeCreated()
			{
			var biomeSettings = new TinyWalnutGames.MetVD.Core.BiomeGenerationSettings(
				biomeCountRange: new int2(3, 6),
				biomeWeight: 1.0f
			);
			var districtSettings = new TinyWalnutGames.MetVD.Core.DistrictGenerationSettings(
				districtCountRange: new int2(4, 12),
				districtMinDistance: 15f,
				districtWeight: 1.0f
			);
			var sectorSettings = new TinyWalnutGames.MetVD.Core.SectorGenerationSettings(
				sectorsPerDistrictRange: new int2(2, 8),
				sectorGridSize: new int2(6, 6)
			);
			var roomSettings = new TinyWalnutGames.MetVD.Core.RoomGenerationSettings(
				roomsPerSectorRange: new int2(3, 12),
				targetLoopDensity: 0.3f
			);
			var config = new TinyWalnutGames.MetVD.Core.WorldBootstrapConfiguration(
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
			Entity entity = this.entityManager.CreateEntity();
			var biomeSettings = new TinyWalnutGames.MetVD.Core.BiomeGenerationSettings(
				biomeCountRange: new int2(2, 4),
				biomeWeight: 0.8f
			);
			var districtSettings = new TinyWalnutGames.MetVD.Core.DistrictGenerationSettings(
				districtCountRange: new int2(3, 8),
				districtMinDistance: 10f,
				districtWeight: 1.2f
			);
			var sectorSettings = new TinyWalnutGames.MetVD.Core.SectorGenerationSettings(
				sectorsPerDistrictRange: new int2(1, 6),
				sectorGridSize: new int2(4, 4)
			);
			var roomSettings = new TinyWalnutGames.MetVD.Core.RoomGenerationSettings(
				roomsPerSectorRange: new int2(2, 10),
				targetLoopDensity: 0.5f
			);
			var config = new TinyWalnutGames.MetVD.Core.WorldBootstrapConfiguration(
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

			this.entityManager.AddComponentData(entity, config);

			Assert.IsTrue(this.entityManager.HasComponent<TinyWalnutGames.MetVD.Core.WorldBootstrapConfiguration>(entity));

			WorldBootstrapConfiguration retrievedConfig = this.entityManager.GetComponentData<TinyWalnutGames.MetVD.Core.WorldBootstrapConfiguration>(entity);
			Assert.AreEqual(12345, retrievedConfig.Seed);
			Assert.AreEqual(new int2(32, 32), retrievedConfig.WorldSize);
			Assert.AreEqual(RandomizationMode.Full, retrievedConfig.RandomizationMode);
			Assert.AreEqual(0.8f, retrievedConfig.BiomeSettings.BiomeWeight, 0.001f);
			Assert.IsFalse(retrievedConfig.EnableDebugVisualization);
			}

		[Test]
		public void WorldBootstrapTags_CanBeAddedToEntity()
			{
			Entity entity = this.entityManager.CreateEntity();

			// Test in-progress tag
			this.entityManager.AddComponentData(entity, new WorldBootstrapInProgressTag());
			Assert.IsTrue(this.entityManager.HasComponent<WorldBootstrapInProgressTag>(entity));

			// Test complete tag
			this.entityManager.RemoveComponent<WorldBootstrapInProgressTag>(entity);
			var completeTag = new WorldBootstrapCompleteTag(biomes: 4, districts: 8, sectors: 24, rooms: 120);
			this.entityManager.AddComponentData(entity, completeTag);

			Assert.IsTrue(this.entityManager.HasComponent<WorldBootstrapCompleteTag>(entity));
			WorldBootstrapCompleteTag retrievedComplete = this.entityManager.GetComponentData<WorldBootstrapCompleteTag>(entity);
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

			Entity bootstrapEntity = this.entityManager.CreateEntity();
			var biomeSettings = new BiomeGenerationSettings(
				new int2(1, 3), // CountRange 
				1.0f            // Weight
			);
			var districtSettings = new DistrictGenerationSettings(
				new int2(2, 5), // CountRange
				20f,            // MinDistance
				1.0f            // Weight
			);
			var sectorSettings = new SectorGenerationSettings(
				new int2(1, 4), // SectorsPerDistrictRange
				new int2(8, 8)  // GridSize
			);
			var roomSettings = new RoomGenerationSettings(
				new int2(1, 8), // RoomsPerSectorRange  
				0.2f            // TargetLoopDensity
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

			this.entityManager.AddComponentData(bootstrapEntity, config);

			// Verify the bootstrap entity has the required configuration
			Assert.IsTrue(this.entityManager.HasComponent<WorldBootstrapConfiguration>(bootstrapEntity));
			Assert.IsFalse(this.entityManager.HasComponent<WorldBootstrapInProgressTag>(bootstrapEntity));
			Assert.IsFalse(this.entityManager.HasComponent<WorldBootstrapCompleteTag>(bootstrapEntity));

			WorldBootstrapConfiguration storedConfig = this.entityManager.GetComponentData<WorldBootstrapConfiguration>(bootstrapEntity);
			Assert.AreEqual(999, storedConfig.Seed);
			Assert.AreEqual(new int2(50, 50), storedConfig.WorldSize);
			}

		[Test]
		public void WorldConfiguration_IsCompatibleWithBootstrap()
			{
			// Test that WorldBootstrapConfiguration can coexist with WorldConfiguration
			Entity entity = this.entityManager.CreateEntity();

			var biomeSettings = new BiomeGenerationSettings(
				new int2(3, 7), // BiomeCountRange
				1.5f            // BiomeWeight
			);
			var districtSettings = new DistrictGenerationSettings(
				new int2(5, 15), // DistrictCountRange
				25f,             // DistrictMinDistance  
				0.9f             // DistrictWeight
			);
			var sectorSettings = new SectorGenerationSettings(
				new int2(3, 10), // SectorsPerDistrictRange
				new int2(10, 10) // SectorGridSize
			);
			var roomSettings = new RoomGenerationSettings(
				new int2(4, 15), // RoomsPerSectorRange
				0.7f             // TargetLoopDensity
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
			var worldConfig = new WorldConfiguration
				{
				Seed = 777,
				WorldSize = new int2(80, 80),
				TargetSectors = 150, // Max possible: 15 districts * 10 sectors
				RandomizationMode = RandomizationMode.Partial
				};

			this.entityManager.AddComponentData(entity, bootstrapConfig);
			this.entityManager.AddComponentData(entity, worldConfig);

			Assert.IsTrue(this.entityManager.HasComponent<WorldBootstrapConfiguration>(entity));
			Assert.IsTrue(this.entityManager.HasComponent<WorldConfiguration>(entity));

			WorldBootstrapConfiguration retrievedBootstrap = this.entityManager.GetComponentData<WorldBootstrapConfiguration>(entity);
			WorldConfiguration retrievedWorld = this.entityManager.GetComponentData<WorldConfiguration>(entity);

			Assert.AreEqual(retrievedBootstrap.Seed, retrievedWorld.Seed);
			Assert.AreEqual(retrievedBootstrap.WorldSize, retrievedWorld.WorldSize);
			Assert.AreEqual(retrievedBootstrap.RandomizationMode, retrievedWorld.RandomizationMode);
			}
		}
	}
