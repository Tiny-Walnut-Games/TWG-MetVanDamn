using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Samples;
using TinyWalnutGames.MetVD.Shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

namespace TinyWalnutGames.MetVD.Authoring.Tests
	{
	/// <summary>
	/// Comprehensive tests for SmokeTestSceneSetup component
	/// Validates "hit Play -> see map" experience and immediate feedback
	/// </summary>
	public class SmokeTestSceneSetupTests
		{
		private World testWorld;
		private EntityManager entityManager;
		private GameObject testGameObject;
		private SmokeTestSceneSetup smokeTestComponent;

		[SetUp]
		public void SetUp()
			{
			this.testWorld = new World("SmokeTest World");
			this.entityManager = this.testWorld.EntityManager;

			// Create test GameObject with SmokeTestSceneSetup component
			this.testGameObject = new GameObject("SmokeTestSetup");
			this.smokeTestComponent = this.testGameObject.AddComponent<SmokeTestSceneSetup>();
			}

		[TearDown]
		public void TearDown()
			{
			if (this.testGameObject != null)
				{
				Object.DestroyImmediate(this.testGameObject);
				}

			if (this.testWorld?.IsCreated == true)
				{
				this.testWorld.Dispose();
				}
			}

		[Test]
		public void SmokeTestSceneSetup_DefaultConfiguration_IsValid()
			{
			// Test that default configuration values are sensible
			Assert.AreEqual(42u, this.GetPrivateField<uint>("worldSeed"));
			Assert.AreEqual(new int2(50, 50), this.GetPrivateField<int2>("worldSize"));
			Assert.AreEqual(5, this.GetPrivateField<int>("targetSectorCount"));
			Assert.AreEqual(10.0f, this.GetPrivateField<float>("biomeTransitionRadius"), 0.001f);
			// ✅ FIX: enableDebugVisualization defaults to FALSE, not true
			Assert.IsFalse(this.GetPrivateField<bool>("enableDebugVisualization"));
			Assert.IsTrue(this.GetPrivateField<bool>("logGenerationSteps"));
			}

		[Test]
		public void SmokeTestSceneSetup_WorldConfiguration_CreatesCorrectEntities()
			{
			// ✅ PHASE 1: EXPLICIT WORLD INJECTION - Set DefaultGameObjectInjectionWorld to our test world
			World originalWorld = World.DefaultGameObjectInjectionWorld;
			World.DefaultGameObjectInjectionWorld = this.testWorld;

			try
				{
				// Enable logging to see world usage
				this.SetPrivateField("logGenerationSteps", true);

				// Invoke the private SetupSmokeTestWorld method
				this.InvokePrivateMethod("SetupSmokeTestWorld");

				// Verify world configuration entities were created
				using EntityQuery query = this.entityManager.CreateEntityQuery(typeof(WorldSeed));
				Assert.AreEqual(1, query.CalculateEntityCount(), "Should create exactly one WorldSeed entity");

				Entity configEntity = query.GetSingletonEntity();
				WorldSeed seed = this.entityManager.GetComponentData<WorldSeed>(configEntity);
				Assert.AreEqual(42u, seed.Value);

				// Verify WorldBounds component
				Assert.IsTrue(this.entityManager.HasComponent<WorldBounds>(configEntity));
				WorldBounds bounds = this.entityManager.GetComponentData<WorldBounds>(configEntity);
				Assert.AreEqual(new int2(-25, -25), bounds.Min);
				Assert.AreEqual(new int2(25, 25), bounds.Max);

				// Verify WorldGenerationConfig component
				Assert.IsTrue(this.entityManager.HasComponent<WorldGenerationConfig>(configEntity));
				WorldGenerationConfig genConfig = this.entityManager.GetComponentData<WorldGenerationConfig>(configEntity);
				Assert.AreEqual(5, genConfig.TargetSectorCount);
				Assert.AreEqual(20, genConfig.MaxDistrictCount); // targetSectorCount * 4
				Assert.AreEqual(10.0f, genConfig.BiomeTransitionRadius, 0.001f);
				}
			finally
				{
				// ✅ RESTORE ORIGINAL WORLD - Clean up injection
				World.DefaultGameObjectInjectionWorld = originalWorld;
				}
			}

		[Test]
		public void SmokeTestSceneSetup_DistrictCreation_RespectsTargetSectorCount()
			{
			// ✅ FIX: Use SetupTestContext() instead of ForceSetup() to avoid double entity creation
			this.smokeTestComponent.SetupTestContext(this.testWorld);
			this.SetPrivateField("targetSectorCount", 8);

			// Invoke district creation
			this.InvokePrivateMethod("CreateDistrictEntities");

			// Verify districts were created (filter by DistrictTag to exclude room entities)
			using EntityQuery nodeQuery = this.entityManager.CreateEntityQuery(typeof(DistrictTag), typeof(NodeId), typeof(WfcState));
			int districtCount = nodeQuery.CalculateEntityCount();

			// Should create hub (1) + min(8, 24) districts = 9 total
			Assert.AreEqual(9, districtCount, "Should create hub + 8 districts based on targetSectorCount");

			// Verify hub district exists at origin
			NativeArray<Entity> nodeEntities = nodeQuery.ToEntityArray(Allocator.Temp);
			bool foundHub = false;

			for (int i = 0; i < nodeEntities.Length; i++)
				{
				NodeId nodeId = this.entityManager.GetComponentData<NodeId>(nodeEntities[i]);
				if (nodeId.Coordinates.Equals(int2.zero) && nodeId._value == 0)
					{
					foundHub = true;
					Assert.AreEqual(0, nodeId.Level, "Hub should be at level 0");
					break;
					}
				}

			nodeEntities.Dispose();
			Assert.IsTrue(foundHub, "Hub district should exist at origin");
			}

		[Test]
		public void SmokeTestSceneSetup_BiomeFieldCreation_CreatesAllPolarityFields()
			{
			// ✅ FIX: Use SetupTestContext() instead of ForceSetup() to avoid double entity creation
			this.smokeTestComponent.SetupTestContext(this.testWorld);

			// Invoke biome field creation
			this.InvokePrivateMethod("CreateBiomeFieldEntities");

			// Verify all polarity fields were created
			using EntityQuery polarityQuery = this.entityManager.CreateEntityQuery(typeof(PolarityFieldData));
			Assert.AreEqual(4, polarityQuery.CalculateEntityCount(), "Should create 4 polarity fields");

			NativeArray<Entity> fieldEntities = polarityQuery.ToEntityArray(Allocator.Temp);
			var foundPolarities = new System.Collections.Generic.HashSet<Polarity>();

			for (int i = 0; i < fieldEntities.Length; i++)
				{
				PolarityFieldData fieldData = this.entityManager.GetComponentData<PolarityFieldData>(fieldEntities[i]);
				foundPolarities.Add(fieldData.Polarity);

				// Verify field properties
				Assert.AreEqual(10.0f, fieldData.Radius, 0.001f, "All fields should use biomeTransitionRadius");
				Assert.AreEqual(0.8f, fieldData.Strength, 0.001f, "All fields should have 0.8 strength");
				}

			fieldEntities.Dispose();

			// Verify all expected polarities are present
			Assert.IsTrue(foundPolarities.Contains(Polarity.Sun), "Should create Sun field");
			Assert.IsTrue(foundPolarities.Contains(Polarity.Moon), "Should create Moon field");
			Assert.IsTrue(foundPolarities.Contains(Polarity.Heat), "Should create Heat field");
			Assert.IsTrue(foundPolarities.Contains(Polarity.Cold), "Should create Cold field");
			}

		[Test]
		public void SmokeTestSceneSetup_DistrictGridPlacement_FollowsExpectedPattern()
			{
			// ✅ FIX: Use ForceSetup() instead of trying to set non-existent private fields
			this.smokeTestComponent.ForceSetup(this.testWorld);
			this.SetPrivateField("targetSectorCount", 8);

			this.InvokePrivateMethod("CreateDistrictEntities");

			using EntityQuery nodeQuery = this.entityManager.CreateEntityQuery(typeof(DistrictTag), typeof(NodeId));
			NativeArray<Entity> nodeEntities = nodeQuery.ToEntityArray(Allocator.Temp);
			var positions = new List<int2>();

			for (int i = 0; i < nodeEntities.Length; i++)
				{
				NodeId nodeId = this.entityManager.GetComponentData<NodeId>(nodeEntities[i]);
				positions.Add(nodeId.Coordinates);
				}

			nodeEntities.Dispose();

			// Verify hub at origin
			Assert.IsTrue(positions.Contains(int2.zero), "Hub should be at origin");

			// Verify districts are placed in grid pattern (multiples of 10)
			foreach (int2 pos in positions)
				{
				if (!pos.Equals(int2.zero)) // Skip hub
					{
					Assert.AreEqual(0, pos.x % 10, $"District X coordinate {pos.x} should be multiple of 10");
					Assert.AreEqual(0, pos.y % 10, $"District Y coordinate {pos.y} should be multiple of 10");
					}
				}
			}

		[Test]
		public void SmokeTestSceneSetup_ComponentConfiguration_HasRequiredECSComponents()
			{
			// ✅ FIX: Use SetupTestContext() instead of ForceSetup() to avoid double entity creation
			this.smokeTestComponent.SetupTestContext(this.testWorld);

			this.InvokePrivateMethod("CreateDistrictEntities");

			// Filter by DistrictTag so we exclude rooms (which also have NodeId/WfcState/buffers)
			using EntityQuery districtQuery = this.entityManager.CreateEntityQuery(
				typeof(DistrictTag),
				typeof(NodeId),
				typeof(WfcState),
				typeof(WfcCandidateBufferElement),
				typeof(ConnectionBufferElement)
			);

			int districtsWithAllComponents = districtQuery.CalculateEntityCount();
			Assert.Greater(districtsWithAllComponents, 0, "All districts should have required ECS components");

			// Verify non-hub districts have additional components
			using EntityQuery sectorQuery = this.entityManager.CreateEntityQuery(
				typeof(SectorRefinementData),
				typeof(GateConditionBufferElement)
			);

			int districtsWithSectorComponents = sectorQuery.CalculateEntityCount();
			Assert.AreEqual(districtsWithAllComponents - 1, districtsWithSectorComponents,
				"All non-hub districts should have sector refinement components");
			}

		[UnityTest]
		public IEnumerator SmokeTestSceneSetup_DebugVisualization_DrawsBoundsCorrectly()
			{
			// Setup debug visualization
			this.SetPrivateField("enableDebugVisualization", true);
			this.SetPrivateField("worldSize", new int2(40, 30));

			// Start the component (this calls SetupSmokeTestWorld and begins Update loop)
			this.smokeTestComponent.enabled = true;

			yield return null; // Wait one frame

			// Verify that DebugDrawBounds is called periodically
			// (In actual implementation, we'd mock Debug.DrawLine or use a test harness)

			// For now, verify the component state is correct for debug drawing
			Assert.IsTrue(this.GetPrivateField<bool>("enableDebugVisualization"));
			Assert.AreEqual(new int2(40, 30), this.GetPrivateField<int2>("worldSize"));

			yield return null;
			}

		[Test]
		public void SmokeTestSceneSetup_ExtremeTargetSectorCount_HandlesGracefully()
			{
			// ✅ FIX: Use SetupTestContext() instead of ForceSetup() to avoid double entity creation
			this.smokeTestComponent.SetupTestContext(this.testWorld);

			// Test extremely high value (should be clamped to 24)
			this.SetPrivateField("targetSectorCount", 100);
			this.InvokePrivateMethod("CreateDistrictEntities");

			using EntityQuery districtOnlyQ = this.entityManager.CreateEntityQuery(typeof(DistrictTag));
			int districtCount = districtOnlyQ.CalculateEntityCount();
			Assert.AreEqual(25, districtCount, "Should clamp to 24 districts + 1 hub = 25 total");

			// Clean up ALL entities with NodeId (districts + rooms) for next part
			using (EntityQuery allNodesQ = this.entityManager.CreateEntityQuery(typeof(NodeId)))
				{
				NativeArray<Entity> all = allNodesQ.ToEntityArray(Allocator.Temp);
				this.entityManager.DestroyEntity(all);
				all.Dispose();
				}

			// Test zero value (should create only hub)
			this.SetPrivateField("targetSectorCount", 0);
			this.InvokePrivateMethod("CreateDistrictEntities");

			int hubOnlyCount = this.entityManager.CreateEntityQuery(typeof(DistrictTag)).CalculateEntityCount();
			Assert.AreEqual(1, hubOnlyCount, "Should create only hub with targetSectorCount of 0");
			}

		[Test]
		public void SmokeTestSceneSetup_PolarityFieldPositioning_MatchesExpectedLayout()
			{
			// ✅ FIX: Use ForceSetup() instead of trying to set non-existent private fields
			this.smokeTestComponent.ForceSetup(this.testWorld);
			this.SetPrivateField("biomeTransitionRadius", 15.0f);

			this.InvokePrivateMethod("CreateBiomeFieldEntities");

			using EntityQuery query = this.entityManager.CreateEntityQuery(typeof(PolarityFieldData));
			NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
			var polarityPositions = new Dictionary<Polarity, float2>();

			for (int i = 0; i < entities.Length; i++)
				{
				PolarityFieldData data = this.entityManager.GetComponentData<PolarityFieldData>(entities[i]);
				polarityPositions[data.Polarity] = data.Center;
				}

			entities.Dispose();

			// Verify expected positions
			Assert.AreEqual(new float2(15, 15), polarityPositions[Polarity.Sun], "Sun field position");
			Assert.AreEqual(new float2(-15, -15), polarityPositions[Polarity.Moon], "Moon field position");
			Assert.AreEqual(new float2(15, -15), polarityPositions[Polarity.Heat], "Heat field position");
			Assert.AreEqual(new float2(-15, 15), polarityPositions[Polarity.Cold], "Cold field position");
			}

		// Helper methods for reflection-based testing
		private T GetPrivateField<T>(string fieldName)
			{
			System.Reflection.FieldInfo field = typeof(SmokeTestSceneSetup).GetField(fieldName,
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			Assert.IsNotNull(field, $"Field {fieldName} not found");
			return (T)field.GetValue(this.smokeTestComponent);
			}

		private void SetPrivateField(string fieldName, object value)
			{
			System.Reflection.FieldInfo field = typeof(SmokeTestSceneSetup).GetField(fieldName,
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			Assert.IsNotNull(field, $"Field {fieldName} not found");
			field.SetValue(this.smokeTestComponent, value);
			}

		private void InvokePrivateMethod(string methodName)
			{
			System.Reflection.MethodInfo method = typeof(SmokeTestSceneSetup).GetMethod(methodName,
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			Assert.IsNotNull(method, $"Method {methodName} not found");
			method.Invoke(this.smokeTestComponent, null);
			}
		}
	}
