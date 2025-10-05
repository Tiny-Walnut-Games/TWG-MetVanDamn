using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Biome;
using CoreBiome = TinyWalnutGames.MetVD.Core.Biome;

namespace TinyWalnutGames.MetVD.Tests.Biome
	{
	/// <summary>
	/// Tests BiomeValidationSystem + buffer setup system: buffer injection, invalid condition recording, and non-record for valid case.
	/// </summary>
	public class BiomeValidationSystemTests
		{
		private SimulationSystemGroup? _simGroup;
		private World? _world;

		[SetUp]
		public void SetUp()
			{
			_world = new World("BiomeValidationTestWorld");
			_simGroup = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
			// Register buffer setup then validation system
			var setupHandle = _world.GetOrCreateSystem<BiomeValidationBufferSetupSystem>();
			var validationHandle = _world.GetOrCreateSystem<BiomeValidationSystem>();
			_simGroup.AddSystemToUpdateList(setupHandle);
			_simGroup.AddSystemToUpdateList(validationHandle);
			_simGroup.SortSystems();
			}

		[TearDown]
		public void TearDown()
			{
			if (_world?.IsCreated ?? false) _world.Dispose();
			}

		private Entity CreateBiomeEntity(CoreBiome biome, int2 coords, byte level = 0)
			{
			var em = _world!.EntityManager;
			var e = em.CreateEntity();
			em.AddComponentData(e, biome);
			em.AddComponentData(e, new NodeId { Value = 1, Coordinates = coords, Level = level, ParentId = 0 });
			return e;
			}

		[Test]
		public void BufferSetup_AddsValidationBuffer()
			{
			var e = CreateBiomeEntity(
				new CoreBiome
					{
					Type = BiomeType.HubArea, PrimaryPolarity = Polarity.Sun, PolarityStrength = 0.5f,
					SecondaryPolarity = Polarity.None, DifficultyModifier = 1f
					}, new int2(0, 0));
			_simGroup!.Update();
			var em = _world!.EntityManager;
			Assert.IsTrue(em.HasBuffer<BiomeValidationRecord>(e), "Validation buffer should be auto-added.");
			Assert.AreEqual(0, em.GetBuffer<BiomeValidationRecord>(e).Length, "No records expected for valid biome.");
			}

		[Test]
		public void PolarityCoherence_Invalid_PrimaryNoneSecondarySet_AddsRecord()
			{
			var e = CreateBiomeEntity(
				new CoreBiome
					{
					Type = BiomeType.TransitionZone, PrimaryPolarity = Polarity.None, SecondaryPolarity = Polarity.Moon,
					PolarityStrength = 0.2f, DifficultyModifier = 1f
					}, new int2(2, 2));
			_simGroup!.Update();
			var buf = _world!.EntityManager.GetBuffer<BiomeValidationRecord>(e);
			Assert.Greater(buf.Length, 0, "Expected validation record for invalid polarity coherence.");
			}

		[Test]
		public void BiomeTypeAssignment_Mismatch_AddsRecord()
			{
			// SolarPlains (Sun) with Moon polarity should be flagged.
			var e = CreateBiomeEntity(
				new CoreBiome
					{
					Type = BiomeType.SolarPlains, PrimaryPolarity = Polarity.Moon, PolarityStrength = 0.5f,
					DifficultyModifier = 1f
					}, new int2(0, 10));
			_simGroup!.Update();
			var buf = _world!.EntityManager.GetBuffer<BiomeValidationRecord>(e);
			Assert.Greater(buf.Length, 0, "Expected record for biome type/polarity mismatch.");
			}

		[Test]
		public void DifficultyProgression_ExtremeValue_AddsRecord()
			{
			var e = CreateBiomeEntity(
				new CoreBiome
					{
					Type = BiomeType.HubArea, PrimaryPolarity = Polarity.Sun, PolarityStrength = 0.5f,
					DifficultyModifier = 4.5f
					}, new int2(1, 1));
			_simGroup!.Update();
			var buf = _world!.EntityManager.GetBuffer<BiomeValidationRecord>(e);
			Assert.Greater(buf.Length, 0, "Expected record for extreme difficulty modifier.");
			}
		}
	}
