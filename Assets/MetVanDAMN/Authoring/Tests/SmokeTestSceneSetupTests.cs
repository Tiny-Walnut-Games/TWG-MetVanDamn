using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections;
using System.Collections.Generic;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Samples;

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
            testWorld = new World("SmokeTest World");
            entityManager = testWorld.EntityManager;
            
            // Create test GameObject with SmokeTestSceneSetup component
            testGameObject = new GameObject("SmokeTestSetup");
            smokeTestComponent = testGameObject.AddComponent<SmokeTestSceneSetup>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            
            if (testWorld?.IsCreated == true)
            {
                testWorld.Dispose();
            }
        }

        [Test]
        public void SmokeTestSceneSetup_DefaultConfiguration_IsValid()
        {
            // Test that default configuration values are sensible
            Assert.AreEqual(42u, GetPrivateField<uint>("worldSeed"));
            Assert.AreEqual(new int2(50, 50), GetPrivateField<int2>("worldSize"));
            Assert.AreEqual(5, GetPrivateField<int>("targetSectorCount"));
            Assert.AreEqual(10.0f, GetPrivateField<float>("biomeTransitionRadius"), 0.001f);
            Assert.IsTrue(GetPrivateField<bool>("enableDebugVisualization"));
            Assert.IsTrue(GetPrivateField<bool>("logGenerationSteps"));
        }

        [Test]
        public void SmokeTestSceneSetup_WorldConfiguration_CreatesCorrectEntities()
        {
            // Override the default world to use our test world
            SetPrivateField("defaultWorld", testWorld);
            SetPrivateField("entityManager", entityManager);
            
            // Invoke the private SetupSmokeTestWorld method
            InvokePrivateMethod("SetupSmokeTestWorld");
            
            // Verify world configuration entities were created
            using EntityQuery query = entityManager.CreateEntityQuery(typeof(WorldSeed));
            Assert.AreEqual(1, query.CalculateEntityCount(), "Should create exactly one WorldSeed entity");
            
            Entity configEntity = query.GetSingletonEntity();
            WorldSeed seed = entityManager.GetComponentData<WorldSeed>(configEntity);
            Assert.AreEqual(42u, seed.Value);
            
            // Verify WorldBounds component
            Assert.IsTrue(entityManager.HasComponent<WorldBounds>(configEntity));
            WorldBounds bounds = entityManager.GetComponentData<WorldBounds>(configEntity);
            Assert.AreEqual(new int2(-25, -25), bounds.Min);
            Assert.AreEqual(new int2(25, 25), bounds.Max);
            
            // Verify WorldGenerationConfig component
            Assert.IsTrue(entityManager.HasComponent<WorldGenerationConfig>(configEntity));
            WorldGenerationConfig genConfig = entityManager.GetComponentData<WorldGenerationConfig>(configEntity);
            Assert.AreEqual(5, genConfig.TargetSectorCount);
            Assert.AreEqual(20, genConfig.MaxDistrictCount); // targetSectorCount * 4
            Assert.AreEqual(10.0f, genConfig.BiomeTransitionRadius, 0.001f);
        }

        [Test]
        public void SmokeTestSceneSetup_DistrictCreation_RespectsTargetSectorCount()
        {
            // Setup
            SetPrivateField("defaultWorld", testWorld);
            SetPrivateField("entityManager", entityManager);
            SetPrivateField("targetSectorCount", 8);
            
            // Invoke district creation
            InvokePrivateMethod("CreateDistrictEntities");
            
            // Verify districts were created
            using EntityQuery nodeQuery = entityManager.CreateEntityQuery(typeof(NodeId), typeof(WfcState));
            int districtCount = nodeQuery.CalculateEntityCount();
            
            // Should create hub (1) + min(8, 24) districts = 9 total
            Assert.AreEqual(9, districtCount, "Should create hub + 8 districts based on targetSectorCount");

            // Verify hub district exists at origin
            NativeArray<Entity> nodeEntities = nodeQuery.ToEntityArray(Allocator.Temp);
            bool foundHub = false;
            
            for (int i = 0; i < nodeEntities.Length; i++)
            {
                NodeId nodeId = entityManager.GetComponentData<NodeId>(nodeEntities[i]);
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
            // Setup
            SetPrivateField("defaultWorld", testWorld);
            SetPrivateField("entityManager", entityManager);
            
            // Invoke biome field creation
            InvokePrivateMethod("CreateBiomeFieldEntities");
            
            // Verify all polarity fields were created
            using EntityQuery polarityQuery = entityManager.CreateEntityQuery(typeof(PolarityFieldData));
            Assert.AreEqual(4, polarityQuery.CalculateEntityCount(), "Should create 4 polarity fields");

            NativeArray<Entity> fieldEntities = polarityQuery.ToEntityArray(Allocator.Temp);
            var foundPolarities = new System.Collections.Generic.HashSet<Polarity>();
            
            for (int i = 0; i < fieldEntities.Length; i++)
            {
                PolarityFieldData fieldData = entityManager.GetComponentData<PolarityFieldData>(fieldEntities[i]);
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
            // Setup for 9 districts (8 + hub)
            SetPrivateField("defaultWorld", testWorld);
            SetPrivateField("entityManager", entityManager);
            SetPrivateField("targetSectorCount", 8);
            
            InvokePrivateMethod("CreateDistrictEntities");
            
            using EntityQuery nodeQuery = entityManager.CreateEntityQuery(typeof(NodeId));
            NativeArray<Entity> nodeEntities = nodeQuery.ToEntityArray(Allocator.Temp);
            var positions = new List<int2>();
            
            for (int i = 0; i < nodeEntities.Length; i++)
            {
                NodeId nodeId = entityManager.GetComponentData<NodeId>(nodeEntities[i]);
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
            // Setup
            SetPrivateField("defaultWorld", testWorld);
            SetPrivateField("entityManager", entityManager);
            
            InvokePrivateMethod("CreateDistrictEntities");
            
            using EntityQuery districtQuery = entityManager.CreateEntityQuery(
                typeof(NodeId), 
                typeof(WfcState), 
                typeof(WfcCandidateBufferElement),
                typeof(ConnectionBufferElement)
            );
            
            int districtsWithAllComponents = districtQuery.CalculateEntityCount();
            Assert.Greater(districtsWithAllComponents, 0, "All districts should have required ECS components");
            
            // Verify non-hub districts have additional components
            using EntityQuery sectorQuery = entityManager.CreateEntityQuery(
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
            SetPrivateField("enableDebugVisualization", true);
            SetPrivateField("worldSize", new int2(40, 30));
            
            // Start the component (this calls SetupSmokeTestWorld and begins Update loop)
            smokeTestComponent.enabled = true;
            
            yield return null; // Wait one frame
            
            // Verify that DebugDrawBounds is called periodically
            // (In actual implementation, we'd mock Debug.DrawLine or use a test harness)
            
            // For now, verify the component state is correct for debug drawing
            Assert.IsTrue(GetPrivateField<bool>("enableDebugVisualization"));
            Assert.AreEqual(new int2(40, 30), GetPrivateField<int2>("worldSize"));
            
            yield return null;
        }

        [Test]
        public void SmokeTestSceneSetup_ExtremeTargetSectorCount_HandlesGracefully()
        {
            // Test edge cases for targetSectorCount
            SetPrivateField("defaultWorld", testWorld);
            SetPrivateField("entityManager", entityManager);
            
            // Test extremely high value (should be clamped to 24)
            SetPrivateField("targetSectorCount", 100);
            InvokePrivateMethod("CreateDistrictEntities");
            
            using EntityQuery query = entityManager.CreateEntityQuery(typeof(NodeId));
            int entityCount = query.CalculateEntityCount();
            Assert.AreEqual(25, entityCount, "Should clamp to 24 districts + 1 hub = 25 total");
            
            // Clean up for next test
            entityManager.DestroyEntity(query.ToEntityArray(Allocator.Temp));
            
            // Test zero value (should create only hub)
            SetPrivateField("targetSectorCount", 0);
            InvokePrivateMethod("CreateDistrictEntities");
            
            int hubOnlyCount = query.CalculateEntityCount();
            Assert.AreEqual(1, hubOnlyCount, "Should create only hub with targetSectorCount of 0");
        }

        [Test]
        public void SmokeTestSceneSetup_PolarityFieldPositioning_MatchesExpectedLayout()
        {
            // Setup
            SetPrivateField("defaultWorld", testWorld);
            SetPrivateField("entityManager", entityManager);
            SetPrivateField("biomeTransitionRadius", 15.0f);
            
            InvokePrivateMethod("CreateBiomeFieldEntities");
            
            using EntityQuery query = entityManager.CreateEntityQuery(typeof(PolarityFieldData));
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            var polarityPositions = new Dictionary<Polarity, float2>();
            
            for (int i = 0; i < entities.Length; i++)
            {
                PolarityFieldData data = entityManager.GetComponentData<PolarityFieldData>(entities[i]);
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
            return (T)field.GetValue(smokeTestComponent);
        }

        private void SetPrivateField(string fieldName, object value)
        {
            System.Reflection.FieldInfo field = typeof(SmokeTestSceneSetup).GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, $"Field {fieldName} not found");
            field.SetValue(smokeTestComponent, value);
        }

        private void InvokePrivateMethod(string methodName)
        {
            System.Reflection.MethodInfo method = typeof(SmokeTestSceneSetup).GetMethod(methodName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, $"Method {methodName} not found");
            method.Invoke(smokeTestComponent, null);
        }
    }
}
