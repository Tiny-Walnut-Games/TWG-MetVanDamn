using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Cinemachine;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Graph.Tests
{
    /// <summary>
    /// Tests for the Procedural Room Generation Master Spec implementation
    /// Validates the complete pipeline flow from room creation to navigation and camera generation
    /// </summary>
    public class ProceduralRoomGenerationTests
    {
        private World _world;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _world = new World("Test World");
            _entityManager = _world.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            if (_world?.IsCreated == true)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void ProceduralRoomGenerator_CreatesRoomTemplate_WithCorrectCapabilityTags()
        {
            // Arrange
            var roomEntity = CreateTestRoom(RoomType.Boss, new RectInt(0, 0, 10, 8));
            var worldConfig = CreateWorldConfiguration();
            
            // Create and update the procedural room generator system
            var system = _world.CreateSystemManaged<ProceduralRoomGeneratorSystem>();
            system.Update();

            // Assert
            Assert.IsTrue(_entityManager.HasComponent<RoomTemplate>(roomEntity));
            Assert.IsTrue(_entityManager.HasComponent<ProceduralRoomGenerated>(roomEntity));
            
            var template = _entityManager.GetComponentData<RoomTemplate>(roomEntity);
            var genStatus = _entityManager.GetComponentData<ProceduralRoomGenerated>(roomEntity);
            
            // Boss rooms should use pattern-driven generation
            Assert.AreEqual(RoomGeneratorType.PatternDrivenModular, template.GeneratorType);
            
            // Should have movement capability requirements
            Assert.AreNotEqual(Ability.None, template.CapabilityTags.RequiredSkills);
            Assert.IsTrue(template.RequiresJumpValidation);
            
            // Generation status should be initialized
            Assert.IsFalse(genStatus.NavigationGenerated);
            Assert.IsFalse(genStatus.CinemachineGenerated);
            Assert.AreNotEqual(0u, genStatus.GenerationSeed);
        }

        [Test]
        public void NavigationGenerator_CreatesMovementConnections_WithCorrectAbilityTags()
        {
            // Arrange
            var roomEntity = CreateTestRoomWithTemplate();
            
            // Create and update systems in order
            var roomGenSystem = _world.CreateSystemManaged<ProceduralRoomGeneratorSystem>();
            var navGenSystem = _world.CreateSystemManaged<RoomNavigationGeneratorSystem>();
            
            roomGenSystem.Update();
            navGenSystem.Update();

            // Assert
            Assert.IsTrue(_entityManager.HasBuffer<RoomNavigationElement>(roomEntity));
            
            var navBuffer = _entityManager.GetBuffer<RoomNavigationElement>(roomEntity);
            Assert.Greater(navBuffer.Length, 0);
            
            var genStatus = _entityManager.GetComponentData<ProceduralRoomGenerated>(roomEntity);
            Assert.IsTrue(genStatus.NavigationGenerated);
            
            // Check that navigation connections have movement type tags
            bool hasJumpConnection = false;
            for (int i = 0; i < navBuffer.Length; i++)
            {
                var connection = navBuffer[i];
                if ((connection.RequiredMovement & Ability.Jump) != 0)
                {
                    hasJumpConnection = true;
                    break;
                }
            }
            Assert.IsTrue(hasJumpConnection);
        }

        [Test]
        public void CinemachineZoneGenerator_CreatesBiomeSpecificCameraSettings()
        {
            // Arrange
            var roomEntity = CreateTestRoomWithTemplate();
            
            // Create and update all systems in pipeline order
            var roomGenSystem = _world.CreateSystemManaged<ProceduralRoomGeneratorSystem>();
            var navGenSystem = _world.CreateSystemManaged<RoomNavigationGeneratorSystem>();
            var cameraGenSystem = _world.CreateSystemManaged<CinemachineZoneGeneratorSystem>();
            
            roomGenSystem.Update();
            navGenSystem.Update();
            cameraGenSystem.Update();

            // Assert
            Assert.IsTrue(_entityManager.HasComponent<CinemachineZoneData>(roomEntity));
            Assert.IsTrue(_entityManager.HasComponent<CinemachineGameObjectReference>(roomEntity));
            
            var cameraZone = _entityManager.GetComponentData<CinemachineZoneData>(roomEntity);
            var genStatus = _entityManager.GetComponentData<ProceduralRoomGenerated>(roomEntity);
            
            Assert.IsTrue(genStatus.CinemachineGenerated);
            Assert.Greater(cameraZone.Priority, 0);
            Assert.Greater(cameraZone.BlendTime, 0.0f);
            
            // Camera should be positioned within reasonable bounds
            var roomData = _entityManager.GetComponentData<RoomHierarchyData>(roomEntity);
            var roomBounds = roomData.Bounds;
            Assert.IsTrue(cameraZone.CameraPosition.x >= roomBounds.x - 5 && 
                         cameraZone.CameraPosition.x <= roomBounds.x + roomBounds.width + 5);
        }

        [Test]
        public void JumpArcSolver_ValidatesReachability_WithDifferentMovementTypes()
        {
            // Arrange
            var physics = new JumpArcPhysics();
            var startPos = new int2(0, 0);
            var targetPos = new int2(3, 2);
            
            // Test different movement capabilities
            var basicMovement = Ability.Jump;
            var advancedMovement = Ability.Jump | Ability.DoubleJump | Ability.Dash;
            
            // Act & Assert
            bool reachableBasic = JumpArcSolver.IsPositionReachable(startPos, targetPos, basicMovement, physics);
            bool reachableAdvanced = JumpArcSolver.IsPositionReachable(startPos, targetPos, advancedMovement, physics);
            
            // Advanced movement should allow reaching more positions
            Assert.IsTrue(reachableAdvanced);
            
            // Test dash reachability
            var dashTarget = new int2(5, 0); // Horizontal dash target
            bool reachableWithDash = JumpArcSolver.IsPositionReachable(startPos, dashTarget, Ability.Dash, physics);
            Assert.IsTrue(reachableWithDash);
        }

        [Test]
        public void MovementCapabilityTags_DetermineCorrectSkillRequirements()
        {
            // Arrange & Act
            var movementTags = new MovementCapabilityTags(
                Ability.Dash | Ability.WallJump,
                Ability.DoubleJump,
                BiomeAffinity.Mountain,
                0.8f
            );

            // Assert
            Assert.AreEqual(Ability.Dash | Ability.WallJump, movementTags.RequiredSkills);
            Assert.AreEqual(Ability.DoubleJump, movementTags.OptionalSkills);
            Assert.AreEqual(BiomeAffinity.Mountain, movementTags.BiomeType);
            Assert.AreEqual(0.8f, movementTags.DifficultyRating);
        }

        [Test]
        public void RoomTemplate_InitializesWithCorrectDefaults()
        {
            // Arrange
            var capabilityTags = new MovementCapabilityTags(Ability.Jump, Ability.None, BiomeAffinity.Forest, 0.5f);
            var minSize = new int2(4, 4);
            var maxSize = new int2(12, 8);

            // Act
            var template = new RoomTemplate(
                RoomGeneratorType.WeightedTilePrefab,
                capabilityTags,
                minSize,
                maxSize,
                0.2f,
                true,
                123u
            );

            // Assert
            Assert.AreEqual(RoomGeneratorType.WeightedTilePrefab, template.GeneratorType);
            Assert.AreEqual(0.2f, template.SecretAreaPercentage);
            Assert.IsTrue(template.RequiresJumpValidation);
            Assert.AreEqual(123u, template.TemplateId);
            Assert.AreEqual(Ability.Jump, template.CapabilityTags.RequiredSkills);
        }

        [Test]
        public void CompleteGenerationPipeline_ProcessesRoomCorrectly()
        {
            // Arrange - Create a room and world configuration
            var roomEntity = CreateTestRoom(RoomType.Normal, new RectInt(0, 0, 8, 6));
            CreateWorldConfiguration();

            // Act - Run complete pipeline
            var roomGenSystem = _world.CreateSystemManaged<ProceduralRoomGeneratorSystem>();
            var navGenSystem = _world.CreateSystemManaged<RoomNavigationGeneratorSystem>();
            var cameraGenSystem = _world.CreateSystemManaged<CinemachineZoneGeneratorSystem>();
            
            roomGenSystem.Update();
            navGenSystem.Update();
            cameraGenSystem.Update();

            // Assert - All pipeline stages should be complete
            var genStatus = _entityManager.GetComponentData<ProceduralRoomGenerated>(roomEntity);
            Assert.IsTrue(genStatus.ContentGenerated || _entityManager.HasComponent<RoomTemplate>(roomEntity));
            Assert.IsTrue(genStatus.NavigationGenerated);
            Assert.IsTrue(genStatus.CinemachineGenerated);
            
            // Should have all expected components
            Assert.IsTrue(_entityManager.HasComponent<RoomTemplate>(roomEntity));
            Assert.IsTrue(_entityManager.HasBuffer<RoomNavigationElement>(roomEntity));
            Assert.IsTrue(_entityManager.HasComponent<CinemachineZoneData>(roomEntity));
            Assert.IsTrue(_entityManager.HasComponent<CinemachineGameObjectReference>(roomEntity));
        }

        private Entity CreateTestRoom(RoomType roomType, RectInt bounds)
        {
            var roomEntity = _entityManager.CreateEntity();
            var nodeId = new NodeId(12345u, 2, 1000u, new int2(bounds.x, bounds.y));
            var roomData = new RoomHierarchyData(bounds, roomType, true);
            
            _entityManager.AddComponentData(roomEntity, nodeId);
            _entityManager.AddComponentData(roomEntity, roomData);
            _entityManager.AddBuffer<RoomNavigationElement>(roomEntity);
            
            return roomEntity;
        }

        private Entity CreateTestRoomWithTemplate()
        {
            var roomEntity = CreateTestRoom(RoomType.Normal, new RectInt(0, 0, 8, 6));
            
            var capabilityTags = new MovementCapabilityTags(Ability.Jump, Ability.DoubleJump, BiomeAffinity.Forest, 0.5f);
            var template = new RoomTemplate(
                RoomGeneratorType.WeightedTilePrefab,
                capabilityTags,
                new int2(4, 3),
                new int2(8, 6),
                0.15f,
                true,
                999u
            );
            
            _entityManager.AddComponentData(roomEntity, template);
            _entityManager.AddComponentData(roomEntity, new ProceduralRoomGenerated(12345u));
            
            return roomEntity;
        }

        private Entity CreateWorldConfiguration()
        {
            var configEntity = _entityManager.CreateEntity();
            var worldConfig = new WorldConfiguration
            {
                Seed = 12345,
                WorldSize = new int2(50, 50),
                TargetSectors = 8,
                RandomizationMode = RandomizationMode.Partial
            };
            
            _entityManager.AddComponentData(configEntity, worldConfig);
            return configEntity;
        }
    }
}
