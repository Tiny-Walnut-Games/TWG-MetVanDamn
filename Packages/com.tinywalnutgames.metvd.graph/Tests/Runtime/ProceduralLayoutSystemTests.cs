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
    /// Tests for the procedural layout system
    /// </summary>
    public class ProceduralLayoutSystemTests
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
            if (_testWorld != null && _testWorld.IsCreated)
            {
                _testWorld.Dispose();
            }
        }

        [Test]
        public void DistrictLayoutSystem_WithUnplacedDistricts_ShouldAssignCoordinates()
        {
            // Arrange
            var worldConfig = _entityManager.CreateEntity();
            _entityManager.AddComponentData(worldConfig, new WorldConfiguration
            {
                Seed = 12345,
                WorldSize = new int2(32, 32),
                TargetSectors = 5,
                RandomizationMode = RandomizationMode.Partial
            });

            // Create unplaced districts
            var district1 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(district1, new NodeId(1, 0, 0, new int2(0, 0)));
            _entityManager.AddComponentData(district1, new WfcState());

            var district2 = _entityManager.CreateEntity();
            _entityManager.AddComponentData(district2, new NodeId(2, 0, 0, new int2(0, 0)));
            _entityManager.AddComponentData(district2, new WfcState());

            // Act
            var layoutSystemHandle = _testWorld.CreateSystem<DistrictLayoutSystem>();
            _testWorld.Update();

            // Assert
            var node1 = _entityManager.GetComponentData<NodeId>(district1);
            var node2 = _entityManager.GetComponentData<NodeId>(district2);

            // Should no longer be at (0,0)
            Assert.That(node1.Coordinates.x != 0 || node1.Coordinates.y != 0, "District 1 should be moved from (0,0)");
            Assert.That(node2.Coordinates.x != 0 || node2.Coordinates.y != 0, "District 2 should be moved from (0,0)");

            // Check that layout done tag was created
            using var layoutDoneQuery = _entityManager.CreateEntityQuery(typeof(DistrictLayoutDoneTag));
            Assert.That(layoutDoneQuery.CalculateEntityCount(), Is.EqualTo(1), "Should create DistrictLayoutDoneTag");
        }

        [Test]
        public void RuleRandomizationSystem_WithPartialMode_ShouldRandomizeBiomes()
        {
            // Arrange
            var worldConfig = _entityManager.CreateEntity();
            _entityManager.AddComponentData(worldConfig, new WorldConfiguration
            {
                Seed = 54321,
                WorldSize = new int2(16, 16),
                TargetSectors = 3,
                RandomizationMode = RandomizationMode.Partial
            });

            var layoutDone = _entityManager.CreateEntity();
            _entityManager.AddComponentData(layoutDone, new DistrictLayoutDoneTag(3, 0));

            // Act
            var ruleSystemHandle = _testWorld.CreateSystem<RuleRandomizationSystem>();
            _testWorld.Update();

            // Assert
            using var ruleQuery = _entityManager.CreateEntityQuery(typeof(WorldRuleSet));
            Assert.That(ruleQuery.CalculateEntityCount(), Is.EqualTo(1), "Should create WorldRuleSet");

            var ruleSet = ruleQuery.GetSingleton<WorldRuleSet>();
            Assert.That(ruleSet.BiomePolarityMask, Is.Not.EqualTo(Polarity.None), "Should have assigned biome polarities");
            Assert.That(ruleSet.UpgradesRandomized, Is.False, "Upgrades should not be randomized in Partial mode");

            using var rulesDoneQuery = _entityManager.CreateEntityQuery(typeof(RuleRandomizationDoneTag));
            Assert.That(rulesDoneQuery.CalculateEntityCount(), Is.EqualTo(1), "Should create RuleRandomizationDoneTag");
        }

        [Test]
        public void RuleRandomizationSystem_WithFullMode_ShouldRandomizeEverything()
        {
            // Arrange
            var worldConfig = _entityManager.CreateEntity();
            _entityManager.AddComponentData(worldConfig, new WorldConfiguration
            {
                Seed = 98765,
                WorldSize = new int2(48, 48),
                TargetSectors = 8,
                RandomizationMode = RandomizationMode.Full
            });

            var layoutDone = _entityManager.CreateEntity();
            _entityManager.AddComponentData(layoutDone, new DistrictLayoutDoneTag(8, 0));

            // Act
            var ruleSystemHandle2 = _testWorld.CreateSystem<RuleRandomizationSystem>();
            _testWorld.Update();

            // Assert
            using var ruleQuery = _entityManager.CreateEntityQuery(typeof(WorldRuleSet));
            var ruleSet = ruleQuery.GetSingleton<WorldRuleSet>();
            
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
            var worldConfig = _entityManager.CreateEntity();
            _entityManager.AddComponentData(worldConfig, new WorldConfiguration
            {
                Seed = 11111,
                WorldSize = new int2(24, 24),
                TargetSectors = 4,
                RandomizationMode = RandomizationMode.None
            });

            var layoutDone = _entityManager.CreateEntity();
            _entityManager.AddComponentData(layoutDone, new DistrictLayoutDoneTag(4, 0));

            // Act
            var ruleSystemHandle3 = _testWorld.CreateSystem<RuleRandomizationSystem>();
            _testWorld.Update();

            // Assert
            using var ruleQuery = _entityManager.CreateEntityQuery(typeof(WorldRuleSet));
            var ruleSet = ruleQuery.GetSingleton<WorldRuleSet>();
            
            Assert.That(ruleSet.UpgradesRandomized, Is.False, "Upgrades should not be randomized in None mode");
            
            // Should have curated polarity distribution
            var expectedCuratedPolarities = Polarity.Sun | Polarity.Moon | Polarity.Heat | Polarity.Cold;
            Assert.That(ruleSet.BiomePolarityMask, Is.EqualTo(expectedCuratedPolarities), "Should use curated polarity distribution");
        }
    }
}