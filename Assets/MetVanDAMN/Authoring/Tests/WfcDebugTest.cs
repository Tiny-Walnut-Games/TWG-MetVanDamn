using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Graph.Data;
using TinyWalnutGames.MetVD.Shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Authoring.Tests
    {
    /// <summary>
    /// Diagnostic test to debug WFC system behavior
    /// </summary>
    public class WfcDebugTest
        {
        private World _testWorld;
        private EntityManager _entityManager;
        private SimulationSystemGroup _simGroup;

        [SetUp]
        public void SetUp()
            {
            this._testWorld = new World("WfcDebugTestWorld");
            this._entityManager = this._testWorld.EntityManager;
            this._simGroup = this._testWorld.GetOrCreateSystemManaged<SimulationSystemGroup>();

            // Add the WFC system to the simulation group
            SystemHandle systemHandle = this._testWorld.GetOrCreateSystem(typeof(DistrictWfcSystem));
            this._simGroup.AddSystemToUpdateList(systemHandle);
            this._simGroup.SortSystems();
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
        public void DebugWfcSystem_ShowAllSteps()
            {
            // Clear any existing entities
            this._entityManager.DestroyEntity(this._entityManager.UniversalQuery);

            // Create WorldSeed entity
            Entity seedEntity = this._entityManager.CreateEntity();
            this._entityManager.AddComponentData(seedEntity, new WorldSeed { Value = 42 });

            // Initialize sample tile prototypes
            int tilesCreated = SampleWfcData.InitializeSampleTileSet(this._entityManager);
            UnityEngine.Debug.Log($"🔍 Created {tilesCreated} tile prototypes");

            // Query for tile prototypes to verify they exist
            EntityQuery prototypeQuery = this._entityManager.CreateEntityQuery(ComponentType.ReadOnly<WfcTilePrototype>());
            NativeArray<WfcTilePrototype> prototypes = prototypeQuery.ToComponentDataArray<WfcTilePrototype>(Allocator.Temp);
            
            UnityEngine.Debug.Log($"🔍 Found {prototypes.Length} tile prototypes in system:");
            for (int i = 0; i < prototypes.Length; i++)
                {
                UnityEngine.Debug.Log($"  Prototype {i}: TileId={prototypes[i].TileId}, Weight={prototypes[i].Weight}");
                }
            prototypes.Dispose();
            prototypeQuery.Dispose();

            // Create one test district entity
            var districtEntity = this._entityManager.CreateEntity();
            this._entityManager.AddComponentData(districtEntity, new NodeId(1, 0, 0, new int2(0, 0)));
            this._entityManager.AddComponentData(districtEntity, new WfcState(WfcGenerationState.Initialized));
            this._entityManager.AddBuffer<WfcCandidateBufferElement>(districtEntity);

            UnityEngine.Debug.Log($"🔍 Created district entity {districtEntity.Index} with Initialized state");

            // Run WFC generation step by step
            for (int frame = 0; frame < 10; frame++)
                {
                UnityEngine.Debug.Log($"🔍 === FRAME {frame} ===");
                
                // Check state before update
                var stateBefore = this._entityManager.GetComponentData<WfcState>(districtEntity);
                UnityEngine.Debug.Log($"🔍 Before: State={stateBefore.State}, TileId={stateBefore.AssignedTileId}, Entropy={stateBefore.Entropy}");
                
                // Check candidates buffer
                var candidatesBuffer = this._entityManager.GetBuffer<WfcCandidateBufferElement>(districtEntity);
                UnityEngine.Debug.Log($"🔍 Candidates buffer length: {candidatesBuffer.Length}");
                for (int i = 0; i < candidatesBuffer.Length && i < 5; i++)
                    {
                    UnityEngine.Debug.Log($"  Candidate {i}: TileId={candidatesBuffer[i].TileId}, Weight={candidatesBuffer[i].Weight}");
                    }

                // Update system
                this._simGroup.Update();
                
                // Check state after update
                var stateAfter = this._entityManager.GetComponentData<WfcState>(districtEntity);
                UnityEngine.Debug.Log($"🔍 After: State={stateAfter.State}, TileId={stateAfter.AssignedTileId}, Entropy={stateAfter.Entropy}");
                
                if (stateAfter.State == WfcGenerationState.Completed || 
                    stateAfter.State == WfcGenerationState.Failed ||
                    stateAfter.State == WfcGenerationState.Contradiction)
                    {
                    UnityEngine.Debug.Log($"🔍 WFC finished with state: {stateAfter.State}");
                    break;
                    }
                }

            var finalState = this._entityManager.GetComponentData<WfcState>(districtEntity);
            UnityEngine.Debug.Log($"🔍 FINAL RESULT: State={finalState.State}, AssignedTileId={finalState.AssignedTileId}");
            
            // This test is for debugging, so we'll always pass but log everything
            Assert.IsTrue(true, "Debug test completed - check logs for details");
            }
        }
    }
