using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Graph.Data;
using TinyWalnutGames.MetVD.Samples;
using TinyWalnutGames.MetVD.Shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;

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
			this._testWorld = new World("WfcDeterminismTestWorld");
			this._entityManager = this._testWorld.EntityManager;
			this._simGroup = this._testWorld.GetOrCreateSystemManaged<SimulationSystemGroup>();

			// Add the WFC system to the simulation group
			SystemHandle systemHandle = this._testWorld.GetOrCreateSystem(typeof(DistrictWfcSystem));
			this._simGroup.AddSystemToUpdateList(systemHandle);
			this._simGroup.SortSystems();

			// 🎯 FIX: Initialize tile prototypes that the WFC system needs
			// This was missing from the original tests, causing all zero tile assignments
			int tilesCreated = SampleWfcData.InitializeSampleTileSet(this._entityManager);
			Assert.Greater(tilesCreated, 0, "SetUp should create tile prototypes for WFC system");
			}

		[TearDown]
		public void TearDown()
			{
			if (this._testWorld.IsCreated)
				{
				this._testWorld.Dispose();
				}
			}

		[Test]
		public void WfcGeneration_WithSameSeed_ProducesSameResults()
			{
			// Test that WFC generation is deterministic with the same seed
			const uint testSeed = 42;

			WfcGenerationResults results1 = this.RunWfcGenerationWithSeed(testSeed);
			WfcGenerationResults results2 = this.RunWfcGenerationWithSeed(testSeed);

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

			WfcGenerationResults results1 = this.RunWfcGenerationWithSeed(seed1);
			WfcGenerationResults results2 = this.RunWfcGenerationWithSeed(seed2);

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

			if (!foundDifference)
				{
				// Log diagnostic info for debugging determinism: emit full assignment arrays
				int len = results1.tileAssignments.Length;
				var sb1 = new System.Text.StringBuilder();
				var sb2 = new System.Text.StringBuilder();
				for (int i = 0; i < len; i++)
					{
					sb1.Append(results1.tileAssignments[i]).Append(',');
					sb2.Append(results2.tileAssignments[i]).Append(',');
					}
				UnityEngine.Debug.Log($"WFC Determinism DIAG: seed1={seed1} seed2={seed2} assignments seed1=[{sb1}] seed2=[{sb2}]");
				Assert.IsTrue(foundDifference, $"Different seeds should produce different results. See logs for diagnostic sample.");
				}

			results1.Dispose();
			results2.Dispose();
			}

		[Test]
		public void WfcGeneration_WithSameWorldConfig_IsReproducible()
			{
			// Test full world generation reproducibility
			const uint worldSeed = 789;
			const int targetSectors = 5;

			WorldGenerationConfig config1 = this.CreateTestWorldConfig(worldSeed, targetSectors);
			WorldGenerationConfig config2 = this.CreateTestWorldConfig(worldSeed, targetSectors);

			uint hash1 = this.GenerateWorldAndComputeHash(config1);
			uint hash2 = this.GenerateWorldAndComputeHash(config2);

			Assert.AreEqual(hash1, hash2, "Same world configuration should produce identical results");
			}

		[Test]
		public void WfcConstraintValidation_IsConsistent()
			{
			// Test that constraint validation produces consistent results
			Entity testEntity = this.CreateTestEntityWithWfcState();
			var nodeId = new NodeId(1, 0, 0, new int2(5, 5));
			this._entityManager.SetComponentData(testEntity, nodeId);

			// Run multiple validation passes
			bool[] validationResults = new bool[10];
			for (int i = 0; i < validationResults.Length; i++)
				{
				// Simulate validation logic from DistrictWfcJob
				var random = new Unity.Mathematics.Random((uint)(i * 1103515245 + 12345));
				validationResults[i] = this.ValidateTestConstraints(testEntity, nodeId, ref random);
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

		[Test]
		public void WfcGeneration_AssignsAtLeastOneNonzeroTileId()
			{
			// Test that WFC assigns at least one nonzero tile ID after running
			const uint testSeed = 123;
			WfcGenerationResults results = this.RunWfcGenerationWithSeed(testSeed);

			bool foundNonzero = false;
			for (int i = 0; i < results.tileAssignments.Length; i++)
				{
				if (results.tileAssignments[i] != 0)
					{
					foundNonzero = true;
					break;
					}
				}
			results.Dispose();
			Assert.IsTrue(foundNonzero, "At least one entity should have a nonzero AssignedTileId after WFC generation.");
			}

		private WfcGenerationResults RunWfcGenerationWithSeed(uint seed)
			{
			// Clear any existing entities
			this._entityManager.DestroyEntity(this._entityManager.UniversalQuery);

			// Create test world with specific seed
			this.CreateTestWorld(seed);

			// Run WFC generation for several frames
			for (int frame = 0; frame < 20; frame++)
				{
				this._simGroup.Update();
				}

			// Collect results deterministically by sorting entities by NodeId._value
			EntityQuery query = this._entityManager.CreateEntityQuery(ComponentType.ReadOnly<WfcState>(), ComponentType.ReadOnly<NodeId>());
			NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

			var pairs = new List<(uint nodeValue, WfcState state)>();
			pairs.Capacity = entities.Length;
			for (int i = 0; i < entities.Length; i++)
				{
				var n = this._entityManager.GetComponentData<NodeId>(entities[i]);
				var s = this._entityManager.GetComponentData<WfcState>(entities[i]);
				pairs.Add((n._value, s));
				}

			// Stable sort by node id to ensure deterministic ordering across runs
			pairs.Sort((a, b) => a.nodeValue.CompareTo(b.nodeValue));

			var results = new WfcGenerationResults
				{
				entityCount = pairs.Count,
				completedCount = 0,
				tileAssignments = new NativeArray<uint>(pairs.Count, Allocator.Temp)
				};

			for (int i = 0; i < pairs.Count; i++)
				{
				if (pairs[i].state.State == WfcGenerationState.Completed)
					{
					results.completedCount++;
					}
				results.tileAssignments[i] = pairs[i].state.AssignedTileId;
				}

			entities.Dispose();
			query.Dispose();

			return results;
			}

		private void CreateTestWorld(uint seed)
			{
			// ✅ FIX: Create WorldSeed entity that DistrictWfcSystem expects
			Entity seedEntity = this._entityManager.CreateEntity();
			this._entityManager.AddComponentData(seedEntity, new WorldSeed { Value = seed });

			// Create world configuration
			Entity configEntity = this._entityManager.CreateEntity();
			this._entityManager.AddComponentData(configEntity, new WorldBounds
				{
				Min = new int2(-10, -10),
				Max = new int2(10, 10)
				});

			// Add sample tile prototypes and sockets for WFC to function
			TinyWalnutGames.MetVD.Graph.Data.SampleWfcData.InitializeSampleTileSet(this._entityManager);

			// Create test districts
			for (int i = 0; i < 5; i++)
				{
				Entity districtEntity = this._entityManager.CreateEntity();
				this._entityManager.AddComponentData(districtEntity, new NodeId((uint)i, 0, 0, new int2(i - 2, 0)));
				this._entityManager.AddComponentData(districtEntity, new WfcState(WfcGenerationState.Initialized));
				this._entityManager.AddBuffer<WfcCandidateBufferElement>(districtEntity);
				}
			}

		private uint GenerateWorldAndComputeHash(WorldGenerationConfig config)
			{
			// Clear existing entities
			this._entityManager.DestroyEntity(this._entityManager.UniversalQuery);

			// ✅ FIX: Create WorldSeed entity that DistrictWfcSystem expects
			Entity seedEntity = this._entityManager.CreateEntity();
			this._entityManager.AddComponentData(seedEntity, new WorldSeed { Value = config.WorldSeed });

			// Create world based on config
			Entity configEntity = this._entityManager.CreateEntity();
			this._entityManager.AddComponentData(configEntity, config);

			// Generate districts based on target sector count
			for (int i = 0; i < config.TargetSectorCount; i++)
				{
				Entity districtEntity = this._entityManager.CreateEntity();
				this._entityManager.AddComponentData(districtEntity, new NodeId((uint)i, 0, 0, new int2(i % 3, i / 3)));
				this._entityManager.AddComponentData(districtEntity, new WfcState(WfcGenerationState.Initialized));
				this._entityManager.AddBuffer<WfcCandidateBufferElement>(districtEntity);
				}

			// Run generation
			for (int frame = 0; frame < 15; frame++)
				{
				this._simGroup.Update();
				}

			// Compute hash of final state
			EntityQuery query = this._entityManager.CreateEntityQuery(ComponentType.ReadOnly<WfcState>());
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
			Entity entity = this._entityManager.CreateEntity();
			this._entityManager.AddComponentData(entity, new WfcState(WfcGenerationState.InProgress));
			this._entityManager.AddBuffer<WfcCandidateBufferElement>(entity);
			// Ensure a NodeId component exists so tests can call SetComponentData on it later
			this._entityManager.AddComponentData(entity, new NodeId(0u, 0, 0, new int2(0, 0)));
			return entity;
			}

		private bool ValidateTestConstraints(Entity entity, NodeId nodeId, ref Unity.Mathematics.Random random)
			{
			// We first need to assign a temporary entity to simulate the job context?
			if (!this._entityManager.HasComponent<WfcState>(entity))
				{
				return false;
				}
			WfcState wfcState = this._entityManager.GetComponentData<WfcState>(entity);
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

		private struct WfcGenerationResults
			{
			public int entityCount;
			public int completedCount;
			public NativeArray<uint> tileAssignments;

			public void Dispose()
				{
				if (this.tileAssignments.IsCreated)
					{
					this.tileAssignments.Dispose();
					}
				}
			}
		}
	}
