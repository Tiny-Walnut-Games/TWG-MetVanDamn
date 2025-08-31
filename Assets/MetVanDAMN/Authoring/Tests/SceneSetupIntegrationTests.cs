using NUnit.Framework;
using System.Collections;
using System.Reflection;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Samples;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

namespace TinyWalnutGames.MetVD.Authoring.Tests
	{
	/// <summary>
	/// Integration tests for scene setup and ECS system pipeline
	/// Tests the complete flow from scene setup to ECS processing
	/// </summary>
	public class SceneSetupIntegrationTests
		{
		private World testWorld;
		private EntityManager entityManager;
		private GameObject sceneSetupObject;
		private SmokeTestSceneSetup sceneSetup;

		[SetUp]
		public void SetUp ()
			{
			// Create a clean test world for integration testing
			this.testWorld = new World("Integration Test World");
			this.entityManager = this.testWorld.EntityManager;

			// Create scene setup component
			this.sceneSetupObject = new GameObject("SceneSetup");
			this.sceneSetup = this.sceneSetupObject.AddComponent<SmokeTestSceneSetup>();

			// Override the default world injection to use our test world
			this.OverrideWorldInjection();
			}

		[TearDown]
		public void TearDown ()
			{
			if (this.sceneSetupObject != null)
				{
				Object.DestroyImmediate(this.sceneSetupObject);
				}

			if (this.testWorld?.IsCreated == true)
				{
				this.testWorld.Dispose();
				}
			}

		[UnityTest]
		public IEnumerator SceneSetup_FullWorkflow_CreatesValidWorld ()
			{
			// Configure scene setup with test parameters
			this.SetField("worldSeed", 12345u);
			this.SetField("worldSize", new int2(30, 30));
			this.SetField("targetSectorCount", 6);
			this.SetField("enableDebugVisualization", false); // Disable for clean testing
			this.SetField("logGenerationSteps", true);

			// Start the scene setup process
			this.sceneSetup.enabled = true;

			yield return null; // Wait for Start() to be called
			yield return null; // Wait one more frame for potential system updates

			// Verify world configuration was created
			this.VerifyWorldConfiguration();

			// Verify districts were created
			this.VerifyDistrictConfiguration();

			// Verify biome fields were created  
			this.VerifyBiomeFieldConfiguration();

			yield return null;
			}

		[Test]
		public void SceneSetup_WorldConfiguration_IntegratesWithECSSystems ()
			{
			// Test that the world configuration created by scene setup
			// is compatible with ECS systems that might process it

			// Setup the scene manually (avoiding Start() dependency)
			this.SetField("defaultWorld", this.testWorld);
			this.SetField("entityManager", this.entityManager);
			this.InvokeMethod("SetupSmokeTestWorld");

			// Verify systems can query the created entities
			using EntityQuery seedQuery = this.entityManager.CreateEntityQuery(typeof(WorldSeed));
			using EntityQuery boundsQuery = this.entityManager.CreateEntityQuery(typeof(WorldBounds));
			using EntityQuery configQuery = this.entityManager.CreateEntityQuery(typeof(WorldGenerationConfig));
			using EntityQuery nodeQuery = this.entityManager.CreateEntityQuery(typeof(NodeId));
			using EntityQuery polarityQuery = this.entityManager.CreateEntityQuery(typeof(PolarityFieldData));

			Assert.AreEqual(1, seedQuery.CalculateEntityCount(), "WorldSeed singleton should exist");
			Assert.AreEqual(1, boundsQuery.CalculateEntityCount(), "WorldBounds singleton should exist");
			Assert.AreEqual(1, configQuery.CalculateEntityCount(), "WorldGenerationConfig singleton should exist");
			Assert.Greater(nodeQuery.CalculateEntityCount(), 0, "Districts should have NodeId components");
			Assert.AreEqual(4, polarityQuery.CalculateEntityCount(), "Four polarity fields should exist");

			// Test that entities have the expected component combinations
			NativeArray<Entity> nodeEntities = nodeQuery.ToEntityArray(Allocator.Temp);
			foreach (Entity entity in nodeEntities)
				{
				Assert.IsTrue(this.entityManager.HasComponent<WfcState>(entity),
					"All district entities should have WfcState for WFC processing");
				Assert.IsTrue(this.entityManager.HasBuffer<WfcCandidateBufferElement>(entity),
					"All district entities should have WfcCandidateBuffer for constraint solving");
				Assert.IsTrue(this.entityManager.HasBuffer<ConnectionBufferElement>(entity),
					"All district entities should have ConnectionBuffer for navigation");
				}
			nodeEntities.Dispose();
			}

		[Test]
		public void SceneSetup_DistrictHierarchy_SupportsNestedSectorGeneration ()
			{
			// Test that district setup supports the expected hierarchical structure
			this.SetField("defaultWorld", this.testWorld);
			this.SetField("entityManager", this.entityManager);
			this.SetField("targetSectorCount", 9); // 3x3 grid for predictable testing

			this.InvokeMethod("CreateDistrictEntities");

			using EntityQuery query = this.entityManager.CreateEntityQuery(typeof(NodeId), typeof(SectorRefinementData));
			NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

			// Verify sector refinement data is present on non-hub districts
			foreach (Entity entity in entities)
				{
				SectorRefinementData refinementData = this.entityManager.GetComponentData<SectorRefinementData>(entity);
				// SectorRefinementData has TargetLoopDensity property
				Assert.AreEqual(0.3f, refinementData.TargetLoopDensity, 0.001f,
					"Sector refinement target loop density should be set to 0.3");

				// Verify gate condition buffer exists for progression gating
				Assert.IsTrue(this.entityManager.HasBuffer<GateConditionBufferElement>(entity),
					"Districts should have gate condition buffers for progression");
				}

			entities.Dispose();
			}

		[Test]
		public void SceneSetup_PolarityFieldLayout_SupportsComplexBiomeGeneration ()
			{
			// Test that polarity field layout supports complex biome interactions
			this.SetField("defaultWorld", this.testWorld);
			this.SetField("entityManager", this.entityManager);
			this.SetField("biomeTransitionRadius", 20.0f);

			this.InvokeMethod("CreateBiomeFieldEntities");

			using EntityQuery query = this.entityManager.CreateEntityQuery(typeof(PolarityFieldData));
			NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

			// Verify field coverage and overlap patterns
			var fields = new System.Collections.Generic.List<(Polarity polarity, float2 center, float radius)>();
			foreach (Entity entity in entities)
				{
				PolarityFieldData data = this.entityManager.GetComponentData<PolarityFieldData>(entity);
				fields.Add((data.Polarity, data.Center, data.Radius));
				}

			entities.Dispose();

			// Verify opposing polarities are positioned for interesting transitions
			// ⚠ Intent ⚠ - @jmeyer1980 - all fields here are IL for legibility, not performance
			(Polarity polarity, float2 center, float radius) = fields.Find(f => f.polarity == Polarity.Sun);
			(Polarity polarity, float2 center, float radius) moonField = fields.Find(f => f.polarity == Polarity.Moon);
			(Polarity polarity, float2 center, float radius) heatField = fields.Find(f => f.polarity == Polarity.Heat);
			(Polarity polarity, float2 center, float radius) coldField = fields.Find(f => f.polarity == Polarity.Cold);

			// Check diagonal opposition for Sun/Moon
			float sunMoonDistance = math.distance(center, moonField.center);
			Assert.Greater(sunMoonDistance, 25.0f, "Sun and Moon fields should be well-separated");

			// Check Heat/Cold opposition
			float heatColdDistance = math.distance(heatField.center, coldField.center);
			Assert.Greater(heatColdDistance, 25.0f, "Heat and Cold fields should be well-separated");

			// Verify field strength supports gradient transitions
			Assert.AreEqual(20.0f, radius, 0.001f, "All fields should have configured radius");
			}

		[Test]
		public void SceneSetup_ConfigurationConsistency_MaintainsDataIntegrity ()
			{
			// Test that all configuration values remain consistent across setup
			this.SetField("defaultWorld", this.testWorld);
			this.SetField("entityManager", this.entityManager);

			uint testSeed = 98765u;
			int2 testWorldSize = new(60, 40);
			int testSectorCount = 12;
			float testTransitionRadius = 25.0f;

			this.SetField("worldSeed", testSeed);
			this.SetField("worldSize", testWorldSize);
			this.SetField("targetSectorCount", testSectorCount);
			this.SetField("biomeTransitionRadius", testTransitionRadius);

			this.InvokeMethod("SetupSmokeTestWorld");

			// Verify WorldSeed matches
			using EntityQuery seedQuery = this.entityManager.CreateEntityQuery(typeof(WorldSeed));
			WorldSeed worldSeed = seedQuery.GetSingleton<WorldSeed>();
			Assert.AreEqual(testSeed, worldSeed.Value, "WorldSeed should match configured value");

			// Verify WorldBounds matches
			using EntityQuery boundsQuery = this.entityManager.CreateEntityQuery(typeof(WorldBounds));
			WorldBounds worldBounds = boundsQuery.GetSingleton<WorldBounds>();
			Assert.AreEqual(new int2(-30, -20), worldBounds.Min, "WorldBounds min should be half world size negative");
			Assert.AreEqual(new int2(30, 20), worldBounds.Max, "WorldBounds max should be half world size positive");

			// Verify WorldGenerationConfig matches
			using EntityQuery configQuery = this.entityManager.CreateEntityQuery(typeof(WorldGenerationConfig));
			WorldGenerationConfig genConfig = configQuery.GetSingleton<WorldGenerationConfig>();
			Assert.AreEqual(testSectorCount, genConfig.TargetSectorCount, "Generation config should match target sector count");
			Assert.AreEqual(testTransitionRadius, genConfig.BiomeTransitionRadius, 0.001f, "Biome transition radius should match");

			// Verify polarity field radius matches
			using EntityQuery polarityQuery = this.entityManager.CreateEntityQuery(typeof(PolarityFieldData));
			NativeArray<Entity> polarityEntities = polarityQuery.ToEntityArray(Allocator.Temp);
			foreach (Entity entity in polarityEntities)
				{
				PolarityFieldData data = this.entityManager.GetComponentData<PolarityFieldData>(entity);
				Assert.AreEqual(testTransitionRadius, data.Radius, 0.001f, "Polarity field radius should match transition radius");
				}
			polarityEntities.Dispose();
			}

		private void VerifyWorldConfiguration ()
			{
			using EntityQuery query = this.entityManager.CreateEntityQuery(typeof(WorldSeed), typeof(WorldBounds), typeof(WorldGenerationConfig));
			Assert.AreEqual(1, query.CalculateEntityCount(), "Should have exactly one world configuration entity");

			Entity configEntity = query.GetSingletonEntity();

			WorldSeed seed = this.entityManager.GetComponentData<WorldSeed>(configEntity);
			WorldBounds bounds = this.entityManager.GetComponentData<WorldBounds>(configEntity);
			WorldGenerationConfig config = this.entityManager.GetComponentData<WorldGenerationConfig>(configEntity);

			Assert.AreEqual(12345u, seed.Value);
			Assert.AreEqual(new int2(-15, -15), bounds.Min);
			Assert.AreEqual(new int2(15, 15), bounds.Max);
			Assert.AreEqual(6, config.TargetSectorCount);
			Assert.AreEqual(24, config.MaxDistrictCount); // targetSectorCount * 4
			}

		private void VerifyDistrictConfiguration ()
			{
			using EntityQuery query = this.entityManager.CreateEntityQuery(typeof(NodeId), typeof(WfcState));
			int districtCount = query.CalculateEntityCount();
			Assert.AreEqual(7, districtCount, "Should create hub + 6 districts = 7 total");

			// Verify hub exists
			NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
			bool foundHub = false;
			foreach (Entity entity in entities)
				{
				NodeId nodeId = this.entityManager.GetComponentData<NodeId>(entity);
				if (nodeId._value == 0 && nodeId.Coordinates.Equals(int2.zero))
					{
					foundHub = true;
					break;
					}
				}
			entities.Dispose();
			Assert.IsTrue(foundHub, "Hub district should exist at origin");
			}

		private void VerifyBiomeFieldConfiguration ()
			{
			using EntityQuery query = this.entityManager.CreateEntityQuery(typeof(PolarityFieldData));
			Assert.AreEqual(4, query.CalculateEntityCount(), "Should create 4 polarity fields");

			NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
			var foundPolarities = new System.Collections.Generic.HashSet<Polarity>();
			foreach (Entity entity in entities)
				{
				PolarityFieldData data = this.entityManager.GetComponentData<PolarityFieldData>(entity);
				foundPolarities.Add(data.Polarity);
				}
			entities.Dispose();

			Assert.IsTrue(foundPolarities.SetEquals(new [ ] { Polarity.Sun, Polarity.Moon, Polarity.Heat, Polarity.Cold }),
				"All expected polarity fields should be created");
			}

		private void OverrideWorldInjection ()
			{
			// For integration testing, we want the scene setup to use our test world
			// This is handled in individual tests by setting the private fields
			}

		private void SetField (string fieldName, object value)
			{
			FieldInfo field = typeof(SmokeTestSceneSetup).GetField(fieldName,
				BindingFlags.NonPublic | BindingFlags.Instance);
			field?.SetValue(this.sceneSetup, value);
			}

		private void InvokeMethod (string methodName)
			{
			MethodInfo method = typeof(SmokeTestSceneSetup).GetMethod(methodName,
				BindingFlags.NonPublic | BindingFlags.Instance);
			method?.Invoke(this.sceneSetup, null);
			}
		}
	}
