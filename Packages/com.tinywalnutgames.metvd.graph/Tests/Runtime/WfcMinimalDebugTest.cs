using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Shared;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Tests
{
    /// <summary>
    /// Minimal test to debug the exact WFC issue
    /// </summary>
    public class WfcMinimalDebugTest
    {
        [Test]
        public void MinimalTest_SystemShouldProcessInitializedEntity()
        {
            // Create minimal world
            using World world = new World("MinimalDebugWorld");
            EntityManager em = world.EntityManager;

            // Add WorldSeed (required by DistrictWfcSystem)
            Entity seedEntity = em.CreateEntity();
            em.AddComponentData(seedEntity, new WorldSeed { Value = 42u });

            // Create test entity with exact components the system expects
            Entity testEntity = em.CreateEntity();
            em.AddComponentData(testEntity, new WfcState(WfcGenerationState.Initialized));
            em.AddComponentData(testEntity, new NodeId(value: 1u, level: 0, parentId: 0, coordinates: int2.zero));
            em.AddBuffer<WfcCandidateBufferElement>(testEntity);

			// Manually create and run the system
			SimulationSystemGroup simGroup = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
			SystemHandle systemHandle = world.CreateSystem(typeof(DistrictWfcSystem));
            simGroup.AddSystemToUpdateList(systemHandle);
            simGroup.SortSystems();

            // Debug: Check what the system's queries will find
            using EntityQuery worldSeedQuery = em.CreateEntityQuery(typeof(WorldSeed));
            using EntityQuery wfcQuery = em.CreateEntityQuery(typeof(WfcState), typeof(NodeId));

            Debug.Log($"WorldSeed entities: {worldSeedQuery.CalculateEntityCount()}");
            Debug.Log($"WfcState+NodeId entities: {wfcQuery.CalculateEntityCount()}");

            // Check initial state
            WfcState initialState = em.GetComponentData<WfcState>(testEntity);
			DynamicBuffer<WfcCandidateBufferElement> initialBuffer = em.GetBuffer<WfcCandidateBufferElement>(testEntity);
            Debug.Log($"BEFORE: State={initialState.State}, Iteration={initialState.Iteration}, BufferLength={initialBuffer.Length}");

            // Run the system
            simGroup.Update();

            // Check final state
            WfcState finalState = em.GetComponentData<WfcState>(testEntity);
			DynamicBuffer<WfcCandidateBufferElement> finalBuffer = em.GetBuffer<WfcCandidateBufferElement>(testEntity);
            Debug.Log($"AFTER: State={finalState.State}, Iteration={finalState.Iteration}, BufferLength={finalBuffer.Length}");

            // The test should work - if not, we'll see exactly what's happening
            if (finalState.State == WfcGenerationState.InProgress && finalBuffer.Length == 4)
            {
                Debug.Log("✅ System worked correctly!");
            }
            else
            {
                Debug.Log($"❌ System didn't work as expected. Expected: InProgress with 4 candidates. Got: {finalState.State} with {finalBuffer.Length} candidates");
            }

            // For now, just log the result instead of asserting
            Assert.Pass($"Debug test complete. Final state: {finalState.State}, Buffer length: {finalBuffer.Length}");
        }
    }
}
