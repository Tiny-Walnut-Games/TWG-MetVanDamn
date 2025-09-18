using NUnit.Framework;
using System.Collections;
using System.IO;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Samples;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TinyWalnutGames.MetVD.Authoring.Tests
	{
	/// <summary>
	/// 🧙‍♂️ PHASE 2: SubScene Conversion Validation Tests
	///
	/// These tests validate the SubScene conversion workflow concepts that make
	/// MetVanDAMN entities discoverable by Unity's runtime ECS systems.
	///
	/// NOTE: Due to Unity Editor test limitations, we validate the logic and workflow
	/// rather than creating actual scene files during test execution.
	/// </summary>
#nullable enable
	public class SubSceneConversionTests
		{
		private World testWorld = null!; // assigned in SetUp
		private EntityManager entityManager; // struct assigned in SetUp
		private GameObject smokeTestObject = null!; // assigned in SetUp
		private SmokeTestSceneSetup smokeTestComponent = null!; // assigned in SetUp

		[SetUp]
		public void SetUp()
			{
			// Create test world for validation
			testWorld = new World("SubSceneConversion Test World");
			entityManager = testWorld.EntityManager;

			// Create SmokeTestSceneSetup component for testing
			smokeTestObject = new GameObject("SmokeTestSetup");
			smokeTestComponent = smokeTestObject.AddComponent<SmokeTestSceneSetup>();
			}

		[TearDown]
		public void TearDown()
			{
			if (smokeTestObject != null)
				{
				Object.DestroyImmediate(smokeTestObject);
				}

			if (testWorld?.IsCreated == true)
				{
				testWorld.Dispose();
				}
			}

		[Test]
		public void SubSceneComponent_CanBeCreated_AndConfigured()
			{
			// Arrange - Create SubScene component and test configuration
			var subSceneObject = new GameObject("TestSubScene");
			SubScene subSceneComponent = subSceneObject.AddComponent<SubScene>();

			// Act - Configure SubScene component
			subSceneComponent.AutoLoadScene = true;

			// Assert - Verify SubScene component is properly configured
			Assert.IsNotNull(subSceneComponent, "SubScene component should be created successfully");
			Assert.IsTrue(subSceneComponent.AutoLoadScene, "SubScene should be configured for auto-loading");
			Assert.IsFalse(subSceneComponent.SceneGUID.IsValid, "SubScene GUID should be invalid when no asset assigned");

			TestContext.WriteLine("✅ SubScene component creation and configuration works correctly");
			TestContext.WriteLine("This validates the component setup portion of the SubScene workflow");

			// Cleanup
			Object.DestroyImmediate(subSceneObject);
			}

		[Test]
		public void SubSceneWorkflow_ValidatesEntityCreation_ForDistrictConversion()
			{
			// Arrange - Set up entity creation like the real SubScene workflow would
			World originalWorld = World.DefaultGameObjectInjectionWorld;
			World.DefaultGameObjectInjectionWorld = testWorld;

			try
				{
				// Configure smoke test for district creation
				SetPrivateField("worldSeed", 98765u);
				SetPrivateField("worldSize", new int2(20, 20));
				SetPrivateField("targetSectorCount", 3);
				SetPrivateField("logGenerationSteps", true);

				// Act - Create the entities that would be converted to SubScenes
				InvokePrivateMethod("SetupSmokeTestWorld");

				// Assert - Verify entities exist and are ready for SubScene conversion
				using EntityQuery districtQuery = entityManager.CreateEntityQuery(typeof(DistrictTag), typeof(NodeId));
				int districtCount = districtQuery.CalculateEntityCount();

				Assert.Greater(districtCount, 0, "District entities should be created for SubScene conversion");
				Assert.AreEqual(4, districtCount, "Should create hub + 3 districts = 4 total"); // hub + 3 districts

				// Verify each district has the components needed for SubScene conversion
				Unity.Collections.NativeArray<Entity> districtEntities = districtQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
				foreach (Entity entity in districtEntities)
					{
					Assert.IsTrue(entityManager.HasComponent<NodeId>(entity), "District should have NodeId for SubScene naming");
					Assert.IsTrue(entityManager.HasComponent<WfcState>(entity), "District should have WfcState for processing");
					}
				districtEntities.Dispose();

				TestContext.WriteLine($"✅ Created {districtCount} district entities ready for SubScene conversion");
				TestContext.WriteLine("This validates the entity creation portion of the SubScene workflow");
				}
			finally
				{
				World.DefaultGameObjectInjectionWorld = originalWorld;
				}
			}

		[Test]
		public void SubSceneNamingLogic_GeneratesCorrectNames_ForDistrictEntities()
			{
			// Arrange - Set up test world and create district entities
			World originalWorld = World.DefaultGameObjectInjectionWorld;
			World.DefaultGameObjectInjectionWorld = testWorld;

			try
				{
				SetPrivateField("worldSeed", 12345u);
				SetPrivateField("targetSectorCount", 2);
				InvokePrivateMethod("SetupSmokeTestWorld");

				// Act - Test the naming logic that would be used for SubScene creation
				using EntityQuery districtQuery = entityManager.CreateEntityQuery(typeof(DistrictTag), typeof(NodeId));
				Unity.Collections.NativeArray<Entity> districtEntities = districtQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
				var generatedNames = new System.Collections.Generic.List<string>();

				foreach (Entity entity in districtEntities)
					{
					NodeId nodeId = entityManager.GetComponentData<NodeId>(entity);

					// This is the naming logic that would be used for SubScene creation
					string subSceneName = nodeId.Coordinates.Equals(Unity.Mathematics.int2.zero)
						? "SubScene_HubDistrict"
						: $"SubScene_District_{nodeId.Coordinates.x}_{nodeId.Coordinates.y}";

					generatedNames.Add(subSceneName);
					}
				districtEntities.Dispose();

				// Assert - Verify naming logic produces valid, unique names
				Assert.Greater(generatedNames.Count, 0, "Should generate names for district entities");
				Assert.IsTrue(generatedNames.Contains("SubScene_HubDistrict"), "Should generate hub district name");

				var uniqueNames = new System.Collections.Generic.HashSet<string>(generatedNames);
				Assert.AreEqual(generatedNames.Count, uniqueNames.Count, "All generated names should be unique");

				TestContext.WriteLine($"✅ Generated {generatedNames.Count} unique SubScene names");
				TestContext.WriteLine("Names: " + string.Join(", ", generatedNames));
				TestContext.WriteLine("This validates the naming logic portion of the SubScene workflow");
				}
			finally
				{
				World.DefaultGameObjectInjectionWorld = originalWorld;
				}
			}

		[Test]
		public void SubSceneConversionWorkflow_ValidatesCompleteLogic_WithoutFileSystem()
			{
			// Arrange - Test the complete logical workflow without file system operations
			World originalWorld = World.DefaultGameObjectInjectionWorld;
			World.DefaultGameObjectInjectionWorld = testWorld;

			try
				{
				// Setup scene data
				SetPrivateField("worldSeed", 54321u);
				SetPrivateField("worldSize", new int2(30, 30));
				SetPrivateField("targetSectorCount", 2);
				InvokePrivateMethod("SetupSmokeTestWorld");

				// Act - Simulate the SubScene conversion workflow logic
				using EntityQuery districtQuery = entityManager.CreateEntityQuery(typeof(DistrictTag), typeof(NodeId));
				Unity.Collections.NativeArray<Entity> districtEntities = districtQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

				var conversionResults = new System.Collections.Generic.List<(string name, bool canConvert)>();

				foreach (Entity entity in districtEntities)
					{
					NodeId nodeId = entityManager.GetComponentData<NodeId>(entity);

					// Simulate SubScene conversion validation logic
					string subSceneName = nodeId.Coordinates.Equals(Unity.Mathematics.int2.zero)
						? "SubScene_HubDistrict"
						: $"SubScene_District_{nodeId.Coordinates.x}_{nodeId.Coordinates.y}";

					// Check if entity has required components for conversion
					bool hasRequiredComponents =
						entityManager.HasComponent<NodeId>(entity) &&
						entityManager.HasComponent<WfcState>(entity) &&
						entityManager.HasBuffer<WfcCandidateBufferElement>(entity);

					conversionResults.Add((subSceneName, hasRequiredComponents));
					}
				districtEntities.Dispose();

				// Assert - Verify all entities are ready for SubScene conversion
				Assert.Greater(conversionResults.Count, 0, "Should have entities to convert");

				foreach ((string name, bool canConvert) in conversionResults)
					{
					Assert.IsTrue(canConvert, $"Entity {name} should have all required components for SubScene conversion");
					}

				TestContext.WriteLine($"✅ Validated {conversionResults.Count} entities ready for SubScene conversion");
				TestContext.WriteLine("All entities have required components: NodeId, WfcState, WfcCandidateBufferElement");
				TestContext.WriteLine("This validates the complete SubScene conversion logic workflow");
				}
			finally
				{
				World.DefaultGameObjectInjectionWorld = originalWorld;
				}
			}

		/// <summary>
		/// Helper method to set private fields on SmokeTestSceneSetup via reflection
		/// </summary>
		private void SetPrivateField(string fieldName, object value)
			{
			System.Reflection.FieldInfo field = typeof(SmokeTestSceneSetup).GetField(fieldName,
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			Assert.IsNotNull(field, $"Field {fieldName} not found");
			field.SetValue(smokeTestComponent, value);
			}

		/// <summary>
		/// Helper method to invoke private methods on SmokeTestSceneSetup via reflection
		/// </summary>
		private void InvokePrivateMethod(string methodName)
			{
			System.Reflection.MethodInfo method = typeof(SmokeTestSceneSetup).GetMethod(methodName,
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			Assert.IsNotNull(method, $"Method {methodName} not found");
			method.Invoke(smokeTestComponent, null);
			}
		}
	}
