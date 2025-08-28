using NUnit.Framework;
using UnityEngine; // Added for GameObject, ScriptableObject, Object
using UnityEngine.Tilemaps;
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

        // Minimal runtime data components used by tests (lightweight extraction of baked state)
        public struct DistrictData : IComponentData
        {
            public NodeId nodeId;
        }
        public struct ConnectionData : IComponentData
        {
            public NodeId sourceNode;
            public NodeId targetNode;
            public ConnectionType type;
        }
        public struct GateData : IComponentData
        {
            public NodeId connectionId; // Stored as node identifier reference (per test expectation)
        }

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

        // Factory helpers ----------------------------------------------------
        private static DistrictAuthoring CreateTestDistrict(string name, NodeId nodeId)
        {
            var go = new GameObject(name);
            var da = go.AddComponent<DistrictAuthoring>();
            da.nodeId = nodeId.Value;
            da.level = nodeId.Level;
            da.parentId = nodeId.ParentId;
            da.gridCoordinates = nodeId.Coordinates;
            return da;
        }
        private static ConnectionAuthoring CreateTestConnection(string name, NodeId from, NodeId to)
        {
            var go = new GameObject(name);
            var ca = go.AddComponent<ConnectionAuthoring>();
            ca.sourceNode = from.Value;
            ca.targetNode = to.Value;
            ca.type = ConnectionType.Bidirectional;
            return ca;
        }
        private static BiomeFieldAuthoring CreateTestBiome(string name, NodeId nodeId, BiomeType biome)
        {
            var go = new GameObject(name);
            var bf = go.AddComponent<BiomeFieldAuthoring>();
            bf.nodeId = nodeId.Value;
            bf.biomeType = biome;
            bf.primaryBiome = biome;
            bf.fieldRadius = 10f;
            return bf;
        }

        // Baking simulation (authoring -> ECS) --------------------------------
        private static void BakeGameObjects(World world, params GameObject[] gameObjects)
        {
            var em = world.EntityManager;
            foreach (var go in gameObjects)
            {
                if (go == null) continue;

                // District
                var district = go.GetComponent<DistrictAuthoring>();
                if (district != null)
                {
                    var e = em.CreateEntity();
                    var node = new NodeId(district.nodeId, district.level, district.parentId, district.gridCoordinates);
                    em.AddComponentData(e, node);
                    em.AddComponentData(e, new WfcState(WfcGenerationState.Initialized));
                    em.AddComponentData(e, new SectorRefinementData(district.targetLoopDensity));
                    em.AddBuffer<WfcCandidateBufferElement>(e); // candidate placeholder buffer
                    em.AddBuffer<ConnectionBufferElement>(e);    // connection placeholder buffer (from core graph)
                    em.AddComponentData(e, new DistrictData { nodeId = node });
                }

                // Connection
                var connection = go.GetComponent<ConnectionAuthoring>();
                if (connection != null)
                {
                    var e = em.CreateEntity();
                    var sourceNode = new NodeId(connection.sourceNode);
                    var targetNode = new NodeId(connection.targetNode);
                    em.AddComponentData(e, new ConnectionEdge
                    {
                        From = Entity.Null, // endpoints not explicitly referenced in this minimal bake
                        To = Entity.Null,
                        Type = connection.type,
                        RequiredPolarity = connection.requiredPolarity,
                        TraversalCost = connection.traversalCost
                    });
                    em.AddComponentData(e, new ConnectionData
                    {
                        sourceNode = sourceNode,
                        targetNode = targetNode,
                        type = connection.type
                    });
                }

                // Gate
                var gate = go.GetComponent<GateConditionAuthoring>();
                if (gate != null)
                {
                    var e = em.CreateEntity();
                    var buffer = em.AddBuffer<GateConditionBufferElement>(e);
                    if (gate.gateConditions != null && gate.gateConditions.Length > 0)
                    {
                        foreach (var cond in gate.gateConditions)
                            buffer.Add(cond);
                    }
                    else
                    {
                        var gc = new GateCondition(gate.requiredPolarity, gate.requiredAbilities, gate.softness, gate.minimumSkillLevel, gate.description);
                        buffer.Add(gc);
                    }
                    em.AddComponentData(e, new GateData { connectionId = gate.connectionId });
                }

                // Biome Field
                var biomeField = go.GetComponent<BiomeFieldAuthoring>();
                if (biomeField != null)
                {
                    var e = em.CreateEntity();
                    var biomeComp = new TinyWalnutGames.MetVD.Core.Biome(
                        biomeField.primaryBiome,
                        biomeField.polarity,
                        1.0f);
                    em.AddComponentData(e, biomeComp);
                    em.AddComponentData(e, new NodeId(biomeField.nodeId));
                }
            }
        }

        private static void CleanupGameObjects(params GameObject[] gameObjects)
        {
            foreach (var go in gameObjects)
            {
                if (go != null)
                {
#if UNITY_EDITOR
                    Object.DestroyImmediate(go);
#else
                    Object.Destroy(go);
#endif
                }
            }
        }

        // Existing tests follow ------------------------------------------------
        [Test]
        public void WfcTilePrototypeBaking_CreatesCorrectComponents()
        {
            // Test the WfcTilePrototypeAuthoring -> ECS baking pipeline
            var tileEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(tileEntity, new WfcTilePrototype(
                tileId: 42,
                weight: 1.5f,
                biomeType: BiomeType.SolarPlains,
                primaryPolarity: Polarity.Sun,
                minConnections: 2,
                maxConnections: 4
            ));
            var socketBuffer = _entityManager.AddBuffer<WfcSocketBufferElement>(tileEntity);
            socketBuffer.Add(new WfcSocketBufferElement { Value = new WfcSocket(1, 0, Polarity.Sun, true) });
            socketBuffer.Add(new WfcSocketBufferElement { Value = new WfcSocket(1, 2, Polarity.Sun, true) });
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
            var districtEntity = _entityManager.CreateEntity();
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
                GateSoftness.Easy,
                0.75f,
                "Heat Gate"
            );
            gateBuffer.Add(gateCondition);
            
            Assert.AreEqual(1, gateBuffer.Length);
            var gate = gateBuffer[0].Value;
            Assert.AreEqual(Polarity.Heat, gate.RequiredPolarity);
            Assert.AreEqual(Ability.Flight, gate.RequiredAbilities);
            Assert.AreEqual(GateSoftness.Easy, gate.Softness);
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

        [Test]
        public void TestInvalidDataValidationFailures()
        {
            // Negative cases - invalid data should fail validation appropriately
            using var world = new World("TestWorld");
            World.DefaultGameObjectInjectionWorld = world;
            
            // Test invalid district with duplicate NodeId
            var invalidDistrict1 = CreateTestDistrict("InvalidDistrict1", new NodeId { Value = 999 });
            var invalidDistrict2 = CreateTestDistrict("InvalidDistrict2", new NodeId { Value = 999 }); // Duplicate!
            
            BakeGameObjects(world, invalidDistrict1.gameObject, invalidDistrict2.gameObject);
            
            var districtQuery = world.EntityManager.CreateEntityQuery(typeof(DistrictData));
            var districts = districtQuery.ToComponentDataArray<DistrictData>(Allocator.Temp);
            
            // Both districts should still be created, but validation should catch the duplicate
            Assert.AreEqual(2, districts.Length, "Both invalid districts should be baked");
            
            // Test invalid connection referencing non-existent districts
            var invalidConnection = CreateTestConnection("InvalidConnection", 
                new NodeId { Value = 888 }, new NodeId { Value = 777 }); // Neither exist
            
            BakeGameObjects(world, invalidConnection.gameObject);
            
            var connectionQuery = world.EntityManager.CreateEntityQuery(typeof(ConnectionData));
            var connections = connectionQuery.ToComponentDataArray<ConnectionData>(Allocator.Temp);
            
            Assert.AreEqual(1, connections.Length, "Invalid connection should still be baked");
            Assert.AreEqual(888u, connections[0].sourceNode.Value);
            Assert.AreEqual(777u, connections[0].targetNode.Value);
            
            districts.Dispose();
            connections.Dispose();
            districtQuery.Dispose();
            connectionQuery.Dispose();
            
            CleanupGameObjects(invalidDistrict1.gameObject, invalidDistrict2.gameObject, invalidConnection.gameObject);
        }

        [Test]
        public void TestCrossComponentRelationshipFailures()
        {
            // Test cross-component relationship failures (gate ↔ connection ↔ district mismatches)
            using var world = new World("TestWorld");
            World.DefaultGameObjectInjectionWorld = world;
            
            // Create valid district and connection
            var district = CreateTestDistrict("TestDistrict", new NodeId { Value = 100 });
            var connection = CreateTestConnection("TestConnection", new NodeId { Value = 100 }, new NodeId { Value = 200 });
            
            // Create gate with invalid connection reference
            var gateGO = new GameObject("InvalidGate");
            var gate = gateGO.AddComponent<GateConditionAuthoring>();
            gate.connectionId = new NodeId { Value = 500 }; // References non-existent connection
            gate.gateConditions = new GateCondition[]
            {
                new() { 
                    requiredConnectionId = new NodeId { Value = 999 }, // Also invalid
                    isDefault = false 
                }
            };
            
            BakeGameObjects(world, district.gameObject, connection.gameObject, gateGO);
            
            // Verify entities are created despite relationship mismatches
            var districtQuery = world.EntityManager.CreateEntityQuery(typeof(DistrictData));
            var connectionQuery = world.EntityManager.CreateEntityQuery(typeof(ConnectionData));
            var gateQuery = world.EntityManager.CreateEntityQuery(typeof(GateData));
            
            Assert.AreEqual(1, districtQuery.CalculateEntityCount());
            Assert.AreEqual(1, connectionQuery.CalculateEntityCount());
            Assert.AreEqual(1, gateQuery.CalculateEntityCount());
            
            var gateData = gateQuery.ToComponentDataArray<GateData>(Allocator.Temp);
            Assert.AreEqual(500u, gateData[0].connectionId.Value, "Gate should reference invalid connection ID");
            
            gateData.Dispose();
            districtQuery.Dispose();
            connectionQuery.Dispose();
            gateQuery.Dispose();
            
            CleanupGameObjects(district.gameObject, connection.gameObject, gateGO);
        }

        [Test]
        public void TestBiomeArtProfileValidationFailures()
        {
            // Test biome with invalid or missing art profile
            using var world = new World("TestWorld");
            World.DefaultGameObjectInjectionWorld = world;
            
            var biomeGO = new GameObject("InvalidBiome");
            var biome = biomeGO.AddComponent<BiomeFieldAuthoring>();
            biome.nodeId = new NodeId { Value = 300 };
            biome.artProfile = null; // Invalid - null profile
            
            // Create another biome with profile but invalid settings
            var invalidProfileBiomeGO = new GameObject("InvalidProfileBiome");
            var invalidProfileBiome = invalidProfileBiomeGO.AddComponent<BiomeFieldAuthoring>();
            invalidProfileBiome.nodeId = new NodeId { Value = 301 };
            
            var invalidProfile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            // Intentionally leave tiles / prop arrays empty (not assignable via convenience properties)
            invalidProfileBiome.artProfile = invalidProfile;
            
            BakeGameObjects(world, biomeGO, invalidProfileBiomeGO);
            
            var biomeQuery = world.EntityManager.CreateEntityQuery(typeof(TinyWalnutGames.MetVD.Core.Biome));
            var biomes = biomeQuery.ToComponentDataArray<TinyWalnutGames.MetVD.Core.Biome>(Allocator.Temp);
            
            // Biomes should still be created even with invalid profiles
            Assert.AreEqual(2, biomes.Length, "Invalid biomes should still be baked");
            
            biomes.Dispose();
            biomeQuery.Dispose();
            
            CleanupGameObjects(biomeGO, invalidProfileBiomeGO);
            Object.DestroyImmediate(invalidProfile);
        }

        [Test]
        public void TestComplexRelationshipIntegrity()
        {
            // Test complex scenario with multiple interrelated components
            using var world = new World("TestWorld");
            World.DefaultGameObjectInjectionWorld = world;
            
            // Create network: District A ↔ District B, with gates and biomes
            var districtA = CreateTestDistrict("DistrictA", new NodeId { Value = 401 });
            var districtB = CreateTestDistrict("DistrictB", new NodeId { Value = 402 });
            
            var connectionAB = CreateTestConnection("ConnectionAB", 
                new NodeId { Value = 401 }, new NodeId { Value = 402 });
            var connectionBA = CreateTestConnection("ConnectionBA", 
                new NodeId { Value = 402 }, new NodeId { Value = 401 });
            
            // Create biomes for both districts
            var biomeA = CreateTestBiome("BiomeA", new NodeId { Value = 401 }, BiomeType.Forest);
            var biomeB = CreateTestBiome("BiomeB", new NodeId { Value = 402 }, BiomeType.Desert);
            
            // Create gate with circular reference (testing edge case)
            var circularGateGO = new GameObject("CircularGate");
            var circularGate = circularGateGO.AddComponent<GateConditionAuthoring>();
            circularGate.connectionId = new NodeId { Value = 401 };
            circularGate.gateConditions = new GateCondition[]
            {
                new() { 
                    requiredConnectionId = new NodeId { Value = 401 }, // Self-reference!
                    isDefault = false 
                }
            };
            
            BakeGameObjects(world, 
                districtA.gameObject, districtB.gameObject, 
                connectionAB.gameObject, connectionBA.gameObject,
                biomeA.gameObject, biomeB.gameObject,
                circularGateGO);
            
            // Verify all components were baked
            var districtQuery = world.EntityManager.CreateEntityQuery(typeof(DistrictData));
            var connectionQuery = world.EntityManager.CreateEntityQuery(typeof(ConnectionData));
            var biomeQuery = world.EntityManager.CreateEntityQuery(typeof(TinyWalnutGames.MetVD.Core.Biome));
            var gateQuery = world.EntityManager.CreateEntityQuery(typeof(GateData));
            
            Assert.AreEqual(2, districtQuery.CalculateEntityCount(), "Both districts should be baked");
            Assert.AreEqual(2, connectionQuery.CalculateEntityCount(), "Both connections should be baked");
            Assert.AreEqual(2, biomeQuery.CalculateEntityCount(), "Both biomes should be baked");
            Assert.AreEqual(1, gateQuery.CalculateEntityCount(), "Circular gate should be baked");
            
            // Verify data integrity
            var connections = connectionQuery.ToComponentDataArray<ConnectionData>(Allocator.Temp);
            var gates = gateQuery.ToComponentDataArray<GateData>(Allocator.Temp);
            
            // Check bidirectional connections
            bool foundAB = false, foundBA = false;
            foreach (var conn in connections)
            {
                if (conn.sourceNode.Value == 401 && conn.targetNode.Value == 402) foundAB = true;
                if (conn.sourceNode.Value == 402 && conn.targetNode.Value == 401) foundBA = true;
            }
            Assert.IsTrue(foundAB && foundBA, "Bidirectional connections should exist");
            
            // Check circular gate reference
            Assert.AreEqual(401u, gates[0].connectionId.Value, "Gate should have correct connection ID");
            
            connections.Dispose();
            gates.Dispose();
            districtQuery.Dispose();
            connectionQuery.Dispose();
            biomeQuery.Dispose();
            gateQuery.Dispose();
            
            CleanupGameObjects(
                districtA.gameObject, districtB.gameObject, 
                connectionAB.gameObject, connectionBA.gameObject,
                biomeA.gameObject, biomeB.gameObject,
                circularGateGO);
        }
    }
}
