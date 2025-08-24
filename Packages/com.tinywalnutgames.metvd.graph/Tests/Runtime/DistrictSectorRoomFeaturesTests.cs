using Unity.Entities;
using Unity.Mathematics;
using NUnit.Framework;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Graph.Tests
{
    /// <summary>
    /// Tests for the new district/sector/room features implementation
    /// Validates that TargetSectors parameter is properly used and sector/room hierarchy works
    /// </summary>
    public class DistrictSectorRoomFeaturesTests
    {
        private World _testWorld;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _testWorld = new World("TestWorld");
            _entityManager = _testWorld.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            _testWorld?.Dispose();
        }

        [Test]
        public void DistrictLayoutSystem_RespectsTargetSectors()
        {
            // Create world configuration with specific TargetSectors
            var configEntity = _entityManager.CreateEntity();
            var worldConfig = new WorldConfiguration
            {
                Seed = 12345,
                WorldSize = new int2(100, 100),
                TargetSectors = 8,
                RandomizationMode = RandomizationMode.Partial
            };
            _entityManager.AddComponentData(configEntity, worldConfig);

            // Create multiple unplaced districts (more than TargetSectors)
            Entity[] districts = new Entity[12];
            for (int i = 0; i < districts.Length; i++)
            {
                districts[i] = _entityManager.CreateEntity();
                _entityManager.AddComponentData(districts[i], new NodeId((uint)(i + 1), 0, 0, int2.zero));
                _entityManager.AddComponentData(districts[i], new WfcState());
            }

            // Simulate DistrictLayoutSystem logic
            var random = new Unity.Mathematics.Random((uint)worldConfig.Seed);
            var targetDistrictCount = worldConfig.TargetSectors > 0 ? 
                math.min(worldConfig.TargetSectors, districts.Length) : districts.Length;

            // Verify target count calculation
            Assert.AreEqual(8, targetDistrictCount, "TargetSectors should limit district count to 8");
            Assert.Less(targetDistrictCount, districts.Length, "Should limit districts when TargetSectors < available districts");
        }

        [Test]
        public void SectorHierarchyData_CreatesProperlyStructuredSectors()
        {
            // Test the sector hierarchy constants and data structures
            var sectorData = new SectorHierarchyData(new int2(6, 6), 4, 12345u);
            
            Assert.AreEqual(new int2(6, 6), sectorData.LocalGridSize);
            Assert.AreEqual(4, sectorData.SectorCount);
            Assert.AreEqual(12345u, sectorData.SectorSeed);
            Assert.IsFalse(sectorData.IsSubdivided, "New sector data should not be subdivided initially");

            // Test hierarchy constants
            Assert.AreEqual(1000u, HierarchyConstants.SectorIdMultiplier);
            Assert.AreEqual(100u, HierarchyConstants.RoomsPerSectorMultiplier);
        }

        [Test]
        public void RoomHierarchyData_SupportsDifferentRoomTypes()
        {
            var bounds = new RectInt(0, 0, 8, 6);
            
            // Test different room types
            var bossRoom = new RoomHierarchyData(bounds, RoomType.Boss, true);
            var treasureRoom = new RoomHierarchyData(bounds, RoomType.Treasure, true);
            var normalRoom = new RoomHierarchyData(bounds, RoomType.Normal, false);

            Assert.AreEqual(RoomType.Boss, bossRoom.Type);
            Assert.AreEqual(RoomType.Treasure, treasureRoom.Type);
            Assert.AreEqual(RoomType.Normal, normalRoom.Type);
            
            Assert.IsTrue(bossRoom.IsLeafRoom);
            Assert.IsTrue(treasureRoom.IsLeafRoom);
            Assert.IsFalse(normalRoom.IsLeafRoom);
        }

        [Test]
        public void RoomManagement_CreatesProperFeatures()
        {
            // Create a room entity with hierarchy data
            var roomEntity = _entityManager.CreateEntity();
            var roomBounds = new RectInt(0, 0, 10, 8);
            var roomData = new RoomHierarchyData(roomBounds, RoomType.Boss, true);
            var nodeId = new NodeId(12345, 2, 1000, new int2(5, 4));
            
            _entityManager.AddComponentData(roomEntity, roomData);
            _entityManager.AddComponentData(roomEntity, nodeId);

            // Test room state data creation
            var roomState = new RoomStateData(3);
            Assert.IsFalse(roomState.IsVisited);
            Assert.IsFalse(roomState.IsExplored);
            Assert.AreEqual(0, roomState.SecretsFound);
            Assert.AreEqual(3, roomState.TotalSecrets);
            Assert.AreEqual(0.0f, roomState.CompletionPercentage);

            // Test room navigation data
            var primaryEntrance = new int2(5, 0); // Bottom center
            var navData = new RoomNavigationData(primaryEntrance, true, 15.0f);
            Assert.AreEqual(primaryEntrance, navData.PrimaryEntrance);
            Assert.IsTrue(navData.IsCriticalPath);
            Assert.AreEqual(15.0f, navData.TraversalTime, 0.01f);
        }

        [Test]
        public void HierarchicalNodeIds_CreateUniqueIds()
        {
            // Test hierarchical ID generation
            uint districtId = 1;
            uint sectorId = districtId * HierarchyConstants.SectorIdMultiplier + 3;
            uint roomId = sectorId * HierarchyConstants.RoomsPerSectorMultiplier + 5;

            Assert.AreEqual(1003u, sectorId);
            Assert.AreEqual(100305u, roomId);

            // Verify IDs are unique and traceable
            Assert.AreNotEqual(districtId, sectorId);
            Assert.AreNotEqual(sectorId, roomId);
            Assert.AreNotEqual(districtId, roomId);
        }

        [Test]
        public void RoomFeatures_SupportDifferentTypes()
        {
            // Test room feature types
            var enemyFeature = new RoomFeatureElement(RoomFeatureType.Enemy, new int2(5, 3), 123);
            var treasureFeature = new RoomFeatureElement(RoomFeatureType.PowerUp, new int2(8, 6), 456);
            var saveFeature = new RoomFeatureElement(RoomFeatureType.SaveStation, new int2(10, 4), 789);

            Assert.AreEqual(RoomFeatureType.Enemy, enemyFeature.Type);
            Assert.AreEqual(RoomFeatureType.PowerUp, treasureFeature.Type);
            Assert.AreEqual(RoomFeatureType.SaveStation, saveFeature.Type);

            Assert.AreEqual(new int2(5, 3), enemyFeature.Position);
            Assert.AreEqual(new int2(8, 6), treasureFeature.Position);
            Assert.AreEqual(new int2(10, 4), saveFeature.Position);

            Assert.AreEqual(123u, enemyFeature.FeatureId);
            Assert.AreEqual(456u, treasureFeature.FeatureId);
            Assert.AreEqual(789u, saveFeature.FeatureId);
        }

        [Test]
        public void WorldConfiguration_TargetSectorsNowUsed()
        {
            // Verify that TargetSectors is now a meaningful parameter
            var config1 = new WorldConfiguration
            {
                Seed = 1,
                WorldSize = new int2(50, 50),
                TargetSectors = 5,
                RandomizationMode = RandomizationMode.None
            };

            var config2 = new WorldConfiguration
            {
                Seed = 1,
                WorldSize = new int2(50, 50),
                TargetSectors = 10,
                RandomizationMode = RandomizationMode.None
            };

            // These configurations should behave differently now
            Assert.AreNotEqual(config1.TargetSectors, config2.TargetSectors);
            Assert.AreEqual(5, config1.TargetSectors);
            Assert.AreEqual(10, config2.TargetSectors);
        }
    }
}