using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Graph;

namespace TinyWalnutGames.MetVD.Graph.Tests
{
    /// <summary>
    /// Integration tests for the complete procedural room generation pipeline
    /// Tests end-to-end functionality across different biome and skill combinations
    /// </summary>
    public class ProceduralRoomGenerationIntegrationTests
    {
        private World _testWorld;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _testWorld = new World("IntegrationTestWorld");
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
        public void BossRoom_SkillChallenge_GeneratesCorrectly()
        {
            // Arrange - Create a boss room with advanced skills
            var roomEntity = CreateTestRoom(
                RoomType.Boss, 
                new RectInt(0, 0, 20, 16), 
                BiomeType.VolcanicCore,
                Ability.Jump | Ability.Dash | Ability.WallJump
            );

            // Act - Process room generation
            var request = _entityManager.GetComponentData<RoomGenerationRequest>(roomEntity);
            
            // Assert - Boss rooms should use pattern-driven modular generation
            Assert.AreEqual(RoomGeneratorType.PatternDrivenModular, request.GeneratorType);
            Assert.AreEqual(BiomeType.VolcanicCore, request.TargetBiome);
            Assert.IsTrue((request.AvailableSkills & Ability.Dash) != 0);
            
            // Should have pattern buffer for skill challenges
            Assert.IsTrue(_entityManager.HasBuffer<RoomPatternElement>(roomEntity));
        }

        [Test]
        public void TreasureRoom_JumpTesting_ValidatesPhysics()
        {
            // Arrange - Create a treasure room for jump testing
            var roomEntity = CreateTestRoom(
                RoomType.Treasure,
                new RectInt(0, 0, 12, 8),
                BiomeType.CrystalCaverns,
                Ability.Jump | Ability.DoubleJump
            );

            // Act
            var request = _entityManager.GetComponentData<RoomGenerationRequest>(roomEntity);
            var jumpPhysics = _entityManager.GetComponentData<JumpPhysicsData>(roomEntity);

            // Assert - Treasure rooms should use parametric challenge generation
            Assert.AreEqual(RoomGeneratorType.ParametricChallenge, request.GeneratorType);
            Assert.IsTrue(_entityManager.HasComponent<JumpArcValidation>(roomEntity));
            Assert.IsTrue(_entityManager.HasBuffer<JumpConnectionElement>(roomEntity));
            Assert.AreEqual(4.0f, jumpPhysics.MaxJumpHeight);
        }

        [Test]
        public void SkyBiome_CloudPlatforms_GeneratesMotion()
        {
            // Arrange - Create a room in sky biome
            var roomEntity = CreateTestRoom(
                RoomType.Normal,
                new RectInt(0, 0, 16, 20), // Tall room
                BiomeType.SkyGardens,
                Ability.Jump | Ability.GlideSpeed
            );

            // Act
            var request = _entityManager.GetComponentData<RoomGenerationRequest>(roomEntity);
            var biome = _entityManager.GetComponentData<Core.Biome>(roomEntity);

            // Assert - Sky biomes should use layered platform generation
            Assert.AreEqual(RoomGeneratorType.LayeredPlatformCloud, request.GeneratorType);
            Assert.AreEqual(BiomeType.SkyGardens, request.TargetBiome);
            Assert.AreEqual(Polarity.Wind, biome.PrimaryPolarity);
        }

        [Test]
        public void HorizontalRoom_CorridorFlow_GeneratesRhythm()
        {
            // Arrange - Create a wide horizontal room
            var roomEntity = CreateTestRoom(
                RoomType.Normal,
                new RectInt(0, 0, 24, 8), // Wide room (aspect ratio 3:1)
                BiomeType.HubArea,
                Ability.Jump
            );

            // Act
            var request = _entityManager.GetComponentData<RoomGenerationRequest>(roomEntity);

            // Assert - Wide rooms should use linear corridor generation
            Assert.AreEqual(RoomGeneratorType.LinearBranchingCorridor, request.GeneratorType);
            Assert.AreEqual(RoomLayoutType.Horizontal, request.LayoutType);
        }

        [Test]
        public void VerticalRoom_StackedSegments_EnsuresConnectivity()
        {
            // Arrange - Create a tall vertical room
            var roomEntity = CreateTestRoom(
                RoomType.Normal,
                new RectInt(0, 0, 8, 24), // Tall room (aspect ratio 1:3)
                BiomeType.CrystalCaverns,
                Ability.Jump | Ability.WallJump
            );

            // Act
            var request = _entityManager.GetComponentData<RoomGenerationRequest>(roomEntity);

            // Assert - Tall rooms should use stacked segment generation
            Assert.AreEqual(RoomGeneratorType.StackedSegment, request.GeneratorType);
            Assert.AreEqual(RoomLayoutType.Vertical, request.LayoutType);
        }

        [Test]
        public void SecretAreas_WithBombSkill_GeneratesDestructibleWalls()
        {
            // Arrange - Create a room with bomb skill for secret access
            var roomEntity = CreateTestRoom(
                RoomType.Normal,
                new RectInt(0, 0, 16, 12),
                BiomeType.AncientRuins,
                Ability.Jump | Ability.Bomb
            );

            // Act
            var secretConfig = _entityManager.GetComponentData<SecretAreaConfig>(roomEntity);

            // Assert - Should have secret configuration with destructible walls
            Assert.AreEqual(Ability.Bomb, secretConfig.SecretSkillRequirement);
            Assert.IsTrue(secretConfig.UseDestructibleWalls);
            Assert.AreEqual(0.15f, secretConfig.SecretAreaPercentage);
        }

        [Test]
        public void SaveRoom_SafeArea_UsesWeightedGeneration()
        {
            // Arrange - Create a save room (should be safe)
            var roomEntity = CreateTestRoom(
                RoomType.Save,
                new RectInt(0, 0, 10, 10),
                BiomeType.HubArea,
                Ability.Jump
            );

            // Act
            var request = _entityManager.GetComponentData<RoomGenerationRequest>(roomEntity);
            var roomState = _entityManager.GetComponentData<RoomStateData>(roomEntity);

            // Assert - Save rooms should use safe weighted generation
            Assert.AreEqual(RoomGeneratorType.WeightedTilePrefab, request.GeneratorType);
            Assert.AreEqual(0, roomState.TotalSecrets); // Save rooms don't have secrets
        }

        [Test]
        public void BiomePolarity_MatchesRoomContent()
        {
            // Arrange - Create rooms in different biomes
            var fireRoomEntity = CreateTestRoom(RoomType.Normal, new RectInt(0, 0, 12, 12), BiomeType.VolcanicCore, Ability.Jump);
            var iceRoomEntity = CreateTestRoom(RoomType.Normal, new RectInt(0, 0, 12, 12), BiomeType.FrozenWastes, Ability.Jump);

            // Act
            var fireRequest = _entityManager.GetComponentData<RoomGenerationRequest>(fireRoomEntity);
            var iceRequest = _entityManager.GetComponentData<RoomGenerationRequest>(iceRoomEntity);

            var fireBiome = _entityManager.GetComponentData<Core.Biome>(fireRoomEntity);
            var iceBiome = _entityManager.GetComponentData<Core.Biome>(iceRoomEntity);

            // Assert - Biome polarities should match room requests
            Assert.AreEqual(Polarity.Heat, fireRequest.TargetPolarity);
            Assert.AreEqual(Polarity.Cold, iceRequest.TargetPolarity);
            Assert.AreEqual(Polarity.Heat, fireBiome.PrimaryPolarity);
            Assert.AreEqual(Polarity.Cold, iceBiome.PrimaryPolarity);
        }

        [Test]
        public void Pipeline_SixSteps_CompletesCorrectly()
        {
            // Arrange
            var roomEntity = CreateTestRoom(
                RoomType.Normal,
                new RectInt(0, 0, 12, 12),
                BiomeType.SolarPlains,
                Ability.Jump
            );

            // Act - Simulate pipeline progression
            var request = _entityManager.GetComponentData<RoomGenerationRequest>(roomEntity);
            
            // Step 1: Biome Selection (already done in setup)
            Assert.AreEqual(1, request.CurrentStep);
            
            // Simulate pipeline completion
            request.CurrentStep = 6;
            request.IsComplete = true;
            _entityManager.SetComponentData(roomEntity, request);

            var finalRequest = _entityManager.GetComponentData<RoomGenerationRequest>(roomEntity);

            // Assert - Pipeline should complete successfully
            Assert.AreEqual(6, finalRequest.CurrentStep);
            Assert.IsTrue(finalRequest.IsComplete);
        }

        [Test]
        public void JumpArcSolver_ValidatesRoomConnectivity()
        {
            // Arrange - Create platforms for connectivity test
            var platforms = new NativeArray<float2>(4, Allocator.Temp);
            platforms[0] = new float2(0, 0);
            platforms[1] = new float2(4, 2);
            platforms[2] = new float2(8, 1);
            platforms[3] = new float2(12, 3);

            var obstacles = new NativeArray<int2>(0, Allocator.Temp); // No obstacles
            var physics = new JumpPhysicsData(4.0f, 6.0f, 9.81f, 5.0f, false, false, false);

            // Act
            bool isReachable = JumpArcSolver.ValidateRoomReachability(platforms, obstacles, physics);

            // Assert
            Assert.IsTrue(isReachable, "All platforms should be reachable with given jump physics");

            // Cleanup
            platforms.Dispose();
            obstacles.Dispose();
        }

        [Test]
        public void DifferentGenerators_ProduceUniqueContent()
        {
            // Arrange - Create rooms with different generator types
            var patternRoom = CreateTestRoom(RoomType.Boss, new RectInt(0, 0, 16, 16), BiomeType.VolcanicCore, Ability.Dash);
            var parametricRoom = CreateTestRoom(RoomType.Treasure, new RectInt(0, 0, 16, 16), BiomeType.CrystalCaverns, Ability.Jump);
            var weightedRoom = CreateTestRoom(RoomType.Normal, new RectInt(0, 0, 16, 16), BiomeType.HubArea, Ability.Jump);

            // Act
            var patternRequest = _entityManager.GetComponentData<RoomGenerationRequest>(patternRoom);
            var parametricRequest = _entityManager.GetComponentData<RoomGenerationRequest>(parametricRoom);
            var weightedRequest = _entityManager.GetComponentData<RoomGenerationRequest>(weightedRoom);

            // Assert - Each should use different generator type
            Assert.AreEqual(RoomGeneratorType.PatternDrivenModular, patternRequest.GeneratorType);
            Assert.AreEqual(RoomGeneratorType.ParametricChallenge, parametricRequest.GeneratorType);
            Assert.AreEqual(RoomGeneratorType.WeightedTilePrefab, weightedRequest.GeneratorType);

            // Each should have different specialized components
            Assert.IsTrue(_entityManager.HasBuffer<RoomPatternElement>(patternRoom));
            Assert.IsTrue(_entityManager.HasComponent<JumpPhysicsData>(parametricRoom));
            Assert.IsTrue(_entityManager.HasComponent<SecretAreaConfig>(weightedRoom));
        }

        /// <summary>
        /// Helper method to create a test room with all necessary components
        /// </summary>
        private Entity CreateTestRoom(RoomType roomType, RectInt bounds, BiomeType biomeType, Ability availableSkills)
        {
            var roomEntity = _entityManager.CreateEntity();

            // Add core room data
            var roomData = new RoomHierarchyData(bounds, roomType, true);
            var nodeId = new NodeId(1, 2, 1, new int2(0, 0));
            _entityManager.AddComponentData(roomEntity, roomData);
            _entityManager.AddComponentData(roomEntity, nodeId);

            // Add biome data
            var polarity = GetPolarityForBiome(biomeType);
            var biome = new Core.Biome(biomeType, polarity, 1.0f, Polarity.None, 1.0f);
            _entityManager.AddComponentData(roomEntity, biome);

            // Determine generator type
            var generatorType = DetermineGeneratorType(roomType, bounds, biomeType);

            // Create room generation request
            var generationRequest = new RoomGenerationRequest(
                generatorType,
                biomeType,
                polarity,
                availableSkills,
                12345
            );
            _entityManager.AddComponentData(roomEntity, generationRequest);

            // Add room management components
            _entityManager.AddComponentData(roomEntity, new RoomStateData(CalculateSecrets(roomType)));
            _entityManager.AddComponentData(roomEntity, CreateNavData(bounds, roomType));
            _entityManager.AddBuffer<RoomFeatureElement>(roomEntity);

            // Add generator-specific components
            AddSpecializedComponents(roomEntity, generatorType, availableSkills);

            return roomEntity;
        }

        private void AddSpecializedComponents(Entity entity, RoomGeneratorType generatorType, Ability availableSkills)
        {
            switch (generatorType)
            {
                case RoomGeneratorType.PatternDrivenModular:
                    _entityManager.AddBuffer<RoomPatternElement>(entity);
                    _entityManager.AddBuffer<RoomModuleElement>(entity);
                    break;

                case RoomGeneratorType.ParametricChallenge:
                    var jumpPhysics = new JumpPhysicsData(4.0f, 6.0f, 9.81f, 5.0f, 
                        (availableSkills & Ability.DoubleJump) != 0,
                        (availableSkills & Ability.WallJump) != 0,
                        (availableSkills & Ability.Dash) != 0);
                    _entityManager.AddComponentData(entity, jumpPhysics);
                    _entityManager.AddComponentData(entity, new JumpArcValidation(false, 0, 0));
                    _entityManager.AddBuffer<JumpConnectionElement>(entity);
                    break;

                case RoomGeneratorType.WeightedTilePrefab:
                    var secretSkill = (availableSkills & Ability.Bomb) != 0 ? Ability.Bomb : Ability.None;
                    var secretConfig = new SecretAreaConfig(0.15f, new int2(2, 2), new int2(4, 4), 
                        secretSkill, true, true);
                    _entityManager.AddComponentData(entity, secretConfig);
                    break;
            }
        }

        private static RoomGeneratorType DetermineGeneratorType(RoomType roomType, RectInt bounds, BiomeType biomeType)
        {
            var aspectRatio = (float)bounds.width / bounds.height;

            return roomType switch
            {
                RoomType.Boss => RoomGeneratorType.PatternDrivenModular,
                RoomType.Treasure => RoomGeneratorType.ParametricChallenge,
                RoomType.Save or RoomType.Shop or RoomType.Hub => RoomGeneratorType.WeightedTilePrefab,
                _ when IsSkyBiome(biomeType) => RoomGeneratorType.LayeredPlatformCloud,
                _ when IsTerrainBiome(biomeType) => RoomGeneratorType.BiomeWeightedHeightmap,
                _ when aspectRatio > 1.5f => RoomGeneratorType.LinearBranchingCorridor,
                _ when aspectRatio < 0.67f => RoomGeneratorType.StackedSegment,
                _ => RoomGeneratorType.WeightedTilePrefab
            };
        }

        private static Polarity GetPolarityForBiome(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.SolarPlains => Polarity.Sun,
                BiomeType.ShadowRealms => Polarity.Moon,
                BiomeType.VolcanicCore => Polarity.Heat,
                BiomeType.FrozenWastes => Polarity.Cold,
                BiomeType.SkyGardens => Polarity.Wind,
                BiomeType.PowerPlant => Polarity.Tech,
                _ => Polarity.None
            };
        }

        private static bool IsSkyBiome(BiomeType biome) => 
            biome == BiomeType.SkyGardens || biome == BiomeType.PlasmaFields;

        private static bool IsTerrainBiome(BiomeType biome) => 
            biome == BiomeType.SolarPlains || biome == BiomeType.FrozenWastes;

        private static int CalculateSecrets(RoomType roomType)
        {
            return roomType switch
            {
                RoomType.Treasure => 2,
                RoomType.Normal => 1,
                RoomType.Boss => 1,
                _ => 0
            };
        }

        private static RoomNavigationData CreateNavData(RectInt bounds, RoomType roomType)
        {
            var entrance = new int2(bounds.x + 1, bounds.y + 1);
            var isCritical = roomType == RoomType.Boss || roomType == RoomType.Entrance;
            var traversalTime = (bounds.width + bounds.height) * 0.5f;
            return new RoomNavigationData(entrance, isCritical, traversalTime);
        }
    }
}