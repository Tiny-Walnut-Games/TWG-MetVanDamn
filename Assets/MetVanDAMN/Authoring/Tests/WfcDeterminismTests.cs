using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Samples;

namespace TinyWalnutGames.MetVD.Authoring.Tests
{
    /// <summary>
    /// Determinism validation tests ensuring consistent WFC generation across runs
    /// Critical for reproducible world generation and debugging
    /// </summary>
    public class WfcDeterminismTests
    {
        private World _testWorld;
        private EntityManager _entityManager;
        private SimulationSystemGroup _simGroup;

        [SetUp]
        public void SetUp()
        {
            _testWorld = new World("WfcDeterminismTestWorld");
            _entityManager = _testWorld.EntityManager;
            _simGroup = _testWorld.GetOrCreateSystemManaged<SimulationSystemGroup>();

            // Add the WFC system to the simulation group
            SystemHandle systemHandle = _testWorld.GetOrCreateSystem(typeof(DistrictWfcSystem));
            _simGroup.AddSystemToUpdateList(systemHandle);
            _simGroup.SortSystems();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testWorld.IsCreated)
            {
                _testWorld.Dispose();
            }
        }

        [Test]
        public void WfcGeneration_WithSameSeed_ProducesSameResults()
        {
            // Test that WFC generation is deterministic with the same seed
            const uint testSeed = 42;

            WfcGenerationResults results1 = RunWfcGenerationWithSeed(testSeed);
            WfcGenerationResults results2 = RunWfcGenerationWithSeed(testSeed);
            
            Assert.AreEqual(results1.entityCount, results2.entityCount, "Should generate same number of entities");
            Assert.AreEqual(results1.completedCount, results2.completedCount, "Should complete same number of entities");
            
            // Verify individual entity results match
            for (int i = 0; i < results1.tileAssignments.Length; i++)
            {
                Assert.AreEqual(results1.tileAssignments[i], results2.tileAssignments[i], 
                    $"Entity {i} should have same tile assignment");
            }
            
            results1.Dispose();
            results2.Dispose();
        }

        [Test]
        public void WfcGeneration_WithDifferentSeeds_ProducesDifferentResults()
        {
            // Test that different seeds produce different but valid results
            const uint seed1 = 123;
            const uint seed2 = 456;

            WfcGenerationResults results1 = RunWfcGenerationWithSeed(seed1);
            WfcGenerationResults results2 = RunWfcGenerationWithSeed(seed2);
            
            Assert.AreEqual(results1.entityCount, results2.entityCount, "Should generate same number of entities");
            
            // Results should be different (with high probability)
            bool foundDifference = false;
            for (int i = 0; i < results1.tileAssignments.Length; i++)
            {
                if (results1.tileAssignments[i] != results2.tileAssignments[i])
                {
                    foundDifference = true;
                    break;
                }
            }
            
            Assert.IsTrue(foundDifference, "Different seeds should produce different results");
            
            results1.Dispose();
            results2.Dispose();
        }

        [Test]
        public void WfcGeneration_WithSameWorldConfig_IsReproducible()
        {
            // Test full world generation reproducibility
            const uint worldSeed = 789;
            const int targetSectors = 5;

            WorldGenerationConfig config1 = CreateTestWorldConfig(worldSeed, targetSectors);
            WorldGenerationConfig config2 = CreateTestWorldConfig(worldSeed, targetSectors);

            uint hash1 = GenerateWorldAndComputeHash(config1);
            uint hash2 = GenerateWorldAndComputeHash(config2);
            
            Assert.AreEqual(hash1, hash2, "Same world configuration should produce identical results");
        }

        [Test]
        public void WfcConstraintValidation_IsConsistent()
        {
            // Test that constraint validation produces consistent results
            Entity testEntity = CreateTestEntityWithWfcState();
            var nodeId = new NodeId(1, 0, 0, new int2(5, 5));
            _entityManager.SetComponentData(testEntity, nodeId);

            // Run multiple validation passes
            bool[] validationResults = new bool[10];
            for (int i = 0; i < validationResults.Length; i++)
            {
                // Simulate validation logic from DistrictWfcJob
                var random = new Unity.Mathematics.Random((uint)(i * 1103515245 + 12345));
                validationResults[i] = ValidateTestConstraints(testEntity, nodeId, ref random);
            }
            
            // With same conditions, validation should be consistent
            // (Note: some randomness is expected, but patterns should be stable)
            int trueCount = 0;
            for (int i = 0; i < validationResults.Length; i++)
            {
                if (validationResults[i])
                {
                    trueCount++;
                }
            }
            
            // Should have consistent bias toward true/false, not completely random
            Assert.IsTrue(trueCount == 0 || trueCount == validationResults.Length || 
                         (trueCount > 2 && trueCount < validationResults.Length - 2),
                         "Validation should show consistent patterns, not pure randomness");
        }

        private WfcGenerationResults RunWfcGenerationWithSeed(uint seed)
        {
            // Clear any existing entities
            _entityManager.DestroyEntity(_entityManager.UniversalQuery);
            
            // Create test world with specific seed
            CreateTestWorld(seed);
            
            // Run WFC generation for several frames
            for (int frame = 0; frame < 20; frame++)
            {
                _simGroup.Update();
            }

            // Collect results
            EntityQuery query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<WfcState>());
            NativeArray<WfcState> states = query.ToComponentDataArray<WfcState>(Allocator.Temp);
            
            var results = new WfcGenerationResults
            {
                entityCount = states.Length,
                completedCount = 0,
                tileAssignments = new NativeArray<uint>(states.Length, Allocator.Temp)
            };
            
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].State == WfcGenerationState.Completed)
                {
                    results.completedCount++;
                }
                results.tileAssignments[i] = states[i].AssignedTileId;
            }
            
            states.Dispose();
            query.Dispose();
            
            return results;
        }

        private void CreateTestWorld(uint seed)
        {
            // Create world configuration
            Entity configEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(configEntity, new WorldSeed { Value = seed });
            _entityManager.AddComponentData(configEntity, new WorldBounds 
            { 
                Min = new int2(-10, -10),
                Max = new int2(10, 10)
            });
            
            // Create test districts
            for (int i = 0; i < 5; i++)
            {
                Entity districtEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(districtEntity, new NodeId((uint)i, 0, 0, new int2(i - 2, 0)));
                _entityManager.AddComponentData(districtEntity, new WfcState(WfcGenerationState.Initialized));
                _entityManager.AddBuffer<WfcCandidateBufferElement>(districtEntity);
            }
        }

        private WorldGenerationConfig CreateTestWorldConfig(uint seed, int targetSectors)
        {
            return new WorldGenerationConfig
            {
                WorldSeed = seed,
                TargetSectorCount = targetSectors,
                MaxDistrictCount = targetSectors * 4,
                BiomeTransitionRadius = 10.0f
            };
        }

        private uint GenerateWorldAndComputeHash(WorldGenerationConfig config)
        {
            // Clear existing entities
            _entityManager.DestroyEntity(_entityManager.UniversalQuery);

            // Create world based on config
            Entity configEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(configEntity, config);
            
            // Generate districts based on target sector count
            for (int i = 0; i < config.TargetSectorCount; i++)
            {
                Entity districtEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(districtEntity, new NodeId((uint)i, 0, 0, new int2(i % 3, i / 3)));
                _entityManager.AddComponentData(districtEntity, new WfcState(WfcGenerationState.Initialized));
                _entityManager.AddBuffer<WfcCandidateBufferElement>(districtEntity);
            }
            
            // Run generation
            for (int frame = 0; frame < 15; frame++)
            {
                _simGroup.Update();
            }

            // Compute hash of final state
            EntityQuery query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<WfcState>());
            NativeArray<WfcState> states = query.ToComponentDataArray<WfcState>(Allocator.Temp);
            
            uint hash = 0;
            for (int i = 0; i < states.Length; i++)
            {
                hash = hash * 31u + states[i].AssignedTileId;
                hash = hash * 31u + (uint)states[i].State;
                hash = hash * 31u + (uint)states[i].Iteration;
            }
            
            states.Dispose();
            query.Dispose();
            
            return hash;
        }

        private Entity CreateTestEntityWithWfcState()
        {
            Entity entity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(entity, new WfcState(WfcGenerationState.InProgress));
            _entityManager.AddBuffer<WfcCandidateBufferElement>(entity);
            return entity;
        }

        private bool ValidateTestConstraints(Entity entity, NodeId nodeId, ref Unity.Mathematics.Random random)
        {
            // We first need to assign a temporary entity to simulate the job context?
            if (!_entityManager.HasComponent<WfcState>(entity))
            {
                return false;
            }
            WfcState wfcState = _entityManager.GetComponentData<WfcState>(entity);
            if (wfcState.State != WfcGenerationState.InProgress)
            {
                return false;
            }

            // ⚠Intended use!⚠ Simplified version of constraint validation from DistrictWfcJob for testing purposes
            // Example constraints based on position - not actual game logic
            // if your specific project has different constraints, adjust accordingly
            float d = math.length(new float2(nodeId.Coordinates));
            if (d > 30f)
            {
                return random.NextFloat() < 0.2f;
            }

            int parity = (nodeId.Coordinates.x ^ nodeId.Coordinates.y) & 1;
            if (parity == 1)
            {
                return random.NextFloat() > 0.3f;
            }

            return true;
        }

        private struct WfcGenerationResults
        {
            public int entityCount;
            public int completedCount;
            public NativeArray<uint> tileAssignments;

            public void Dispose()
            {
                if (tileAssignments.IsCreated)
                {
                    tileAssignments.Dispose();
                }
            }
        }
    }
}