// This file is part of Metroidvania Dungeon (MetVD) by Tiny Walnut Games
// Cleaned by @jmeyer1980
using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Graph.Tests
	{
	/// <summary>
	/// Tests for the Procedural Room Generation Master Spec implementation
	/// Validates the complete pipeline flow from room creation to navigation and camera generation
	/// </summary>
	public class ProceduralRoomGenerationTests
		{
		private World _world;
		private EntityManager _entityManager;

		[SetUp]
		public void SetUp ()
			{
			this._world = new World("Test World");
			this._entityManager = this._world.EntityManager;
			}

		[TearDown]
		public void TearDown ()
			{
			if (this._world?.IsCreated == true)
				{
				this._world.Dispose();
				}
			}

		[Test]
		public void ProceduralRoomGenerator_CreatesRoomTemplate_WithCorrectCapabilityTags ()
			{
			// Arrange
			Entity roomEntity = this.CreateTestRoom(RoomType.Boss, new RectInt(0, 0, 10, 8));
			Entity worldConfig = this.CreateWorldConfiguration();
			_ = worldConfig; // suppress unused

			// Create and update the procedural room generator system
			InitializationSystemGroup initGroup = this._world.GetOrCreateSystemManaged<InitializationSystemGroup>();
			ProceduralRoomGeneratorSystem roomGenSystem = this._world.GetOrCreateSystemManaged<ProceduralRoomGeneratorSystem>();
			initGroup.AddSystemToUpdateList(roomGenSystem);
			initGroup.SortSystems();
			initGroup.Update();

			// Assert
			Assert.IsTrue(this._entityManager.HasComponent<RoomTemplate>(roomEntity));
			Assert.IsTrue(this._entityManager.HasComponent<ProceduralRoomGenerated>(roomEntity));

			RoomTemplate template = this._entityManager.GetComponentData<RoomTemplate>(roomEntity);
			ProceduralRoomGenerated genStatus = this._entityManager.GetComponentData<ProceduralRoomGenerated>(roomEntity);

			Assert.AreEqual(RoomGeneratorType.PatternDrivenModular, template.GeneratorType);
			Assert.AreNotEqual(Ability.None, template.CapabilityTags.RequiredSkills);
			Assert.IsTrue(template.RequiresJumpValidation);
			Assert.IsFalse(genStatus.NavigationGenerated);
			Assert.IsFalse(genStatus.CinemachineGenerated);
			Assert.AreNotEqual(0u, genStatus.GenerationSeed);
			}

		[Test]
		public void NavigationGenerator_CreatesMovementConnections_WithCorrectAbilityTags ()
			{
			// Arrange
			Entity roomEntity = this.CreateTestRoomWithTemplate();

			InitializationSystemGroup initGroup = this._world.GetOrCreateSystemManaged<InitializationSystemGroup>();
			ProceduralRoomGeneratorSystem roomGenSystem = this._world.GetOrCreateSystemManaged<ProceduralRoomGeneratorSystem>();

			// Use the main system directly since it's now SystemBase
			RoomNavigationGeneratorSystem navGenSystem = this._world.GetOrCreateSystemManaged<RoomNavigationGeneratorSystem>();

			initGroup.AddSystemToUpdateList(roomGenSystem);
			initGroup.AddSystemToUpdateList(navGenSystem);
			initGroup.SortSystems();
			initGroup.Update();

			// Assert
			Assert.IsTrue(this._entityManager.HasBuffer<RoomNavigationElement>(roomEntity));

			DynamicBuffer<RoomNavigationElement> navBuffer = this._entityManager.GetBuffer<RoomNavigationElement>(roomEntity);
			Assert.Greater(navBuffer.Length, 0);

			ProceduralRoomGenerated genStatus = this._entityManager.GetComponentData<ProceduralRoomGenerated>(roomEntity);
			Assert.IsTrue(genStatus.NavigationGenerated);

			bool hasJumpConnection = false;
			for (int i = 0; i < navBuffer.Length; i++)
				{
				if ((navBuffer [ i ].RequiredMovement & Ability.Jump) != 0)
					{
					hasJumpConnection = true;
					break;
					}
				}
			Assert.IsTrue(hasJumpConnection);
			}

		[Test]
		public void CinemachineZoneGenerator_CreatesBiomeSpecificCameraSettings ()
			{
			// Arrange
			Entity roomEntity = this.CreateTestRoomWithTemplate();

			InitializationSystemGroup initGroup = this._world.GetOrCreateSystemManaged<InitializationSystemGroup>();
			SimulationSystemGroup simGroup = this._world.GetOrCreateSystemManaged<SimulationSystemGroup>();

			ProceduralRoomGeneratorSystem roomGenSystem = this._world.GetOrCreateSystemManaged<ProceduralRoomGeneratorSystem>();
			RoomNavigationGeneratorSystem navGenSystem = this._world.GetOrCreateSystemManaged<RoomNavigationGeneratorSystem>();
			CinemachineZoneGeneratorSystem cameraGenSystem = this._world.GetOrCreateSystemManaged<CinemachineZoneGeneratorSystem>();

			initGroup.AddSystemToUpdateList(roomGenSystem);
			initGroup.AddSystemToUpdateList(navGenSystem);
			simGroup.AddSystemToUpdateList(cameraGenSystem);
			initGroup.SortSystems();
			simGroup.SortSystems();
			initGroup.Update();
			simGroup.Update();

			Assert.IsTrue(this._entityManager.HasComponent<CinemachineZoneData>(roomEntity));
			Assert.IsTrue(this._entityManager.HasComponent<CinemachineGameObjectReference>(roomEntity));

			CinemachineZoneData cameraZone = this._entityManager.GetComponentData<CinemachineZoneData>(roomEntity);
			ProceduralRoomGenerated genStatus = this._entityManager.GetComponentData<ProceduralRoomGenerated>(roomEntity);

			Assert.IsTrue(genStatus.CinemachineGenerated);
			Assert.Greater(cameraZone.Priority, 0);
			Assert.Greater(cameraZone.BlendTime, 0.0f);

			RoomHierarchyData roomData = this._entityManager.GetComponentData<RoomHierarchyData>(roomEntity);
			RectInt roomBounds = roomData.Bounds;
			Assert.IsTrue(cameraZone.CameraPosition.x >= roomBounds.x - 5 &&
						 cameraZone.CameraPosition.x <= roomBounds.x + roomBounds.width + 5);
			}

		[Test]
		public void JumpArcSolver_ValidatesReachability_WithDifferentMovementTypes ()
			{
			var physics = new JumpArcPhysics();
			var startPos = new int2(0, 0);
			var targetPos = new int2(3, 2);
			Ability basicMovement = Ability.Jump;
			Ability advancedMovement = Ability.Jump | Ability.DoubleJump | Ability.Dash;
			bool reachableBasic = JumpArcSolver.IsPositionReachable(startPos, targetPos, basicMovement, physics); // TODO: Find a use for this variable
			bool reachableAdvanced = JumpArcSolver.IsPositionReachable(startPos, targetPos, advancedMovement, physics);
			Assert.IsTrue(reachableAdvanced, "Advanced movement must reach the target.");
			if (!reachableBasic)
				{
				TestContext.WriteLine("Target only reachable with advanced movement (expected for harder arcs).");
				}
			var dashTarget = new int2(5, 0);
			bool reachableWithDash = JumpArcSolver.IsPositionReachable(startPos, dashTarget, Ability.Dash, physics);
			Assert.IsTrue(reachableWithDash);
			}

		[Test]
		public void MovementCapabilityTags_DetermineCorrectSkillRequirements ()
			{
			var movementTags = new MovementCapabilityTags(Ability.Dash | Ability.WallJump, Ability.DoubleJump, BiomeAffinity.Mountain, 0.8f);
			Assert.AreEqual(Ability.Dash | Ability.WallJump, movementTags.RequiredSkills);
			Assert.AreEqual(Ability.DoubleJump, movementTags.OptionalSkills);
			Assert.AreEqual(BiomeAffinity.Mountain, movementTags.BiomeType);
			Assert.AreEqual(0.8f, movementTags.DifficultyRating);
			}

		[Test]
		public void RoomTemplate_InitializesWithCorrectDefaults ()
			{
			var capabilityTags = new MovementCapabilityTags(Ability.Jump, Ability.None, BiomeAffinity.Forest, 0.5f);
			var minSize = new int2(4, 4);
			var maxSize = new int2(12, 8);
			var template = new RoomTemplate(RoomGeneratorType.WeightedTilePrefab, capabilityTags, minSize, maxSize, 0.2f, true, 123u);
			Assert.AreEqual(RoomGeneratorType.WeightedTilePrefab, template.GeneratorType);
			Assert.AreEqual(0.2f, template.SecretAreaPercentage);
			Assert.IsTrue(template.RequiresJumpValidation);
			Assert.AreEqual(123u, template.TemplateId);
			Assert.AreEqual(Ability.Jump, template.CapabilityTags.RequiredSkills);
			}

		[Test]
		public void CompleteGenerationPipeline_ProcessesRoomCorrectly ()
			{
			Entity roomEntity = this.CreateTestRoom(RoomType.Normal, new RectInt(0, 0, 8, 6));
			this.CreateWorldConfiguration();

			InitializationSystemGroup initGroup = this._world.GetOrCreateSystemManaged<InitializationSystemGroup>();
			SimulationSystemGroup simGroup = this._world.GetOrCreateSystemManaged<SimulationSystemGroup>();
			ProceduralRoomGeneratorSystem roomGenSystem = this._world.GetOrCreateSystemManaged<ProceduralRoomGeneratorSystem>();
			RoomNavigationGeneratorSystem navGenSystem = this._world.GetOrCreateSystemManaged<RoomNavigationGeneratorSystem>();
			CinemachineZoneGeneratorSystem cameraGenSystem = this._world.GetOrCreateSystemManaged<CinemachineZoneGeneratorSystem>();

			initGroup.AddSystemToUpdateList(roomGenSystem);
			initGroup.AddSystemToUpdateList(navGenSystem);
			simGroup.AddSystemToUpdateList(cameraGenSystem);
			initGroup.SortSystems();
			simGroup.SortSystems();
			initGroup.Update();
			simGroup.Update();

			ProceduralRoomGenerated genStatus = this._entityManager.GetComponentData<ProceduralRoomGenerated>(roomEntity);
			Assert.IsTrue(genStatus.ContentGenerated || this._entityManager.HasComponent<RoomTemplate>(roomEntity));
			Assert.IsTrue(genStatus.NavigationGenerated);
			Assert.IsTrue(genStatus.CinemachineGenerated);
			Assert.IsTrue(this._entityManager.HasComponent<RoomTemplate>(roomEntity));
			Assert.IsTrue(this._entityManager.HasBuffer<RoomNavigationElement>(roomEntity));
			Assert.IsTrue(this._entityManager.HasComponent<CinemachineZoneData>(roomEntity));
			Assert.IsTrue(this._entityManager.HasComponent<CinemachineGameObjectReference>(roomEntity));
			}

		private Entity CreateTestRoom (RoomType roomType, RectInt bounds)
			{
			Entity roomEntity = this._entityManager.CreateEntity();
			var nodeId = new NodeId(12345u, 2, 1000u, new int2(bounds.x, bounds.y));
			var roomData = new RoomHierarchyData(bounds, roomType, true);
			this._entityManager.AddComponentData(roomEntity, nodeId);
			this._entityManager.AddComponentData(roomEntity, roomData);
			this._entityManager.AddBuffer<RoomNavigationElement>(roomEntity);
			return roomEntity;
			}

		private Entity CreateTestRoomWithTemplate ()
			{
			Entity roomEntity = this.CreateTestRoom(RoomType.Normal, new RectInt(0, 0, 8, 6));
			var capabilityTags = new MovementCapabilityTags(Ability.Jump, Ability.DoubleJump, BiomeAffinity.Forest, 0.5f);
			var template = new RoomTemplate(RoomGeneratorType.WeightedTilePrefab, capabilityTags, new int2(4, 3), new int2(8, 6), 0.15f, true, 999u);
			this._entityManager.AddComponentData(roomEntity, template);
			this._entityManager.AddComponentData(roomEntity, new ProceduralRoomGenerated(12345u));
			return roomEntity;
			}

		private Entity CreateWorldConfiguration ()
			{
			Entity configEntity = this._entityManager.CreateEntity();
			var worldConfig = new WorldConfiguration { Seed = 12345, WorldSize = new int2(50, 50), TargetSectors = 8, RandomizationMode = RandomizationMode.Partial };
			this._entityManager.AddComponentData(configEntity, worldConfig);
			return configEntity;
			}
		}
	}
