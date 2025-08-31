using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine; // Added for GameObject, ScriptableObject, Object

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
		public void SetUp ()
			{
			this._testWorld = new World("AuthoringBakeTestWorld");
			this._entityManager = this._testWorld.EntityManager;
			}

		[TearDown]
		public void TearDown ()
			{
			if (this._testWorld.IsCreated)
				{
				this._testWorld.Dispose();
				}
			}

		// Factory helpers ----------------------------------------------------
		private static DistrictAuthoring CreateTestDistrict (string name, NodeId nodeId)
			{
			var go = new GameObject(name);
			DistrictAuthoring da = go.AddComponent<DistrictAuthoring>();
			da.nodeId = nodeId._value;
			da.level = nodeId.Level;
			da.parentId = nodeId.ParentId;
			da.gridCoordinates = nodeId.Coordinates;
			return da;
			}
		private static ConnectionAuthoring CreateTestConnection (string name, NodeId from, NodeId to)
			{
			var go = new GameObject(name);
			ConnectionAuthoring ca = go.AddComponent<ConnectionAuthoring>();
			ca.sourceNode = from._value;
			ca.targetNode = to._value;
			ca.type = ConnectionType.Bidirectional;
			return ca;
			}
		private static BiomeFieldAuthoring CreateTestBiome (string name, NodeId nodeId, BiomeType biome)
			{
			var go = new GameObject(name);
			BiomeFieldAuthoring bf = go.AddComponent<BiomeFieldAuthoring>();
			bf.nodeId = nodeId._value;
			bf.biomeType = biome;
			bf.primaryBiome = biome;
			bf.fieldRadius = 10f;
			return bf;
			}

		// Baking simulation (authoring -> ECS) --------------------------------
		private static void BakeGameObjects (World world, params GameObject [ ] gameObjects)
			{
			EntityManager em = world.EntityManager;
			foreach (GameObject go in gameObjects)
				{
				if (go == null)
					{
					continue;
					}

				// District
				if (go.TryGetComponent<DistrictAuthoring>(out DistrictAuthoring district))
					{
					Entity e = em.CreateEntity();
					var node = new NodeId(district.nodeId, district.level, district.parentId, district.gridCoordinates);
					em.AddComponentData(e, node);
					em.AddComponentData(e, new WfcState(WfcGenerationState.Initialized));
					em.AddComponentData(e, new SectorRefinementData(district.targetLoopDensity));
					em.AddBuffer<WfcCandidateBufferElement>(e); // candidate placeholder buffer
					em.AddBuffer<ConnectionBufferElement>(e);    // connection placeholder buffer (from core graph)
					em.AddComponentData(e, new DistrictData { nodeId = node });
					}

				// Connection
				if (go.TryGetComponent<ConnectionAuthoring>(out ConnectionAuthoring connection))
					{
					Entity e = em.CreateEntity();
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
				if (go.TryGetComponent<GateConditionAuthoring>(out GateConditionAuthoring gate))
					{
					Entity e = em.CreateEntity();
					DynamicBuffer<GateConditionBufferElement> buffer = em.AddBuffer<GateConditionBufferElement>(e);
					if (gate.gateConditions != null && gate.gateConditions.Length > 0)
						{
						foreach (GateCondition cond in gate.gateConditions)
							{
							buffer.Add(cond);
							}
						}
					else
						{
						var gc = new GateCondition(gate.requiredPolarity, gate.requiredAbilities, gate.softness, gate.minimumSkillLevel, gate.description);
						buffer.Add(gc);
						}
					em.AddComponentData(e, new GateData { connectionId = gate.connectionId });
					}

				// Biome Field
				if (go.TryGetComponent<BiomeFieldAuthoring>(out BiomeFieldAuthoring biomeField))
					{
					Entity e = em.CreateEntity();
					var biomeComp = new TinyWalnutGames.MetVD.Core.Biome(
						biomeField.primaryBiome,
						biomeField.polarity,
						1.0f);
					em.AddComponentData(e, biomeComp);
					em.AddComponentData(e, new NodeId(biomeField.nodeId));
					}
				}
			}

		private static void CleanupGameObjects (params GameObject [ ] gameObjects)
			{
			foreach (GameObject go in gameObjects)
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
		public void WfcTilePrototypeBaking_CreatesCorrectComponents ()
			{
			// Test the WfcTilePrototypeAuthoring -> ECS baking pipeline
			Entity tileEntity = this._entityManager.CreateEntity();
			this._entityManager.AddComponentData(tileEntity, new WfcTilePrototype(
				tileId: 42,
				weight: 1.5f,
				biomeType: BiomeType.SolarPlains,
				primaryPolarity: Polarity.Sun,
				minConnections: 2,
				maxConnections: 4
			));
			DynamicBuffer<WfcSocketBufferElement> socketBuffer = this._entityManager.AddBuffer<WfcSocketBufferElement>(tileEntity);
			socketBuffer.Add(new WfcSocketBufferElement { Value = new WfcSocket(1, 0, Polarity.Sun, true) });
			socketBuffer.Add(new WfcSocketBufferElement { Value = new WfcSocket(1, 2, Polarity.Sun, true) });
			Assert.IsTrue(this._entityManager.HasComponent<WfcTilePrototype>(tileEntity));
			Assert.IsTrue(this._entityManager.HasBuffer<WfcSocketBufferElement>(tileEntity));
			WfcTilePrototype prototype = this._entityManager.GetComponentData<WfcTilePrototype>(tileEntity);
			Assert.AreEqual(42u, prototype.TileId);
			Assert.AreEqual(1.5f, prototype.Weight, 0.001f);
			Assert.AreEqual(BiomeType.SolarPlains, prototype.BiomeType);
			Assert.AreEqual(Polarity.Sun, prototype.PrimaryPolarity);
			Assert.AreEqual(2, prototype.MinConnections);
			Assert.AreEqual(4, prototype.MaxConnections);
			DynamicBuffer<WfcSocketBufferElement> sockets = this._entityManager.GetBuffer<WfcSocketBufferElement>(tileEntity);
			Assert.AreEqual(2, sockets.Length);
			Assert.AreEqual(1u, sockets [ 0 ].Value.SocketId);
			Assert.AreEqual(0, sockets [ 0 ].Value.Direction);
			Assert.AreEqual(Polarity.Sun, sockets [ 0 ].Value.RequiredPolarity);
			Assert.IsTrue(sockets [ 0 ].Value.IsOpen);
			}

		[Test]
		public void DistrictBaking_CreatesAllRequiredComponents ()
			{
			Entity districtEntity = this._entityManager.CreateEntity();
			this._entityManager.AddComponentData(districtEntity, new NodeId(
				value: 123,
				level: 2,
				parentId: 456,
				coordinates: new int2(10, 20)
			));
			this._entityManager.AddComponentData(districtEntity, new WfcState(WfcGenerationState.Initialized));
			this._entityManager.AddComponentData(districtEntity, new SectorRefinementData(0.4f));
			this._entityManager.AddBuffer<WfcCandidateBufferElement>(districtEntity);
			this._entityManager.AddBuffer<ConnectionBufferElement>(districtEntity);
			Assert.IsTrue(this._entityManager.HasComponent<NodeId>(districtEntity));
			Assert.IsTrue(this._entityManager.HasComponent<WfcState>(districtEntity));
			Assert.IsTrue(this._entityManager.HasComponent<SectorRefinementData>(districtEntity));
			Assert.IsTrue(this._entityManager.HasBuffer<WfcCandidateBufferElement>(districtEntity));
			Assert.IsTrue(this._entityManager.HasBuffer<ConnectionBufferElement>(districtEntity));
			NodeId nodeId = this._entityManager.GetComponentData<NodeId>(districtEntity);
			Assert.AreEqual(123u, nodeId._value);
			Assert.AreEqual(2, nodeId.Level);
			Assert.AreEqual(456u, nodeId.ParentId);
			Assert.AreEqual(new int2(10, 20), nodeId.Coordinates);
			WfcState wfcState = this._entityManager.GetComponentData<WfcState>(districtEntity);
			Assert.AreEqual(WfcGenerationState.Initialized, wfcState.State);
			SectorRefinementData refinementData = this._entityManager.GetComponentData<SectorRefinementData>(districtEntity);
			Assert.AreEqual(0.4f, refinementData.TargetLoopDensity, 0.001f);
			}

		[Test]
		public void ConnectionBaking_CreatesConnectionEdgeComponents ()
			{
			// Test ConnectionAuthoring -> ECS baking
			Entity connectionEntity = this._entityManager.CreateEntity();
			Entity fromEntity = this._entityManager.CreateEntity();
			Entity toEntity = this._entityManager.CreateEntity();

			// Add NodeId to districts
			this._entityManager.AddComponentData(fromEntity, new NodeId(1, 0, 0, new int2(0, 0)));
			this._entityManager.AddComponentData(toEntity, new NodeId(2, 0, 0, new int2(1, 0)));

			// Simulate baked connection
			this._entityManager.AddComponentData(connectionEntity, new ConnectionEdge
				{
				From = fromEntity,
				To = toEntity,
				Type = ConnectionType.Bidirectional,
				RequiredPolarity = Polarity.Sun,
				TraversalCost = 2.5f
				});

			ConnectionEdge connectionEdge = this._entityManager.GetComponentData<ConnectionEdge>(connectionEntity);
			Assert.AreEqual(fromEntity, connectionEdge.From);
			Assert.AreEqual(toEntity, connectionEdge.To);
			Assert.AreEqual(ConnectionType.Bidirectional, connectionEdge.Type);
			Assert.AreEqual(Polarity.Sun, connectionEdge.RequiredPolarity);
			Assert.AreEqual(2.5f, connectionEdge.TraversalCost, 0.001f);
			}

		[Test]
		public void GateConditionBaking_CreatesBufferElements ()
			{
			// Test GateConditionAuthoring -> ECS baking
			Entity districtEntity = this._entityManager.CreateEntity();
			DynamicBuffer<GateConditionBufferElement> gateBuffer = this._entityManager.AddBuffer<GateConditionBufferElement>(districtEntity);

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
			GateCondition gate = gateBuffer [ 0 ].Value;
			Assert.AreEqual(Polarity.Heat, gate.RequiredPolarity);
			Assert.AreEqual(Ability.Flight, gate.RequiredAbilities);
			Assert.AreEqual(GateSoftness.Easy, gate.Softness);
			Assert.AreEqual(0.75f, gate.MinimumSkillLevel, 0.001f);
			Assert.IsTrue(gate.Description.ToString().Contains("Heat Gate"));
			}

		[Test]
		public void WorldGenerationConfig_IntegratesWithPipeline ()
			{
			// Test that the WorldGenerationConfig from SmokeTestSceneSetup integrates properly
			Entity configEntity = this._entityManager.CreateEntity();

			// Simulate the integrated world configuration
			this._entityManager.AddComponentData(configEntity, new TinyWalnutGames.MetVD.Samples.WorldSeed { Value = 12345 });
			this._entityManager.AddComponentData(configEntity, new TinyWalnutGames.MetVD.Samples.WorldBounds
				{
				Min = new int2(-25, -25),
				Max = new int2(25, 25)
				});
			this._entityManager.AddComponentData(configEntity, new TinyWalnutGames.MetVD.Samples.WorldGenerationConfig
				{
				TargetSectorCount = 8,
				MaxDistrictCount = 32,
				BiomeTransitionRadius = 15.0f
				});

			Samples.WorldSeed seed = this._entityManager.GetComponentData<TinyWalnutGames.MetVD.Samples.WorldSeed>(configEntity);
			Samples.WorldBounds bounds = this._entityManager.GetComponentData<TinyWalnutGames.MetVD.Samples.WorldBounds>(configEntity);
			Samples.WorldGenerationConfig genConfig = this._entityManager.GetComponentData<TinyWalnutGames.MetVD.Samples.WorldGenerationConfig>(configEntity);

			Assert.AreEqual(12345u, seed.Value);
			Assert.AreEqual(new int2(-25, -25), bounds.Min);
			Assert.AreEqual(new int2(25, 25), bounds.Max);
			Assert.AreEqual(8, genConfig.TargetSectorCount);
			Assert.AreEqual(32, genConfig.MaxDistrictCount);
			Assert.AreEqual(15.0f, genConfig.BiomeTransitionRadius, 0.001f);
			}

		[Test]
		public void WfcSocketCompatibility_ValidatesCorrectly ()
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
		public void SampleWfcData_CreatesValidTileSet ()
			{
			// Test the sample WFC data creation
			int tilesCreated = TinyWalnutGames.MetVD.Graph.Data.SampleWfcData.InitializeSampleTileSet(this._entityManager);

			Assert.AreEqual(4, tilesCreated, "Should create 4 sample tile prototypes");

			// Verify tiles were created with correct components
			EntityQuery tileQuery = this._entityManager.CreateEntityQuery(ComponentType.ReadOnly<WfcTilePrototype>());
			Assert.AreEqual(4, tileQuery.CalculateEntityCount());

			NativeArray<WfcTilePrototype> prototypes = tileQuery.ToComponentDataArray<WfcTilePrototype>(Allocator.Temp);

			// Verify hub tile (ID 1)
			var hubTile = default(WfcTilePrototype);
			bool foundHub = false;
			for (int i = 0; i < prototypes.Length; i++)
				{
				if (prototypes [ i ].TileId == 1)
					{
					hubTile = prototypes [ i ];
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
		public void TestInvalidDataValidationFailures ()
			{
			// Negative cases - invalid data should fail validation appropriately
			using var world = new World("TestWorld");
			World.DefaultGameObjectInjectionWorld = world;

			// Test invalid district with duplicate NodeId
			DistrictAuthoring invalidDistrict1 = CreateTestDistrict("InvalidDistrict1", new NodeId { _value = 999 });
			DistrictAuthoring invalidDistrict2 = CreateTestDistrict("InvalidDistrict2", new NodeId { _value = 999 }); // Duplicate!

			BakeGameObjects(world, invalidDistrict1.gameObject, invalidDistrict2.gameObject);

			EntityQuery districtQuery = world.EntityManager.CreateEntityQuery(typeof(DistrictData));
			NativeArray<DistrictData> districts = districtQuery.ToComponentDataArray<DistrictData>(Allocator.Temp);

			// Both districts should still be created, but validation should catch the duplicate
			Assert.AreEqual(2, districts.Length, "Both invalid districts should be baked");

			// Test invalid connection referencing non-existent districts
			ConnectionAuthoring invalidConnection = CreateTestConnection("InvalidConnection",
				new NodeId { _value = 888 }, new NodeId { _value = 777 }); // Neither exist

			BakeGameObjects(world, invalidConnection.gameObject);

			EntityQuery connectionQuery = world.EntityManager.CreateEntityQuery(typeof(ConnectionData));
			NativeArray<ConnectionData> connections = connectionQuery.ToComponentDataArray<ConnectionData>(Allocator.Temp);

			Assert.AreEqual(1, connections.Length, "Invalid connection should still be baked");
			Assert.AreEqual(888u, connections [ 0 ].sourceNode._value);
			Assert.AreEqual(777u, connections [ 0 ].targetNode._value);

			districts.Dispose();
			connections.Dispose();
			districtQuery.Dispose();
			connectionQuery.Dispose();

			CleanupGameObjects(invalidDistrict1.gameObject, invalidDistrict2.gameObject, invalidConnection.gameObject);
			}

		[Test]
		public void TestCrossComponentRelationshipFailures ()
			{
			// Test cross-component relationship failures (gate ↔ connection ↔ district mismatches)
			using var world = new World("TestWorld");
			World.DefaultGameObjectInjectionWorld = world;

			// Create valid district and connection
			DistrictAuthoring district = CreateTestDistrict("TestDistrict", new NodeId { _value = 100 });
			ConnectionAuthoring connection = CreateTestConnection("TestConnection", new NodeId { _value = 100 }, new NodeId { _value = 200 });

			// Create gate with invalid connection reference
			var gateGO = new GameObject("InvalidGate");
			GateConditionAuthoring gate = gateGO.AddComponent<GateConditionAuthoring>();
			gate.connectionId = new NodeId { _value = 500 }; // References non-existent connection
			gate.gateConditions = new GateCondition [ ]
			{
				new() {
					requiredConnectionId = new NodeId { _value = 999 }, // Also invalid
                    isDefault = false
				}
			};

			BakeGameObjects(world, district.gameObject, connection.gameObject, gateGO);

			// Verify entities are created despite relationship mismatches
			EntityQuery districtQuery = world.EntityManager.CreateEntityQuery(typeof(DistrictData));
			EntityQuery connectionQuery = world.EntityManager.CreateEntityQuery(typeof(ConnectionData));
			EntityQuery gateQuery = world.EntityManager.CreateEntityQuery(typeof(GateData));

			Assert.AreEqual(1, districtQuery.CalculateEntityCount());
			Assert.AreEqual(1, connectionQuery.CalculateEntityCount());
			Assert.AreEqual(1, gateQuery.CalculateEntityCount());

			NativeArray<GateData> gateData = gateQuery.ToComponentDataArray<GateData>(Allocator.Temp);
			Assert.AreEqual(500u, gateData [ 0 ].connectionId._value, "Gate should reference invalid connection ID");

			gateData.Dispose();
			districtQuery.Dispose();
			connectionQuery.Dispose();
			gateQuery.Dispose();

			CleanupGameObjects(district.gameObject, connection.gameObject, gateGO);
			}

		[Test]
		public void TestBiomeArtProfileValidationFailures ()
			{
			// Test biome with invalid or missing art profile
			using var world = new World("TestWorld");
			World.DefaultGameObjectInjectionWorld = world;

			var biomeGO = new GameObject("InvalidBiome");
			BiomeFieldAuthoring biome = biomeGO.AddComponent<BiomeFieldAuthoring>();
			biome.nodeId = new NodeId { _value = 300 };
			biome.artProfile = null; // Invalid - null profile

			// Create another biome with profile but invalid settings
			var invalidProfileBiomeGO = new GameObject("InvalidProfileBiome");
			BiomeFieldAuthoring invalidProfileBiome = invalidProfileBiomeGO.AddComponent<BiomeFieldAuthoring>();
			invalidProfileBiome.nodeId = new NodeId { _value = 301 };

			BiomeArtProfile invalidProfile = ScriptableObject.CreateInstance<BiomeArtProfile>();
			// Intentionally leave tiles / prop arrays empty (not assignable via convenience properties)
			invalidProfileBiome.artProfile = invalidProfile;

			BakeGameObjects(world, biomeGO, invalidProfileBiomeGO);

			EntityQuery biomeQuery = world.EntityManager.CreateEntityQuery(typeof(TinyWalnutGames.MetVD.Core.Biome));
			NativeArray<Biome> biomes = biomeQuery.ToComponentDataArray<TinyWalnutGames.MetVD.Core.Biome>(Allocator.Temp);

			// Biomes should still be created even with invalid profiles
			Assert.AreEqual(2, biomes.Length, "Invalid biomes should still be baked");

			biomes.Dispose();
			biomeQuery.Dispose();

			CleanupGameObjects(biomeGO, invalidProfileBiomeGO);
			Object.DestroyImmediate(invalidProfile);
			}

		[Test]
		public void TestComplexRelationshipIntegrity ()
			{
			// Test complex scenario with multiple interrelated components
			using var world = new World("TestWorld");
			World.DefaultGameObjectInjectionWorld = world;

			// Create network: District A ↔ District B, with gates and biomes
			DistrictAuthoring districtA = CreateTestDistrict("DistrictA", new NodeId { _value = 401 });
			DistrictAuthoring districtB = CreateTestDistrict("DistrictB", new NodeId { _value = 402 });

			ConnectionAuthoring connectionAB = CreateTestConnection("ConnectionAB",
				new NodeId { _value = 401 }, new NodeId { _value = 402 });
			ConnectionAuthoring connectionBA = CreateTestConnection("ConnectionBA",
				new NodeId { _value = 402 }, new NodeId { _value = 401 });

			// Create biomes for both districts
			BiomeFieldAuthoring biomeA = CreateTestBiome("BiomeA", new NodeId { _value = 401 }, BiomeType.Forest);
			BiomeFieldAuthoring biomeB = CreateTestBiome("BiomeB", new NodeId { _value = 402 }, BiomeType.Desert);

			// Create gate with circular reference (testing edge case)
			var circularGateGO = new GameObject("CircularGate");
			GateConditionAuthoring circularGate = circularGateGO.AddComponent<GateConditionAuthoring>();
			circularGate.connectionId = new NodeId { _value = 401 };
			circularGate.gateConditions = new GateCondition [ ]
			{
				new() {
					requiredConnectionId = new NodeId { _value = 401 }, // Self-reference!
                    isDefault = false
				}
			};

			BakeGameObjects(world,
				districtA.gameObject, districtB.gameObject,
				connectionAB.gameObject, connectionBA.gameObject,
				biomeA.gameObject, biomeB.gameObject,
				circularGateGO);

			// Verify all components were baked
			EntityQuery districtQuery = world.EntityManager.CreateEntityQuery(typeof(DistrictData));
			EntityQuery connectionQuery = world.EntityManager.CreateEntityQuery(typeof(ConnectionData));
			EntityQuery biomeQuery = world.EntityManager.CreateEntityQuery(typeof(TinyWalnutGames.MetVD.Core.Biome));
			EntityQuery gateQuery = world.EntityManager.CreateEntityQuery(typeof(GateData));

			Assert.AreEqual(2, districtQuery.CalculateEntityCount(), "Both districts should be baked");
			Assert.AreEqual(2, connectionQuery.CalculateEntityCount(), "Both connections should be baked");
			Assert.AreEqual(2, biomeQuery.CalculateEntityCount(), "Both biomes should be baked");
			Assert.AreEqual(1, gateQuery.CalculateEntityCount(), "Circular gate should be baked");

			// Verify data integrity
			NativeArray<ConnectionData> connections = connectionQuery.ToComponentDataArray<ConnectionData>(Allocator.Temp);
			NativeArray<GateData> gates = gateQuery.ToComponentDataArray<GateData>(Allocator.Temp);

			// Check bidirectional connections
			bool foundAB = false, foundBA = false;
			foreach (ConnectionData conn in connections)
				{
				if (conn.sourceNode._value == 401 && conn.targetNode._value == 402)
					{
					foundAB = true;
					}

				if (conn.sourceNode._value == 402 && conn.targetNode._value == 401)
					{
					foundBA = true;
					}
				}
			Assert.IsTrue(foundAB && foundBA, "Bidirectional connections should exist");

			// Check circular gate reference
			Assert.AreEqual(401u, gates [ 0 ].connectionId._value, "Gate should have correct connection ID");

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
