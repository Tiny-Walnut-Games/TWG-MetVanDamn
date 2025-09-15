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
    /// Debug version of DistrictWfcSystemTests to help identify what's going wrong
    /// </summary>
    public class DistrictWfcSystemDebugTests
    {
        private World _world;
        private SimulationSystemGroup _simGroup;

        [SetUp]
        public void SetUp()
        {
            _world = new World("DistrictWfcDebugTestWorld");
            _simGroup = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            SystemHandle systemHandle = _world.GetOrCreateSystem(typeof(DistrictWfcSystem));
            _simGroup.AddSystemToUpdateList(systemHandle);
            _simGroup.SortSystems();

            // Add WorldSeed component that DistrictWfcSystem requires for deterministic behavior
            Entity worldSeedEntity = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddComponentData(worldSeedEntity, new WorldSeed { Value = 42u });
        }

        [TearDown]
        public void TearDown()
        {
            if (_world.IsCreated)
            {
                _world.Dispose();
            }
        }

        private Entity CreateDebugEntity(WfcGenerationState initial, bool withBuffer = true, int iteration = 0)
        {
            EntityManager em = _world.EntityManager;
            Entity e = em.CreateEntity();
            em.AddComponentData(e, new WfcState(initial) { Iteration = iteration });
            em.AddComponentData(e, new NodeId(value: 42u, level: 0, parentId: 0, coordinates: int2.zero));
            if (withBuffer)
            {
                em.AddBuffer<WfcCandidateBufferElement>(e);
            }

            Debug.Log($"[DEBUG] Created entity {e.Index} with WfcState={initial}, HasBuffer={withBuffer}");
            return e;
        }

        [Test]
        public void Debug_SystemSetup_ShouldHaveCorrectComponents()
        {
            // Test if basic setup is working
            Entity e = CreateDebugEntity(WfcGenerationState.Initialized, withBuffer: true);

            EntityManager em = _world.EntityManager;

            // Verify components exist
            Assert.IsTrue(em.HasComponent<WfcState>(e), "Entity should have WfcState");
            Assert.IsTrue(em.HasComponent<NodeId>(e), "Entity should have NodeId");
            Assert.IsTrue(em.HasBuffer<WfcCandidateBufferElement>(e), "Entity should have WfcCandidateBufferElement buffer");

            // Check initial state
            WfcState initialState = em.GetComponentData<WfcState>(e);
            Debug.Log($"[DEBUG] Initial WfcState: {initialState.State}, Iteration: {initialState.Iteration}, Entropy: {initialState.Entropy}");

            DynamicBuffer<WfcCandidateBufferElement> initialBuffer = em.GetBuffer<WfcCandidateBufferElement>(e);
            Debug.Log($"[DEBUG] Initial buffer length: {initialBuffer.Length}");

            Assert.AreEqual(WfcGenerationState.Initialized, initialState.State);
            Assert.AreEqual(0, initialBuffer.Length);
        }

        [Test]
        public void Debug_SingleSystemUpdate_ShouldShowStateChanges()
        {
            Entity e = CreateDebugEntity(WfcGenerationState.Initialized, withBuffer: true);
            EntityManager em = _world.EntityManager;

            // Capture before state
            WfcState beforeState = em.GetComponentData<WfcState>(e);
            DynamicBuffer<WfcCandidateBufferElement> beforeBuffer = em.GetBuffer<WfcCandidateBufferElement>(e);
            Debug.Log($"[DEBUG] BEFORE Update: State={beforeState.State}, Iteration={beforeState.Iteration}, Entropy={beforeState.Entropy}, BufferLength={beforeBuffer.Length}");

            // Run one system update
            _simGroup.Update();

            // Capture after state
            WfcState afterState = em.GetComponentData<WfcState>(e);
            DynamicBuffer<WfcCandidateBufferElement> afterBuffer = em.GetBuffer<WfcCandidateBufferElement>(e);
            Debug.Log($"[DEBUG] AFTER Update: State={afterState.State}, Iteration={afterState.Iteration}, Entropy={afterState.Entropy}, BufferLength={afterBuffer.Length}");

            // Log buffer contents if any
            if (afterBuffer.Length > 0)
            {
                for (int i = 0; i < afterBuffer.Length; i++)
                {
                    var candidate = afterBuffer[i];
                    Debug.Log($"[DEBUG] Candidate {i}: TileId={candidate.TileId}, Weight={candidate.Weight}");
                }
            }

            // Let the test pass regardless of outcome so we can see the debug output
            Debug.Log($"[DEBUG] Test completed. Expected: InProgress with 4 candidates. Actual: {afterState.State} with {afterBuffer.Length} candidates");
        }

        [Test]
        public void Debug_WorldSeedCheck_ShouldFindSeedEntity()
        {
            // Check if WorldSeed entity is properly set up
            using var query = _world.EntityManager.CreateEntityQuery(typeof(WorldSeed));
            int count = query.CalculateEntityCount();
            Debug.Log($"[DEBUG] WorldSeed entities found: {count}");

            if (count > 0)
            {
                Entity seedEntity = query.GetSingletonEntity();
                WorldSeed seedComponent = _world.EntityManager.GetComponentData<WorldSeed>(seedEntity);
                Debug.Log($"[DEBUG] WorldSeed entity {seedEntity.Index} has seed value: {seedComponent.Value}");
            }

            Assert.Greater(count, 0, "Should have at least one WorldSeed entity");
        }

        [Test]
        public void Debug_WfcStateQuery_ShouldFindEntities()
        {
            Entity e = CreateDebugEntity(WfcGenerationState.Initialized, withBuffer: true);

            // Check if the system's query will find our entity
            using var query = _world.EntityManager.CreateEntityQuery(typeof(WfcState), typeof(NodeId));
            int count = query.CalculateEntityCount();
            Debug.Log($"[DEBUG] Entities matching WfcState+NodeId query: {count}");

            Assert.AreEqual(1, count, "Should find exactly one entity with WfcState and NodeId");
        }
    }
}
