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
    /// Quick test to verify our WFC fix is working
    /// </summary>
    public class WfcQuickTest
    {
        private World _testWorld;
        private EntityManager _entityManager;
        private SimulationSystemGroup _simGroup;

        [SetUp]
        public void SetUp()
        {
            this._testWorld = new World("WfcQuickTestWorld");
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
        public void WfcWithTilePrototypes_GeneratesNonzeroTileIds()
        {
            // Clear any existing entities
            this._entityManager.DestroyEntity(this._entityManager.UniversalQuery);

            // Create WorldSeed entity that DistrictWfcSystem expects
            Entity seedEntity = this._entityManager.CreateEntity();
            this._entityManager.AddComponentData(seedEntity, new WorldSeed { Value = 12345 });

            // Initialize sample tile prototypes (this should create WfcTilePrototype entities)
            int tilesCreated = SampleWfcData.InitializeSampleTileSet(this._entityManager);
            Assert.Greater(tilesCreated, 0, "Should create tile prototypes");

            // Create test district entities
            for (int i = 0; i < 3; i++)
            {
                var districtEntity = this._entityManager.CreateEntity();
                this._entityManager.AddComponentData(districtEntity, new NodeId((uint)i, 0, 0, new int2(i - 1, 0)));
                this._entityManager.AddComponentData(districtEntity, new WfcState(WfcGenerationState.Initialized));
                this._entityManager.AddBuffer<WfcCandidateBufferElement>(districtEntity);
            }

            // Run WFC generation for several frames
            for (int frame = 0; frame < 25; frame++)
            {
                this._simGroup.Update();
            }

            // Check results
            EntityQuery query = this._entityManager.CreateEntityQuery(ComponentType.ReadOnly<WfcState>());
            NativeArray<WfcState> states = query.ToComponentDataArray<WfcState>(Allocator.Temp);

            bool foundNonzero = false;
            int completedCount = 0;
            
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].State == WfcGenerationState.Completed)
                {
                    completedCount++;
                }
                
                if (states[i].AssignedTileId != 0)
                {
                    foundNonzero = true;
                    UnityEngine.Debug.Log($"✅ WFC Generated nonzero tile ID: {states[i].AssignedTileId} (state: {states[i].State})");
                }
            }

            states.Dispose();
            query.Dispose();

            UnityEngine.Debug.Log($"WFC Results: {states.Length} entities, {completedCount} completed, foundNonzero: {foundNonzero}");
            Assert.IsTrue(foundNonzero, "WFC should generate at least one nonzero tile ID when tile prototypes are available");
        }
    }
}
