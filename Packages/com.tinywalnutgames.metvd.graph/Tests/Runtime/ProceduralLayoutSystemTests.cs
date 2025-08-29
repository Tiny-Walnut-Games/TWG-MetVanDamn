using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Graph.Tests
{
    /// <summary>
    /// Tests for the procedural layout system and rule randomization using actual unmanaged systems.
    /// </summary>
    public class ProceduralLayoutSystemTests
    {
        private World _testWorld;
        private EntityManager _entityManager;
        private InitializationSystemGroup _initGroup;

        [SetUp]
        public void SetUp()
        {
            _testWorld = new World("TestWorld");
            _entityManager = _testWorld.EntityManager;
            _initGroup = _testWorld.GetOrCreateSystemManaged<InitializationSystemGroup>();

            // Create unmanaged systems (District layout -> connections -> rules -> sector/room hierarchy)
            SystemHandle layoutHandle = _testWorld.CreateSystem(typeof(DistrictLayoutSystem));
            SystemHandle connectionHandle = _testWorld.CreateSystem(typeof(ConnectionBuilderSystem));
            SystemHandle rulesHandle = _testWorld.CreateSystem(typeof(RuleRandomizationSystem));
            SystemHandle sectorRoomHandle = _testWorld.CreateSystem(typeof(SectorRoomHierarchySystem));

            _initGroup.AddSystemToUpdateList(layoutHandle);
            _initGroup.AddSystemToUpdateList(connectionHandle);
            _initGroup.AddSystemToUpdateList(rulesHandle);
            _initGroup.AddSystemToUpdateList(sectorRoomHandle);
            _initGroup.SortSystems();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testWorld != null && _testWorld.IsCreated)
            {
                _testWorld.Dispose();
            }
        }

        [Test]
        public void DistrictLayoutSystem_WithUnplacedDistricts_ShouldAssignCoordinates()
        {
            // Arrange
            Entity worldConfig = _entityManager.CreateEntity();
            _entityManager.AddComponentData(worldConfig, new WorldConfiguration
            {
                Seed = 12345,
                WorldSize = new int2(32, 32),
                TargetSectors = 5,
                RandomizationMode = RandomizationMode.Partial
            });

            // Create unplaced districts
            Entity district1 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(district1, new NodeId(1, 0, 0, new int2(0, 0)));
            _entityManager.AddComponentData(district1, new WfcState());

            Entity district2 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(district2, new NodeId(2, 0, 0, new int2(0, 0)));
            _entityManager.AddComponentData(district2, new WfcState());

            // Act
            _initGroup.Update();

            // Assert
            NodeId node1 = _entityManager.GetComponentData<NodeId>(district1);
            NodeId node2 = _entityManager.GetComponentData<NodeId>(district2);

            // Should no longer be at (0,0)
            Assert.That(node1.Coordinates.x != 0 || node1.Coordinates.y != 0, "District 1 should be moved from (0,0)");
            Assert.That(node2.Coordinates.x != 0 || node2.Coordinates.y != 0, "District 2 should be moved from (0,0)");

            // Check that layout done tag was created
            using EntityQuery layoutDoneQuery = _entityManager.CreateEntityQuery(typeof(DistrictLayoutDoneTag));
            Assert.That(layoutDoneQuery.CalculateEntityCount(), Is.EqualTo(1), "Should create DistrictLayoutDoneTag");
        }

        [Test]
        public void SectorRoomHierarchySystem_GeneratesSectorsAndRooms()
        {
            // Arrange world + districts (TargetSectors influences sectors per district)
            Entity worldConfig = _entityManager.CreateEntity();
            _entityManager.AddComponentData(worldConfig, new WorldConfiguration
            {
                Seed = 2222,
                WorldSize = new int2(40, 40),
                TargetSectors = 6,
                RandomizationMode = RandomizationMode.None
            });
            for (uint i = 0; i < 3; i++)
            {
                Entity d = _entityManager.CreateEntity();
                _entityManager.AddComponentData(d, new NodeId(i + 1, 0, 0, int2.zero));
                _entityManager.AddComponentData(d, new WfcState());
            }

            // Act (single update should: place districts -> add DistrictLayoutDoneTag -> subdivide into sectors + rooms)
            _initGroup.Update();

            using EntityQuery sectorQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<NodeId>(), ComponentType.ReadOnly<SectorHierarchyData>());
            using EntityQuery roomQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<NodeId>(), ComponentType.ReadOnly<RoomHierarchyData>());

            int sectorCount = 0;
            int roomCount = 0;
            Unity.Collections.NativeArray<NodeId> sectorNodeIds = sectorQuery.ToComponentDataArray<NodeId>(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < sectorNodeIds.Length; i++)
            {
                if (sectorNodeIds[i].Level == 1)
                {
                    sectorCount++;
                }
            }

            Unity.Collections.NativeArray<NodeId> roomNodeIds = roomQuery.ToComponentDataArray<NodeId>(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < roomNodeIds.Length; i++)
            {
                if (roomNodeIds[i].Level == 2)
                {
                    roomCount++;
                }
            }

            sectorNodeIds.Dispose(); roomNodeIds.Dispose();

            Assert.Greater(sectorCount, 0, "Should generate at least one sector");
            Assert.Greater(roomCount, 0, "Should generate at least one room");
        }

        [Test]
        public void RuleRandomizationSystem_WithPartialMode_ShouldRandomizeBiomes()
        {
            // Arrange
            Entity worldConfig = _entityManager.CreateEntity();
            _entityManager.AddComponentData(worldConfig, new WorldConfiguration
            {
                Seed = 54321,
                WorldSize = new int2(16, 16),
                TargetSectors = 3,
                RandomizationMode = RandomizationMode.Partial
            });

            Entity layoutDone = _entityManager.CreateEntity();
            _entityManager.AddComponentData(layoutDone, new DistrictLayoutDoneTag(3, 0));

            // Create test districts to satisfy ConnectionBuilderSystem requirements
            Entity district1 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(district1, new NodeId(1, 0, 0, new int2(1, 1)));
            _entityManager.AddBuffer<ConnectionBufferElement>(district1);

            Entity district2 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(district2, new NodeId(2, 0, 0, new int2(2, 2)));
            _entityManager.AddBuffer<ConnectionBufferElement>(district2);

            // Act - Run systems in dependency order
            _initGroup.Update();

            // Assert
            using EntityQuery ruleQuery = _entityManager.CreateEntityQuery(typeof(WorldRuleSet));
            Assert.That(ruleQuery.CalculateEntityCount(), Is.EqualTo(1), "Should create WorldRuleSet");

            WorldRuleSet ruleSet = ruleQuery.GetSingleton<WorldRuleSet>();
            Assert.That(ruleSet.BiomePolarityMask, Is.Not.EqualTo(Polarity.None), "Should have assigned biome polarities");
            Assert.That(ruleSet.UpgradesRandomized, Is.False, "Upgrades should not be randomized in Partial mode");

            using EntityQuery rulesDoneQuery = _entityManager.CreateEntityQuery(typeof(RuleRandomizationDoneTag));
            Assert.That(rulesDoneQuery.CalculateEntityCount(), Is.EqualTo(1), "Should create RuleRandomizationDoneTag");
        }

        [Test]
        public void RuleRandomizationSystem_WithFullMode_ShouldRandomizeEverything()
        {
            // Arrange
            Entity worldConfig = _entityManager.CreateEntity();
            _entityManager.AddComponentData(worldConfig, new WorldConfiguration
            {
                Seed = 98765,
                WorldSize = new int2(48, 48),
                TargetSectors = 8,
                RandomizationMode = RandomizationMode.Full
            });

            Entity layoutDone = _entityManager.CreateEntity();
            _entityManager.AddComponentData(layoutDone, new DistrictLayoutDoneTag(8, 0));

            // Create test districts to satisfy ConnectionBuilderSystem requirements
            Entity district1 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(district1, new NodeId(1, 0, 0, new int2(1, 1)));
            _entityManager.AddBuffer<ConnectionBufferElement>(district1);

            Entity district2 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(district2, new NodeId(2, 0, 0, new int2(2, 2)));
            _entityManager.AddBuffer<ConnectionBufferElement>(district2);

            // Act - Run systems in dependency order
            _initGroup.Update();

            // Assert
            using EntityQuery ruleQuery = _entityManager.CreateEntityQuery(typeof(WorldRuleSet));
            WorldRuleSet ruleSet = ruleQuery.GetSingleton<WorldRuleSet>();

            Assert.That(ruleSet.BiomePolarityMask, Is.Not.EqualTo(Polarity.None), "Should have assigned biome polarities");
            Assert.That(ruleSet.UpgradesRandomized, Is.True, "Upgrades should be randomized in Full mode");
            Assert.That(ruleSet.AvailableUpgradesMask, Is.Not.EqualTo(0u), "Should have some upgrades available");

            // Essential upgrade should always be available (reachability guard)
            uint jumpUpgrade = 1u << 0;
            Assert.That((ruleSet.AvailableUpgradesMask & jumpUpgrade), Is.EqualTo(jumpUpgrade), "Jump upgrade should always be available");
        }

        [Test]
        public void RuleRandomizationSystem_WithNoneMode_ShouldUseCuratedRules()
        {
            // Arrange
            Entity worldConfig = _entityManager.CreateEntity();
            _entityManager.AddComponentData(worldConfig, new WorldConfiguration
            {
                Seed = 11111,
                WorldSize = new int2(24, 24),
                TargetSectors = 4,
                RandomizationMode = RandomizationMode.None
            });

            Entity layoutDone = _entityManager.CreateEntity();
            _entityManager.AddComponentData(layoutDone, new DistrictLayoutDoneTag(4, 0));

            // Create test districts to satisfy ConnectionBuilderSystem requirements
            Entity district1 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(district1, new NodeId(1, 0, 0, new int2(1, 1)));
            _entityManager.AddBuffer<ConnectionBufferElement>(district1);

            Entity district2 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(district2, new NodeId(2, 0, 0, new int2(2, 2)));
            _entityManager.AddBuffer<ConnectionBufferElement>(district2);

            // Act - Run systems in dependency order
            _initGroup.Update();

            // Assert
            using EntityQuery ruleQuery = _entityManager.CreateEntityQuery(typeof(WorldRuleSet));
            WorldRuleSet ruleSet = ruleQuery.GetSingleton<WorldRuleSet>();

            Assert.That(ruleSet.UpgradesRandomized, Is.False, "Upgrades should not be randomized in None mode");

            // Should have curated polarity distribution
            Polarity expectedCuratedPolarities = Polarity.Sun | Polarity.Moon | Polarity.Heat | Polarity.Cold;
            Assert.That(ruleSet.BiomePolarityMask, Is.EqualTo(expectedCuratedPolarities), "Should use curated polarity distribution");
        }
    }
}
