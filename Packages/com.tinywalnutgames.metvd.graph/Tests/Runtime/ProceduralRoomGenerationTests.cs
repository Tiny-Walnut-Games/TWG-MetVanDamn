using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;

namespace TinyWalnutGames.MetVD.Graph.Tests
{
    /// <summary>
    /// Tests for the Procedural Room Generation Best Fit Matrix & Pipeline implementation
    /// </summary>
    public class ProceduralRoomGenerationTests
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
        public void RoomGenerationRequest_InitializesCorrectly()
        {
            // Arrange
            var generatorType = RoomGeneratorType.PatternDrivenModular;
            var targetBiome = BiomeType.SolarPlains;
            var targetPolarity = Polarity.Sun;
            var availableSkills = Ability.Jump | Ability.Dash;
            uint seed = 12345;

            // Act
            var request = new RoomGenerationRequest(generatorType, targetBiome, targetPolarity, availableSkills, seed);

            // Assert
            Assert.AreEqual(1, request.CurrentStep);
            Assert.AreEqual(generatorType, request.GeneratorType);
            Assert.AreEqual(targetBiome, request.TargetBiome);
            Assert.AreEqual(targetPolarity, request.TargetPolarity);
            Assert.AreEqual(availableSkills, request.AvailableSkills);
            Assert.AreEqual(seed, request.GenerationSeed);
            Assert.IsFalse(request.IsComplete);
        }

        [Test]
        public void SkillTag_CompatibilityCheck()
        {
            // Arrange
            var skillTag = new SkillTag(Ability.Dash, Ability.WallJump, 0.8f);

            // Act & Assert
            Assert.AreEqual(Ability.Dash, skillTag.RequiredSkill);
            Assert.AreEqual(Ability.WallJump, skillTag.OptionalSkill);
            Assert.AreEqual(0.8f, skillTag.SkillDifficulty);
        }

        [Test]
        public void BiomeAffinity_CompatibilityCheck()
        {
            // Arrange
            var biomeAffinity = new BiomeAffinity(BiomeType.SolarPlains, Polarity.Sun, 0.8f);

            // Act & Assert
            Assert.IsTrue(biomeAffinity.IsCompatibleWith(BiomeType.SolarPlains, Polarity.Sun));
            Assert.IsFalse(biomeAffinity.IsCompatibleWith(BiomeType.ShadowRealms, Polarity.Moon));
            Assert.IsTrue(biomeAffinity.IsCompatibleWith(BiomeType.SolarPlains, Polarity.Sun | Polarity.Heat)); // Contains Sun
        }

        [Test]
        public void JumpArcSolver_ReachabilityCalculation()
        {
            // Arrange
            var startPos = new float2(0, 0);
            var targetPos = new float2(5, 3);
            var physics = new JumpPhysicsData(4.0f, 6.0f, 9.81f, 5.0f, false, false, false);

            // Act
            bool isReachable = JumpArcSolver.IsReachable(startPos, targetPos, physics);

            // Assert
            Assert.IsTrue(isReachable, "Target should be reachable within jump physics constraints");
        }

        [Test]
        public void JumpArcSolver_UnreachableTarget()
        {
            // Arrange
            var startPos = new float2(0, 0);
            var targetPos = new float2(10, 8); // Beyond max distance and height
            var physics = new JumpPhysicsData(4.0f, 6.0f, 9.81f, 5.0f, false, false, false);

            // Act
            bool isReachable = JumpArcSolver.IsReachable(startPos, targetPos, physics);

            // Assert
            Assert.IsFalse(isReachable, "Target should be unreachable due to physics constraints");
        }

        [Test]
        public void JumpArcSolver_JumpVectorCalculation()
        {
            // Arrange
            var platforms = new NativeArray<float2>(3, Allocator.Temp);
            platforms[0] = new float2(0, 0);
            platforms[1] = new float2(4, 2);
            platforms[2] = new float2(8, 0);
            
            var physics = new JumpPhysicsData(4.0f, 6.0f, 9.81f, 5.0f, false, false, false);

            // Act
            var jumpVectors = JumpArcSolver.CalculateJumpVectors(platforms, physics, Allocator.Temp);

            // Assert
            Assert.IsTrue(jumpVectors.Length > 0, "Should generate valid jump vectors between reachable platforms");
            
            // Cleanup
            platforms.Dispose();
            jumpVectors.Dispose();
        }

        [Test]
        public void SecretAreaConfig_ValidatesCorrectly()
        {
            // Arrange & Act
            var config = new SecretAreaConfig(0.2f, new int2(3, 3), new int2(6, 6), Ability.Bomb, true, false);

            // Assert
            Assert.AreEqual(0.2f, config.SecretAreaPercentage);
            Assert.AreEqual(new int2(3, 3), config.MinSecretSize);
            Assert.AreEqual(new int2(6, 6), config.MaxSecretSize);
            Assert.AreEqual(Ability.Bomb, config.SecretSkillRequirement);
            Assert.IsTrue(config.UseDestructibleWalls);
            Assert.IsFalse(config.UseAlternateRoutes);
        }

        [Test]
        public void JumpPhysicsData_InitializesWithDefaults()
        {
            // Arrange & Act
            var physics = new JumpPhysicsData();

            // Assert
            Assert.AreEqual(4.0f, physics.MaxJumpHeight);
            Assert.AreEqual(6.0f, physics.MaxJumpDistance);
            Assert.AreEqual(9.81f, physics.Gravity);
            Assert.AreEqual(5.0f, physics.MovementSpeed);
            Assert.IsFalse(physics.HasDoubleJump);
            Assert.IsFalse(physics.HasWallJump);
            Assert.IsFalse(physics.HasDash);
        }

        [Test]
        public void RoomGeneratorType_AllTypesValid()
        {
            // Test that all generator types from Best Fit Matrix are present
            Assert.IsTrue(System.Enum.IsDefined(typeof(RoomGeneratorType), RoomGeneratorType.PatternDrivenModular));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RoomGeneratorType), RoomGeneratorType.ParametricChallenge));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RoomGeneratorType), RoomGeneratorType.WeightedTilePrefab));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RoomGeneratorType), RoomGeneratorType.StackedSegment));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RoomGeneratorType), RoomGeneratorType.LinearBranchingCorridor));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RoomGeneratorType), RoomGeneratorType.BiomeWeightedHeightmap));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RoomGeneratorType), RoomGeneratorType.LayeredPlatformCloud));
        }

        [Test]
        public void RoomGenerationPipeline_StepProgression()
        {
            // Arrange
            var entity = _entityManager.CreateEntity();
            var roomData = new RoomHierarchyData(new RectInt(0, 0, 10, 8), RoomType.Normal, true);
            var nodeId = new NodeId(1, 2, 1, new int2(5, 5));
            var request = new RoomGenerationRequest(
                RoomGeneratorType.WeightedTilePrefab, 
                BiomeType.HubArea, 
                Polarity.None, 
                Ability.Jump, 
                12345
            );

            _entityManager.AddComponentData(entity, roomData);
            _entityManager.AddComponentData(entity, nodeId);
            _entityManager.AddComponentData(entity, request);

            // Act - simulate pipeline step progression
            request.CurrentStep = 2;
            request.LayoutType = RoomLayoutType.Horizontal;
            _entityManager.SetComponentData(entity, request);

            request.CurrentStep = 3;
            _entityManager.SetComponentData(entity, request);

            request.CurrentStep = 6;
            request.IsComplete = true;
            _entityManager.SetComponentData(entity, request);

            // Assert
            var finalRequest = _entityManager.GetComponentData<RoomGenerationRequest>(entity);
            Assert.AreEqual(6, finalRequest.CurrentStep);
            Assert.IsTrue(finalRequest.IsComplete);
            Assert.AreEqual(RoomLayoutType.Horizontal, finalRequest.LayoutType);
        }

        [Test]
        public void MinimumPlatformSpacing_CalculatesCorrectly()
        {
            // Arrange
            var physics = new JumpPhysicsData(4.0f, 6.0f, 9.81f, 5.0f, false, false, false);
            float difficulty = 0.7f; // 70% difficulty

            // Act
            var spacing = JumpArcSolver.CalculateMinimumPlatformSpacing(physics, difficulty);

            // Assert
            Assert.IsTrue(spacing.x > 1.0f && spacing.x < physics.MaxJumpDistance);
            Assert.IsTrue(spacing.y > 0.5f && spacing.y < physics.MaxJumpHeight);
        }

        [Test]
        public void JumpArcValidation_StoresResults()
        {
            // Arrange & Act
            var validation = new JumpArcValidation(true, 5, 4);

            // Assert
            Assert.IsTrue(validation.IsValidated);
            Assert.IsTrue(validation.IsReachable);
            Assert.AreEqual(5, validation.TestedConnections);
            Assert.AreEqual(4, validation.ValidConnections);
        }

        [Test]
        public void CloudMotionType_AllTypesValid()
        {
            // Test that all cloud motion types are present for sky biome generation
            Assert.IsTrue(System.Enum.IsDefined(typeof(CloudMotionType), CloudMotionType.Gentle));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CloudMotionType), CloudMotionType.Gusty));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CloudMotionType), CloudMotionType.Conveyor));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CloudMotionType), CloudMotionType.Electric));
        }
    }
}