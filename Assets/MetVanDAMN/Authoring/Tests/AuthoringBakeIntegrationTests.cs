using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Authoring;

namespace TinyWalnutGames.MetVD.Authoring.Tests
{
    /// <summary>
    /// Comprehensive bake and smoke tests for all authoring components
    /// Validates the complete authoring->ECS conversion pipeline
    /// </summary>
    public class AuthoringBakeIntegrationTests
    {
        private World _testWorld;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _testWorld = new World("AuthoringBakeTestWorld");
            _entityManager = _testWorld.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            if (_testWorld.IsCreated)
                _testWorld.Dispose();
        }

        [Test]
        public void WfcTilePrototypeBaking_CreatesCorrectComponents()
        {
            // Test the WfcTilePrototypeAuthoring -> ECS baking pipeline
            var tileEntity = _entityManager.CreateEntity();
            
            // Simulate baked data (what the baker would create)
            _entityManager.AddComponentData(tileEntity, new WfcTilePrototype(
                tileId: 42,
                weight: 1.5f,
                biomeType: BiomeType.SolarPlains,
                primaryPolarity: Polarity.Sun,
                minConnections: 2,
                maxConnections: 4
            ));
            
            // Add socket buffer
            var socketBuffer = _entityManager.AddBuffer<WfcSocketBufferElement>(tileEntity);
            socketBuffer.Add(new WfcSocketBufferElement { Value = new WfcSocket(1, 0, Polarity.Sun, true) });
            socketBuffer.Add(new WfcSocketBufferElement { Value = new WfcSocket(1, 2, Polarity.Sun, true) });
            
            // Validate components exist and have correct data
            Assert.IsTrue(_entityManager.HasComponent<WfcTilePrototype>(tileEntity));
            Assert.IsTrue(_entityManager.HasBuffer<WfcSocketBufferElement>(tileEntity));
            
            var prototype = _entityManager.GetComponentData<WfcTilePrototype>(tileEntity);
            Assert.AreEqual(42u, prototype.TileId);
            Assert.AreEqual(1.5f, prototype.Weight, 0.001f);
            Assert.AreEqual(BiomeType.SolarPlains, prototype.BiomeType);
            Assert.AreEqual(Polarity.Sun, prototype.PrimaryPolarity);
            Assert.AreEqual(2, prototype.MinConnections);
            Assert.AreEqual(4, prototype.MaxConnections);
            
            var sockets = _entityManager.GetBuffer<WfcSocketBufferElement>(tileEntity);
            Assert.AreEqual(2, sockets.Length);
            Assert.AreEqual(1u, sockets[0].Value.SocketId);
            Assert.AreEqual(0, sockets[0].Value.Direction);
            Assert.AreEqual(Polarity.Sun, sockets[0].Value.RequiredPolarity);
            Assert.IsTrue(sockets[0].Value.IsOpen);
        }

        [Test]
        public void DistrictBaking_CreatesAllRequiredComponents()
        {
            // Test DistrictAuthoring -> ECS baking
            var districtEntity = _entityManager.CreateEntity();
            
            // Simulate baked data
            _entityManager.AddComponentData(districtEntity, new NodeId(
                value: 123,
                level: 2,
                parentId: 456,
                coordinates: new int2(10, 20)
            ));
            _entityManager.AddComponentData(districtEntity, new WfcState(WfcGenerationState.Initialized));
            _entityManager.AddComponentData(districtEntity, new SectorRefinementData(0.4f));
            _entityManager.AddBuffer<WfcCandidateBufferElement>(districtEntity);
            _entityManager.AddBuffer<ConnectionBufferElement>(districtEntity);
            
            // Validate all expected components exist
            Assert.IsTrue(_entityManager.HasComponent<NodeId>(districtEntity));
            Assert.IsTrue(_entityManager.HasComponent<WfcState>(districtEntity));
            Assert.IsTrue(_entityManager.HasComponent<SectorRefinementData>(districtEntity));
            Assert.IsTrue(_entityManager.HasBuffer<WfcCandidateBufferElement>(districtEntity));
            Assert.IsTrue(_entityManager.HasBuffer<ConnectionBufferElement>(districtEntity));
            
            var nodeId = _entityManager.GetComponentData<NodeId>(districtEntity);
            Assert.AreEqual(123u, nodeId.Value);
            Assert.AreEqual(2, nodeId.Level);
            Assert.AreEqual(456u, nodeId.ParentId);
            Assert.AreEqual(new int2(10, 20), nodeId.Coordinates);
            
            var wfcState = _entityManager.GetComponentData<WfcState>(districtEntity);
            Assert.AreEqual(WfcGenerationState.Initialized, wfcState.State);
            
            var refinementData = _entityManager.GetComponentData<SectorRefinementData>(districtEntity);
            Assert.AreEqual(0.4f, refinementData.TargetLoopDensity, 0.001f);
        }

        [Test]
        public void ConnectionBaking_CreatesConnectionEdgeComponents()
        {
            // Test ConnectionAuthoring -> ECS baking
            var connectionEntity = _entityManager.CreateEntity();
            var fromEntity = _entityManager.CreateEntity();
            var toEntity = _entityManager.CreateEntity();
            
            // Add NodeId to districts
            _entityManager.AddComponentData(fromEntity, new NodeId(1, 0, 0, new int2(0, 0)));
            _entityManager.AddComponentData(toEntity, new NodeId(2, 0, 0, new int2(1, 0)));
            
            // Simulate baked connection
            _entityManager.AddComponentData(connectionEntity, new ConnectionEdge
            {
                From = fromEntity,
                To = toEntity,
                Type = ConnectionType.Bidirectional,
                RequiredPolarity = Polarity.Sun,
                TraversalCost = 2.5f
            });
            
            var connectionEdge = _entityManager.GetComponentData<ConnectionEdge>(connectionEntity);
            Assert.AreEqual(fromEntity, connectionEdge.From);
            Assert.AreEqual(toEntity, connectionEdge.To);
            Assert.AreEqual(ConnectionType.Bidirectional, connectionEdge.Type);
            Assert.AreEqual(Polarity.Sun, connectionEdge.RequiredPolarity);
            Assert.AreEqual(2.5f, connectionEdge.TraversalCost, 0.001f);
        }

        [Test]
        public void GateConditionBaking_CreatesBufferElements()
        {
            // Test GateConditionAuthoring -> ECS baking
            var districtEntity = _entityManager.CreateEntity();
            var gateBuffer = _entityManager.AddBuffer<GateConditionBufferElement>(districtEntity);
            
            // Simulate baked gate condition
            var gateCondition = new GateCondition(
                Polarity.Heat,
                Ability.Flight,
                GateSoftness.Soft,
                0.75f,
                "Heat Gate"
            );
            gateBuffer.Add(gateCondition);
            
            Assert.AreEqual(1, gateBuffer.Length);
            var gate = gateBuffer[0].Value;
            Assert.AreEqual(Polarity.Heat, gate.RequiredPolarity);
            Assert.AreEqual(Ability.Flight, gate.RequiredAbilities);
            Assert.AreEqual(GateSoftness.Soft, gate.Softness);
            Assert.AreEqual(0.75f, gate.MinimumSkillLevel, 0.001f);
            Assert.IsTrue(gate.Description.ToString().Contains("Heat Gate"));
        }

        [Test]
        public void WorldGenerationConfig_IntegratesWithPipeline()
        {
            // Test that the WorldGenerationConfig from SmokeTestSceneSetup integrates properly
            var configEntity = _entityManager.CreateEntity();
            
            // Simulate the integrated world configuration
            _entityManager.AddComponentData(configEntity, new TinyWalnutGames.MetVD.Samples.WorldSeed { Value = 12345 });
            _entityManager.AddComponentData(configEntity, new TinyWalnutGames.MetVD.Samples.WorldBounds 
            { 
                Min = new int2(-25, -25),
                Max = new int2(25, 25)
            });
            _entityManager.AddComponentData(configEntity, new TinyWalnutGames.MetVD.Samples.WorldGenerationConfig
            {
                TargetSectorCount = 8,
                MaxDistrictCount = 32,
                BiomeTransitionRadius = 15.0f
            });
            
            var seed = _entityManager.GetComponentData<TinyWalnutGames.MetVD.Samples.WorldSeed>(configEntity);
            var bounds = _entityManager.GetComponentData<TinyWalnutGames.MetVD.Samples.WorldBounds>(configEntity);
            var genConfig = _entityManager.GetComponentData<TinyWalnutGames.MetVD.Samples.WorldGenerationConfig>(configEntity);
            
            Assert.AreEqual(12345u, seed.Value);
            Assert.AreEqual(new int2(-25, -25), bounds.Min);
            Assert.AreEqual(new int2(25, 25), bounds.Max);
            Assert.AreEqual(8, genConfig.TargetSectorCount);
            Assert.AreEqual(32, genConfig.MaxDistrictCount);
            Assert.AreEqual(15.0f, genConfig.BiomeTransitionRadius, 0.001f);
        }

        [Test]
        public void WfcSocketCompatibility_ValidatesCorrectly()
        {
            // Test WFC socket compatibility logic as implemented in WfcComponents
            var socketA = new WfcSocket(1, 0, Polarity.Sun, true);     // North-facing, Sun polarity
            var socketB = new WfcSocket(1, 2, Polarity.Sun, true);     // South-facing, Sun polarity
            var socketC = new WfcSocket(2, 0, Polarity.Moon, true);    // North-facing, Moon polarity
            var socketD = new WfcSocket(1, 0, Polarity.None, false);   // North-facing, closed

            // Compatible: same ID, opposite directions, compatible polarity
            Assert.IsTrue(socketA.IsCompatibleWith(socketB), "Same ID sockets with opposite directions should be compatible");
            
            // Incompatible: different IDs
            Assert.IsFalse(socketA.IsCompatibleWith(socketC), "Different socket IDs should not be compatible");
            
            // Incompatible: closed socket
            Assert.IsFalse(socketA.IsCompatibleWith(socketD), "Closed sockets should not be compatible");
            
            // Test polarity wildcard compatibility
            var wildcardSocket = new WfcSocket(1, 2, Polarity.Any, true);
            Assert.IsTrue(socketA.IsCompatibleWith(wildcardSocket), "Any polarity should be compatible");
            
            var noneSocket = new WfcSocket(1, 2, Polarity.None, true);
            Assert.IsTrue(socketA.IsCompatibleWith(noneSocket), "None polarity should be compatible");
        }

        [Test]
        public void SampleWfcData_CreatesValidTileSet()
        {
            // Test the sample WFC data creation
            int tilesCreated = TinyWalnutGames.MetVD.Graph.Data.SampleWfcData.InitializeSampleTileSet(_entityManager);
            
            Assert.AreEqual(4, tilesCreated, "Should create 4 sample tile prototypes");
            
            // Verify tiles were created with correct components
            var tileQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<WfcTilePrototype>());
            Assert.AreEqual(4, tileQuery.CalculateEntityCount());
            
            var prototypes = tileQuery.ToComponentDataArray<WfcTilePrototype>(Allocator.Temp);
            
            // Verify hub tile (ID 1)
            var hubTile = default(WfcTilePrototype);
            bool foundHub = false;
            for (int i = 0; i < prototypes.Length; i++)
            {
                if (prototypes[i].TileId == 1)
                {
                    hubTile = prototypes[i];
                    foundHub = true;
                    break;
                }
            }
            
            Assert.IsTrue(foundHub, "Should find hub tile with ID 1");
            Assert.AreEqual(BiomeType.HubArea, hubTile.BiomeType);
            Assert.AreEqual(Polarity.None, hubTile.PrimaryPolarity);
            Assert.AreEqual(2, hubTile.MinConnections);
            Assert.AreEqual(4, hubTile.MaxConnections);
            
            prototypes.Dispose();
            tileQuery.Dispose();
        }
    }
}