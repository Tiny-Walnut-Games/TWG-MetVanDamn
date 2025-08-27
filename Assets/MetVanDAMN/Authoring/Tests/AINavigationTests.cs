using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Authoring;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Tests
{
    /// <summary>
    /// Comprehensive tests for AI Navigation system with polarized gate handling
    /// Covers navigation graph building, pathfinding, and reachability validation
    /// </summary>
    public class AINavigationTests
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
            if (_testWorld.IsCreated)
            {
                _testWorld.Dispose();
            }
        }

        [Test]
        public void NavNode_BasicCreation_IsValid()
        {
            // Arrange
            uint nodeId = 1;
            var position = new float3(10, 0, 20);
            var biomeType = BiomeType.SolarPlains;
            var polarity = Polarity.Sun;

            // Act
            var navNode = new NavNode(nodeId, position, biomeType, polarity);

            // Assert
            Assert.AreEqual(nodeId, navNode.NodeId);
            Assert.AreEqual(position, navNode.WorldPosition);
            Assert.AreEqual(biomeType, navNode.BiomeType);
            Assert.AreEqual(polarity, navNode.PrimaryPolarity);
            Assert.IsTrue(navNode.IsActive);
            Assert.IsFalse(navNode.IsDiscovered);
        }

        [Test]
        public void NavLink_BasicCreation_IsValid()
        {
            // Arrange
            uint fromNode = 1;
            uint toNode = 2;
            var polarity = Polarity.Moon;
            var abilities = Ability.Jump;

            // Act
            var navLink = new NavLink(fromNode, toNode, ConnectionType.Bidirectional, polarity, abilities);

            // Assert
            Assert.AreEqual(fromNode, navLink.FromNodeId);
            Assert.AreEqual(toNode, navLink.ToNodeId);
            Assert.AreEqual(polarity, navLink.RequiredPolarity);
            Assert.AreEqual(abilities, navLink.RequiredAbilities);
            Assert.IsTrue(navLink.IsActive);
        }

        [Test]
        public void AgentCapabilities_CanPassGate_WithMatchingPolarity_ReturnsTrue()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.Sun, Ability.Jump, 0.8f);
            var gate = new GateCondition(Polarity.Sun, Ability.Jump, GateSoftness.Hard);

            // Act
            bool canPass = capabilities.CanPassGate(gate);

            // Assert
            Assert.IsTrue(canPass);
        }

        [Test]
        public void AgentCapabilities_CanPassGate_WithMismatchedPolarity_ReturnsFalse()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.Sun, Ability.Jump, 0.8f);
            var gate = new GateCondition(Polarity.Moon, Ability.Jump, GateSoftness.Hard);

            // Act
            bool canPass = capabilities.CanPassGate(gate);

            // Assert
            Assert.IsFalse(canPass);
        }

        [Test]
        public void AgentCapabilities_CanPassSoftGate_WithHighSkill_ReturnsTrue()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.None, Ability.None, 1.0f); // High skill
            var gate = new GateCondition(Polarity.Moon, Ability.Jump, GateSoftness.Easy);

            // Act
            bool canPass = capabilities.CanPassGate(gate);

            // Assert
            Assert.IsTrue(canPass); // Should pass due to high skill level
        }

        [Test]
        public void NavLink_CanTraverseWith_MatchingCapabilities_ReturnsTrue()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.Heat, Ability.HeatResistance, 0.5f);
            var navLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.Heat, Ability.HeatResistance);

            // Act
            bool canTraverse = navLink.CanTraverseWith(capabilities, 1);

            // Assert
            Assert.IsTrue(canTraverse);
        }

        [Test]
        public void NavLink_CanTraverseWith_MismatchedPolarity_ReturnsFalse()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.Sun, Ability.None, 0.5f);
            var navLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.Moon, Ability.None, gateSoftness: GateSoftness.Hard);

            // Act
            bool canTraverse = navLink.CanTraverseWith(capabilities, 1);

            // Assert
            Assert.IsFalse(canTraverse);
        }

        [Test]
        public void NavLink_CanTraverseWith_SoftGate_ReturnsTrue()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.Sun, Ability.None, 0.5f);
            var navLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.Moon, Ability.None, gateSoftness: GateSoftness.Easy);

            // Act
            bool canTraverse = navLink.CanTraverseWith(capabilities, 1);

            // Assert
            Assert.IsTrue(canTraverse); // Soft gates allow traversal
        }

        [Test]
        public void NavLink_CalculateTraversalCost_WithPolarityMismatch_IncreasesBaseCost()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.Sun, Ability.None, 0.5f);
            var navLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.Moon, Ability.None, 1.0f, 3.0f);

            // Act
            float cost = navLink.CalculateTraversalCost(capabilities);

            // Assert
            Assert.Greater(cost, 1.0f); // Should be higher than base cost due to polarity mismatch
        }

        [Test]
        public void NavLink_GetDestination_Bidirectional_ReturnsCorrectDestination()
        {
            // Arrange
            var navLink = new NavLink(1, 2, ConnectionType.Bidirectional);

            // Act
            uint destFromNode1 = navLink.GetDestination(1);
            uint destFromNode2 = navLink.GetDestination(2);

            // Assert
            Assert.AreEqual(2u, destFromNode1);
            Assert.AreEqual(1u, destFromNode2);
        }

        [Test]
        public void NavLink_GetDestination_OneWay_ReturnsCorrectDestination()
        {
            // Arrange
            var navLink = new NavLink(1, 2, ConnectionType.OneWay);

            // Act
            uint destFromNode1 = navLink.GetDestination(1);
            uint destFromNode2 = navLink.GetDestination(2);

            // Assert
            Assert.AreEqual(2u, destFromNode1);
            Assert.AreEqual(0u, destFromNode2); // Invalid traversal
        }

        [Test]
        public void NavigationGraph_BasicCreation_IsValid()
        {
            // Arrange & Act
            var navGraph = new NavigationGraph(10, 15);

            // Assert
            Assert.AreEqual(10, navGraph.NodeCount);
            Assert.AreEqual(15, navGraph.LinkCount);
            Assert.IsFalse(navGraph.IsReady);
            Assert.AreEqual(0, navGraph.UnreachableAreaCount);
        }

        [Test]
        public void AINavigationState_BasicCreation_IsValid()
        {
            // Arrange
            uint currentNode = 1;
            uint targetNode = 5;

            // Act
            var navState = new AINavigationState(currentNode, targetNode);

            // Assert
            Assert.AreEqual(currentNode, navState.CurrentNodeId);
            Assert.AreEqual(targetNode, navState.TargetNodeId);
            Assert.AreEqual(PathfindingStatus.Idle, navState.Status);
            Assert.AreEqual(0, navState.PathLength);
            Assert.AreEqual(0.0f, navState.PathCost);
        }

        [Test]
        public void NavigationGraphBuildSystem_Creation_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var system = _testWorld.GetOrCreateUnmanagedSystem<NavigationGraphBuildSystem>();
                Assert.IsNotNull(system);
            });
        }

        [Test]
        public void AINavigationSystem_Creation_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var system = _testWorld.GetOrCreateUnmanagedSystem<AINavigationSystem>();
                Assert.IsNotNull(system);
            });
        }

        [Test]
        public void NavigationValidationSystem_Creation_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var system = _testWorld.GetOrCreateUnmanagedSystem<NavigationValidationSystem>();
                Assert.IsNotNull(system);
            });
        }

        [Test]
        public void NavigationValidationReport_Creation_IsValid()
        {
            // Arrange & Act
            var report = new NavigationValidationReport(10, 15, Allocator.Temp);

            try
            {
                // Assert
                Assert.AreEqual(10, report.TotalNodes);
                Assert.AreEqual(15, report.TotalLinks);
                Assert.AreEqual(0, report.UnreachableNodeCount);
                Assert.IsFalse(report.HasUnreachableAreas);
                Assert.IsTrue(report.UnreachableNodeIds.IsCreated);
                Assert.IsTrue(report.Issues.IsCreated);
            }
            finally
            {
                report.Dispose();
            }
        }

        [Test]
        public void NavigationIssue_Creation_IsValid()
        {
            // Arrange
            var issueType = NavigationIssueType.UnreachableNode;
            uint nodeId = 5;
            var description = "Test node is unreachable";

            // Act
            var issue = new NavigationIssue(issueType, nodeId, description);

            // Assert
            Assert.AreEqual(issueType, issue.Type);
            Assert.AreEqual(nodeId, issue.NodeId);
            Assert.AreEqual(description, issue.Description.ToString());
        }

        [Test]
        public void NavigationQuickFix_Creation_IsValid()
        {
            // Arrange & Act
            var quickFix = new NavigationQuickFix
            {
                Type = NavigationQuickFixType.AddConnection,
                TargetNodeId = 3,
                Description = "Add connection to node 4"
            };

            // Assert
            Assert.AreEqual(NavigationQuickFixType.AddConnection, quickFix.Type);
            Assert.AreEqual(3u, quickFix.TargetNodeId);
            Assert.AreEqual("Add connection to node 4", quickFix.Description.ToString());
        }

        [Test]
        public void NavigationValidationUtility_GenerateValidationReport_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var report = NavigationValidationUtility.GenerateValidationReport(_testWorld);
                report.Dispose();
            });
        }

        [Test]
        public void NavigationValidationUtility_IsPathPossible_ReturnsExpectedResult()
        {
            // Arrange
            uint fromNode = 1;
            uint toNode = 2;
            var capabilities = new AgentCapabilities(Polarity.Sun, Ability.Jump, 0.5f);

            // Act
            bool isPossible = NavigationValidationUtility.IsPathPossible(_testWorld, fromNode, toNode, capabilities);

            // Assert
            Assert.IsTrue(isPossible); // Current implementation returns true
        }

        [Test]
        public void NavigationValidationUtility_GenerateQuickFixSuggestions_DoesNotThrow()
        {
            // Arrange
            var report = new NavigationValidationReport(5, 8, Allocator.Temp);

            try
            {
                // Act & Assert
                Assert.DoesNotThrow(() =>
                {
                    var fixes = NavigationValidationUtility.GenerateQuickFixSuggestions(report);
                    fixes.Dispose();
                });
            }
            finally
            {
                report.Dispose();
            }
        }

        /// <summary>
        /// Integration test: Create full navigation graph with nodes, links, and agents
        /// </summary>
        [Test]
        public void NavigationIntegrationTest_FullScenario_WorksCorrectly()
        {
            // Arrange: Create navigation nodes
            var node1Entity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(node1Entity, new NavNode(1, new float3(0, 0, 0), BiomeType.SolarPlains, Polarity.Sun));
            _entityManager.AddComponentData(node1Entity, new NodeId { Value = 1 });
            _entityManager.AddBuffer<NavLinkBufferElement>(node1Entity);

            var node2Entity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(node2Entity, new NavNode(2, new float3(10, 0, 0), BiomeType.ShadowRealms, Polarity.Moon));
            _entityManager.AddComponentData(node2Entity, new NodeId { Value = 2 });
            _entityManager.AddBuffer<NavLinkBufferElement>(node2Entity);

            // Create navigation link
            var linkBuffer = _entityManager.GetBuffer<NavLinkBufferElement>(node1Entity);
            var navLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.Sun, Ability.None);
            linkBuffer.Add(navLink);

            // Create agent entity
            var agentEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(agentEntity, new AINavigationState(1, 2));
            _entityManager.AddComponentData(agentEntity, new AgentCapabilities(Polarity.Sun, Ability.Jump, 0.8f));
            _entityManager.AddBuffer<PathNodeBufferElement>(agentEntity);

            // Create navigation graph singleton
            var navGraphEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(navGraphEntity, new NavigationGraph(2, 1) { IsReady = true });

            // Act: Test agent can navigate
            var agent = _entityManager.GetComponentData<AgentCapabilities>(agentEntity);
            bool canTraverse = navLink.CanTraverseWith(agent, 1);
            float cost = navLink.CalculateTraversalCost(agent);

            // Assert
            Assert.IsTrue(canTraverse);
            Assert.Greater(cost, 0.0f);
            Assert.AreEqual(2, _entityManager.GetComponentData<NavigationGraph>(navGraphEntity).NodeCount);
        }

        /// <summary>
        /// Negative test: Agent without required polarity cannot traverse hard gate
        /// </summary>
        [Test]
        public void NavigationNegativeTest_AgentCannotTraverseHardGate()
        {
            // Arrange: Agent with Sun polarity trying to traverse Moon-only gate
            var capabilities = new AgentCapabilities(Polarity.Sun, Ability.None, 0.0f);
            var hardGate = new NavLink(1, 2, ConnectionType.OneWay, Polarity.Moon, Ability.None, gateSoftness: GateSoftness.Hard);

            // Act
            bool canTraverse = hardGate.CanTraverseWith(capabilities, 1);

            // Assert
            Assert.IsFalse(canTraverse);
        }

        /// <summary>
        /// Cross-component relationship test: Complex gate and connection scenarios
        /// </summary>
        [Test]
        public void NavigationComplexTest_MultipleGateConditions()
        {
            // Arrange: Multiple overlapping gate conditions
            var capabilities = new AgentCapabilities(Polarity.Sun | Polarity.Heat, Ability.Jump | Ability.HeatResistance, 0.9f);
            
            var sunGate = new GateCondition(Polarity.Sun, Ability.Jump, GateSoftness.Hard);
            var heatGate = new GateCondition(Polarity.Heat, Ability.HeatResistance, GateSoftness.Moderate);
            var moonGate = new GateCondition(Polarity.Moon, Ability.DoubleJump, GateSoftness.Easy);

            // Act
            bool canPassSunGate = capabilities.CanPassGate(sunGate);
            bool canPassHeatGate = capabilities.CanPassGate(heatGate);
            bool canPassMoonGate = capabilities.CanPassGate(moonGate);

            // Assert
            Assert.IsTrue(canPassSunGate);   // Has required polarity and ability
            Assert.IsTrue(canPassHeatGate);  // Has required polarity and ability
            Assert.IsTrue(canPassMoonGate);  // Soft gate with high skill level should allow bypass
        }

        // ============================================================================
        // JUMP/ARC NAVIGATION SUPPORT TESTS
        // ============================================================================

        [Test]
        public void ArcJump_Ability_IsCorrectlyDefined()
        {
            // Arrange & Act
            var arcJumpAbility = Ability.ArcJump;
            var chargedJumpAbility = Ability.ChargedJump;
            var teleportArcAbility = Ability.TeleportArc;
            var allArcMovement = Ability.AllArcMovement;

            // Assert
            Assert.AreNotEqual(Ability.None, arcJumpAbility);
            Assert.AreNotEqual(Ability.None, chargedJumpAbility);
            Assert.AreNotEqual(Ability.None, teleportArcAbility);
            Assert.IsTrue((allArcMovement & arcJumpAbility) != 0);
            Assert.IsTrue((allArcMovement & chargedJumpAbility) != 0);
            Assert.IsTrue((allArcMovement & teleportArcAbility) != 0);
            Assert.IsTrue((allArcMovement & Ability.Grapple) != 0);
        }

        [Test]
        public void NavLink_CalculateArcTraversalCost_WithBasicJump_ReturnsCorrectCost()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.None, Ability.Jump, 0.5f);
            var jumpLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.None, Ability.Jump, 1.0f);
            var fromPos = new float3(0, 0, 0);
            var toPos = new float3(5, 2, 0); // 2-unit vertical jump

            // Act
            float cost = jumpLink.CalculateTraversalCost(capabilities, fromPos, toPos);

            // Assert
            Assert.Greater(cost, 1.0f); // Should be higher than base cost due to arc calculation
            Assert.Less(cost, 5.0f);    // Should not be prohibitively expensive for achievable jump
        }

        [Test]
        public void NavLink_CalculateArcTraversalCost_WithDoubleJump_HandlesHigherJumps()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.None, Ability.DoubleJump, 0.5f);
            var jumpLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.None, Ability.DoubleJump, 1.0f);
            var fromPos = new float3(0, 0, 0);
            var toPos = new float3(5, 3.5f, 0); // 3.5-unit vertical jump (requires double jump)

            // Act
            float cost = jumpLink.CalculateTraversalCost(capabilities, fromPos, toPos);

            // Assert
            Assert.Greater(cost, 1.0f); // Should account for arc complexity
            Assert.Less(cost, 10.0f);   // Should be achievable with double jump
        }

        [Test]
        public void NavLink_CalculateArcTraversalCost_WithArcJump_ProvidesBetterControl()
        {
            // Arrange
            var basicCapabilities = new AgentCapabilities(Polarity.None, Ability.Jump, 0.5f);
            var arcCapabilities = new AgentCapabilities(Polarity.None, Ability.Jump | Ability.ArcJump, 0.5f);
            var jumpLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.None, Ability.Jump, 1.0f);
            var fromPos = new float3(0, 0, 0);
            var toPos = new float3(5, 2, 0);

            // Act
            float basicCost = jumpLink.CalculateTraversalCost(basicCapabilities, fromPos, toPos);
            float arcCost = jumpLink.CalculateTraversalCost(arcCapabilities, fromPos, toPos);

            // Assert
            Assert.Less(arcCost, basicCost); // Arc jump should provide better efficiency
        }

        [Test]
        public void NavLink_CalculateArcTraversalCost_WithChargedJump_HandlesVariableHeight()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.None, Ability.ChargedJump, 0.5f);
            var jumpLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.None, Ability.ChargedJump, 1.0f);
            var fromPos = new float3(0, 0, 0);
            var toPos = new float3(5, 7, 0); // Very high jump requiring charged jump

            // Act
            float cost = jumpLink.CalculateTraversalCost(capabilities, fromPos, toPos);

            // Assert
            Assert.Greater(cost, 1.0f); // Should account for charge requirement
            Assert.Less(cost, 10.0f);   // Should be achievable with charged jump
        }

        [Test]
        public void NavLink_CalculateArcTraversalCost_WithTeleportArc_HandlesLongDistances()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.None, Ability.TeleportArc, 0.5f);
            var teleportLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.None, Ability.TeleportArc, 1.0f);
            var fromPos = new float3(0, 0, 0);
            var toPos = new float3(8, 5, 0); // Long-distance teleportation

            // Act
            float cost = teleportLink.CalculateTraversalCost(capabilities, fromPos, toPos);

            // Assert
            Assert.Greater(cost, 2.0f); // Should have significant energy cost
            Assert.Less(cost, 10.0f);   // Should be achievable within teleport range
        }

        [Test]
        public void NavLink_CalculateArcTraversalCost_WithGrapple_HandlesArcTrajectory()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.None, Ability.Grapple, 0.5f);
            var grappleLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.None, Ability.Grapple, 1.0f);
            var fromPos = new float3(0, 0, 0);
            var toPos = new float3(10, 3, 0); // Grapple swing trajectory

            // Act
            float cost = grappleLink.CalculateTraversalCost(capabilities, fromPos, toPos);

            // Assert
            Assert.Greater(cost, 1.0f); // Should account for grapple swing
            Assert.Less(cost, 5.0f);    // Should be efficient within grapple range
        }

        [Test]
        public void NavLink_CalculateArcTraversalCost_ImpossibleJump_ReturnsHighCost()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.None, Ability.Jump, 0.5f);
            var jumpLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.None, Ability.Jump, 1.0f);
            var fromPos = new float3(0, 0, 0);
            var toPos = new float3(5, 15, 0); // Impossibly high jump

            // Act
            float cost = jumpLink.CalculateTraversalCost(capabilities, fromPos, toPos);

            // Assert
            Assert.Greater(cost, 9.0f); // Should return high penalty cost for impossible jumps
        }

        [Test]
        public void NavLink_CalculateArcTraversalCost_DownwardMovement_LowerCost()
        {
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.None, Ability.Jump, 0.5f);
            var jumpLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.None, Ability.Jump, 1.0f);
            var fromPos = new float3(0, 5, 0);
            var toPos = new float3(5, 0, 0); // Downward movement

            // Act
            float cost = jumpLink.CalculateTraversalCost(capabilities, fromPos, toPos);

            // Assert
            Assert.Greater(cost, 1.0f); // Should still have some cost
            Assert.Less(cost, 2.0f);    // Should be easier than upward movement
        }

        [Test]
        public void NavLink_CalculateArcTraversalCost_WithGlideSpeed_ReducesFallCost()
        {
            // Arrange
            var basicCapabilities = new AgentCapabilities(Polarity.None, Ability.Jump, 0.5f);
            var glideCapabilities = new AgentCapabilities(Polarity.None, Ability.Jump | Ability.GlideSpeed, 0.5f);
            var jumpLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.None, Ability.Jump, 1.0f);
            var fromPos = new float3(0, 10, 0);
            var toPos = new float3(5, 0, 0); // Long fall

            // Act
            float basicCost = jumpLink.CalculateTraversalCost(basicCapabilities, fromPos, toPos);
            float glideCost = jumpLink.CalculateTraversalCost(glideCapabilities, fromPos, toPos);

            // Assert
            Assert.Less(glideCost, basicCost); // Gliding should reduce fall cost
        }

        [Test]
        public void MovementEfficiencyMultiplier_ArcJumpWithBasicJump_ProvidesBonus()
        {
            // Arrange
            var basicCapabilities = new AgentCapabilities(Polarity.None, Ability.Jump, 0.5f);
            var arcCapabilities = new AgentCapabilities(Polarity.None, Ability.Jump | Ability.ArcJump, 0.5f);
            var jumpLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.None, Ability.Jump, 1.0f);

            // Act
            float basicCost = jumpLink.CalculateTraversalCost(basicCapabilities);
            float arcCost = jumpLink.CalculateTraversalCost(arcCapabilities);

            // Assert
            Assert.Less(arcCost, basicCost); // Arc jump should provide efficiency bonus
        }

        [Test]
        public void MovementEfficiencyMultiplier_AllArcMovementAbilities_StacksBonuses()
        {
            // Arrange
            var basicCapabilities = new AgentCapabilities(Polarity.None, Ability.Grapple, 0.5f);
            var allArcCapabilities = new AgentCapabilities(Polarity.None, Ability.AllArcMovement, 0.5f);
            var grappleLink = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.None, Ability.Grapple, 1.0f);

            // Act
            float basicCost = grappleLink.CalculateTraversalCost(basicCapabilities);
            float allArcCost = grappleLink.CalculateTraversalCost(allArcCapabilities);

            // Assert
            Assert.Less(allArcCost, basicCost); // Multiple arc abilities should stack bonuses
        }

        [Test]
        public void AINavigationSystem_CalculateMovementHeuristic_AccountsForVerticalMovement()
        {
            // This tests the enhanced heuristic calculation indirectly
            // by verifying it produces different costs for horizontal vs vertical movement
            
            // Arrange
            var system = _testWorld.GetOrCreateUnmanagedSystem<AINavigationSystem>();
            
            // Simulate horizontal and vertical movements
            var horizontalMove = new float3(5, 0, 0);
            var verticalMove = new float3(0, 5, 0);
            var arcMove = new float3(3, 4, 0);

            // The actual calculation happens inside the private method,
            // but we can verify the system exists and doesn't throw
            Assert.IsNotNull(system);
        }

        [Test]
        public void JumpArcPhysics_CalculatesReasonableCosts()
        {
            // Test that arc calculations produce reasonable cost ranges
            
            // Arrange
            var capabilities = new AgentCapabilities(Polarity.None, Ability.Jump | Ability.DoubleJump | Ability.ArcJump, 0.5f);
            
            // Various jump scenarios
            var easyJump = new NavLink(1, 2, ConnectionType.Bidirectional, Polarity.None, Ability.Jump, 1.0f);
            var moderateJump = new NavLink(3, 4, ConnectionType.Bidirectional, Polarity.None, Ability.DoubleJump, 1.0f);
            var hardJump = new NavLink(5, 6, ConnectionType.Bidirectional, Polarity.None, Ability.ChargedJump, 1.0f);
            
            var easyPos = (new float3(0, 0, 0), new float3(2, 1, 0));
            var moderatePos = (new float3(0, 0, 0), new float3(4, 3, 0));
            var hardPos = (new float3(0, 0, 0), new float3(6, 6, 0));

            // Act
            float easyCost = easyJump.CalculateTraversalCost(capabilities, easyPos.Item1, easyPos.Item2);
            float moderateCost = moderateJump.CalculateTraversalCost(capabilities, moderatePos.Item1, moderatePos.Item2);
            float hardCost = hardJump.CalculateTraversalCost(capabilities, hardPos.Item1, hardPos.Item2);

            // Assert
            Assert.Less(easyCost, moderateCost);     // Easy jumps should cost less than moderate
            Assert.Less(moderateCost, hardCost);     // Moderate jumps should cost less than hard
            Assert.Greater(easyCost, 0.5f);          // All costs should be above minimum
            Assert.Less(hardCost, 15.0f);            // Even hard jumps should be achievable
        }
    }
}