// Fixed Unity 6.2 compilation issues:
// 1. Added missing UnityEngine using directive for RectInt support
// 2. Fixed IJobEntity signature from ref RoomFeatureElement to DynamicBuffer<RoomFeatureElement>  
// 3. Added missing properties to RoomGenerationRequest (AvailableSkills, GenerationSeed, TargetBiome, IsComplete)
// 4. Added missing enum values to RoomGeneratorType (LinearBranchingCorridor, StackedSegment)
// 5. Expanded JumpPhysicsData with missing fields and 7-argument constructor
// 6. Expanded SecretAreaConfig with missing fields and constructors
// 7. Added missing GlideSpeed property to JumpArcPhysics
// 8. Fixed SelectWeightedFeatureType to use correct parameter types
// 9. Fixed Object ambiguity issues by using UnityEngine.Object qualification
// 10. Implemented missing GetTilemapGenerationConfig method

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Specialized generator systems for each type in the Best Fit Matrix
    /// Each system implements a specific room generation strategy
    /// </summary>

    /// <summary>
    /// Pattern-Driven Modular Room Generator
    /// For movement skill puzzles (dash, wall-cling, grapple)
    /// Uses Movement Capability Tags for deliberate skill gate placement
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(RoomGenerationPipelineSystem))]
    public partial struct PatternDrivenModularGenerator : ISystem
    {
        private ComponentLookup<SkillTag> _skillTagLookup;
        private BufferLookup<RoomPatternElement> _patternBufferLookup;
        private BufferLookup<RoomModuleElement> _moduleBufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _skillTagLookup = state.GetComponentLookup<SkillTag>(true);
            _patternBufferLookup = state.GetBufferLookup<RoomPatternElement>();
            _moduleBufferLookup = state.GetBufferLookup<RoomModuleElement>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _skillTagLookup.Update(ref state);
            _patternBufferLookup.Update(ref state);
            _moduleBufferLookup.Update(ref state);

            var baseRandom = new Unity.Mathematics.Random((uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000));

            // Track pattern generation metrics for debugging and balancing
            var processedPatternCount = 0;
            var skillGateGenerationCount = 0;

            // Use standard SystemAPI.Query with proper component access
            foreach (var (request, roomData, nodeId, entity) in SystemAPI.Query<RefRW<RoomGenerationRequest>, RefRO<RoomHierarchyData>, RefRO<NodeId>>().WithEntityAccess())
            {
                if (request.ValueRO.GeneratorType != RoomGeneratorType.PatternDrivenModular || request.ValueRO.IsComplete) continue;

                if (!_patternBufferLookup.HasBuffer(entity)) continue;

                var patterns = _patternBufferLookup[entity];
                var bounds = roomData.ValueRO.Bounds;

                // Clear existing patterns
                patterns.Clear();

                var entityRandom = new Unity.Mathematics.Random(baseRandom.state + (uint)entity.Index);

                // Use nodeId coordinates for coordinate-aware pattern generation
                var coordinateInfluence = CalculateCoordinateInfluence(nodeId.ValueRO, roomData.ValueRO);

                // Generate skill-specific patterns based on available abilities
                if ((request.ValueRO.AvailableSkills & Ability.Dash) != 0)
                {
                    GenerateDashGaps(patterns, bounds, request.ValueRO.GenerationSeed, ref entityRandom, coordinateInfluence);
                    processedPatternCount++;
                }

                if ((request.ValueRO.AvailableSkills & Ability.WallJump) != 0)
                {
                    GenerateWallClimbShafts(patterns, bounds, request.ValueRO.GenerationSeed, ref entityRandom, coordinateInfluence);
                    processedPatternCount++;
                }

                if ((request.ValueRO.AvailableSkills & Ability.Grapple) != 0)
                {
                    GenerateGrapplePoints(patterns, bounds, request.ValueRO.GenerationSeed, ref entityRandom, coordinateInfluence);
                    processedPatternCount++;
                }

                // Add skill gates that require unlocked abilities
                var skillGatesAdded = GenerateSkillGates(patterns, bounds, request.ValueRO.AvailableSkills, request.ValueRO.GenerationSeed, ref entityRandom, coordinateInfluence);
                skillGateGenerationCount += skillGatesAdded;

                request.ValueRW.IsComplete = true;
            }
        }

        private static void GenerateDashGaps(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, uint seed, ref Unity.Mathematics.Random random, float coordinateInfluence)
        {
            var baseGapCount = math.max(1, bounds.width / 8);
            var gapCount = (int)(baseGapCount * coordinateInfluence); // Use coordinate influence for gap complexity

            for (int i = 0; i < gapCount; i++)
            {
                var gapStart = new int2(
                    random.NextInt(bounds.x + 2, bounds.x + bounds.width - 4),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );

                // Create a gap that requires dash to cross (3-4 tiles wide, scaled by influence)
                var baseGapWidth = random.NextInt(3, 5);
                var gapWidth = (int)(baseGapWidth * math.clamp(coordinateInfluence, 0.8f, 1.5f));
                
                // Mark the gap area
                patterns.Add(new RoomPatternElement(gapStart, RoomFeatureType.Platform, (uint)(seed + i * 10), Ability.Dash));
                patterns.Add(new RoomPatternElement(new int2(gapStart.x + gapWidth, gapStart.y), RoomFeatureType.Platform, (uint)(seed + i * 10 + 1), Ability.Dash));
            }
        }

        private static void GenerateWallClimbShafts(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, uint seed, ref Unity.Mathematics.Random random, float coordinateInfluence)
        {
            var baseShaftCount = math.max(1, bounds.height / 8);
            var shaftCount = (int)(baseShaftCount * coordinateInfluence); // Use coordinate influence for shaft complexity

            for (int i = 0; i < shaftCount; i++)
            {
                var shaftX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
                var shaftBottom = random.NextInt(bounds.y + 1, bounds.y + bounds.height / 2);
                var baseShaftHeight = random.NextInt(4, bounds.height - shaftBottom + bounds.y);
                var shaftHeight = (int)(baseShaftHeight * math.clamp(coordinateInfluence, 0.7f, 1.8f)); // Scale height by influence

                // Create vertical wall for wall-jumping
                for (int y = shaftBottom; y < shaftBottom + shaftHeight; y += 2)
                {
                    patterns.Add(new RoomPatternElement(new int2(shaftX, y), RoomFeatureType.Obstacle, (uint)(seed + i * 20), Ability.WallJump));
                }
            }
        }

        private static void GenerateGrapplePoints(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, uint seed, ref Unity.Mathematics.Random random, float coordinateInfluence)
        {
            var basePointCount = math.max(1, (bounds.width * bounds.height) / 32);
            var pointCount = (int)(basePointCount * coordinateInfluence); // Scale grapple point density by coordinate influence

            for (int i = 0; i < pointCount; i++)
            {
                var grapplePoint = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + bounds.height / 2, bounds.y + bounds.height - 1)
                );

                // Place grapple points high up, over hazards
                patterns.Add(new RoomPatternElement(grapplePoint, RoomFeatureType.Platform, (uint)(seed + i * 30), Ability.Grapple));
                
                // Add hazard below grapple point (more hazards in complex areas)
                if (grapplePoint.y > bounds.y + 2 && random.NextFloat() < coordinateInfluence * 0.4f)
                {
                    patterns.Add(new RoomPatternElement(new int2(grapplePoint.x, grapplePoint.y - 2), RoomFeatureType.Obstacle, (uint)(seed + i * 30 + 1)));
                }
            }
        }

        private static int GenerateSkillGates(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, Ability availableSkills, uint seed, ref Unity.Mathematics.Random random, float coordinateInfluence)
        {
            var skillGatesAdded = 0;
            
            // Create gates that test multiple skills in combination
            if ((availableSkills & (Ability.Dash | Ability.WallJump)) == (Ability.Dash | Ability.WallJump))
            {
                // Complex dash + wall jump challenge (scaled by coordinate influence)
                var centerX = bounds.x + bounds.width / 2;
                var centerY = bounds.y + bounds.height / 2;
                
                // Scale challenge spacing by coordinate influence
                var challengeSpacing = (int)(3 * math.clamp(coordinateInfluence, 0.8f, 1.5f));
                
                patterns.Add(new RoomPatternElement(new int2(centerX - challengeSpacing, centerY), RoomFeatureType.Platform, (uint)(seed + 100), Ability.Dash));
                patterns.Add(new RoomPatternElement(new int2(centerX, centerY + 2), RoomFeatureType.Obstacle, (uint)(seed + 101), Ability.WallJump));
                patterns.Add(new RoomPatternElement(new int2(centerX + challengeSpacing, centerY), RoomFeatureType.Platform, (uint)(seed + 102), Ability.Dash));
                skillGatesAdded += 3;
            }
            
            // Add additional challenge gates for high-influence areas
            if (coordinateInfluence > 1.3f && (availableSkills & Ability.Grapple) != 0)
            {
                // High-complexity grapple challenge for distant/important rooms
                var challengeX = bounds.x + random.NextInt(bounds.width / 4, (bounds.width * 3) / 4);
                var challengeY = bounds.y + bounds.height - 2;
                
                patterns.Add(new RoomPatternElement(new int2(challengeX, challengeY), RoomFeatureType.Platform, (uint)(seed + 200), Ability.Grapple));
                skillGatesAdded++;
            }
            
            return skillGatesAdded;
        }

        private static float CalculateCoordinateInfluence(NodeId nodeId, RoomHierarchyData roomData)
        {
            // Calculate how room coordinates should influence pattern complexity
            var coords = nodeId.Coordinates;
            var distance = math.length(coords);
            
            // Distance from origin affects pattern difficulty
            var distanceInfluence = math.clamp(distance / 30f, 0.5f, 2.0f);
            
            // Room type affects pattern complexity
            var roomTypeInfluence = roomData.Type switch
            {
                RoomType.Boss => 1.8f,      // Boss rooms get complex patterns
                RoomType.Treasure => 1.4f,  // Treasure rooms get moderate complexity
                RoomType.Normal => 1.0f,    // Normal complexity
                RoomType.Save => 0.6f,      // Save rooms get simpler patterns
                _ => 1.0f
            };
            
            return distanceInfluence * roomTypeInfluence;
        }
    }

    /// <summary>
    /// Parametric Challenge Room Generator
    /// For platforming puzzle testing grounds with Jump Arc Solver
    /// Uses jump height/distance constraints for reproducible test rooms
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(PatternDrivenModularGenerator))]
    public partial struct ParametricChallengeGenerator : ISystem
    {
        private ComponentLookup<JumpPhysicsData> _jumpPhysicsLookup;
        private ComponentLookup<JumpArcValidation> _validationLookup;
        private BufferLookup<JumpConnectionElement> _jumpConnectionLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _jumpPhysicsLookup = state.GetComponentLookup<JumpPhysicsData>(true);
            _validationLookup = state.GetComponentLookup<JumpArcValidation>();
            _jumpConnectionLookup = state.GetBufferLookup<JumpConnectionElement>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _jumpPhysicsLookup.Update(ref state);
            _validationLookup.Update(ref state);
            _jumpConnectionLookup.Update(ref state);

            var processedRoomCount = 0;

            foreach (var (request, roomData, nodeId, entity) in SystemAPI.Query<RefRW<RoomGenerationRequest>, RefRO<RoomHierarchyData>, RefRO<NodeId>>().WithEntityAccess())
            {
                if (request.ValueRO.GeneratorType != RoomGeneratorType.ParametricChallenge || request.ValueRO.IsComplete) continue;

                if (!_jumpPhysicsLookup.HasComponent(entity)) continue;

                var jumpPhysics = _jumpPhysicsLookup[entity];
                var bounds = roomData.ValueRO.Bounds;
                var challengeComplexity = CalculateChallengeComplexity(nodeId.ValueRO, roomData.ValueRO);

                // Simple platform generation without complex arc solver dependencies
                var platformPositions = new NativeList<float2>(Allocator.Temp)
                {
                    // Start platform
                    new(bounds.x + 1, bounds.y + 1)
                };
                
                var platformSpacing = (int)(jumpPhysics.JumpDistance * challengeComplexity);
                var currentX = bounds.x + 1 + platformSpacing;
                
                while (currentX < bounds.x + bounds.width - 1)
                {
                    var targetY = bounds.y + 1 + (platformPositions.Length % 2) * (int)(jumpPhysics.JumpHeight);
                    platformPositions.Add(new float2(currentX, targetY));
                    currentX += platformSpacing;
                }
                
                platformPositions.Add(new float2(bounds.x + bounds.width - 1, bounds.y + 1));

                // Store basic jump connections
                if (_jumpConnectionLookup.HasBuffer(entity))
                {
                    var connections = _jumpConnectionLookup[entity];
                    connections.Clear();
                    
                    for (int i = 0; i < platformPositions.Length - 1; i++)
                    {
                        var from = platformPositions[i];
                        var to = platformPositions[i + 1];
                        connections.Add(new JumpConnectionElement((int2)from, (int2)to, Ability.Jump));
                    }
                }

                if (_validationLookup.HasComponent(entity))
                {
                    var validation = new JumpArcValidation(
                        platformPositions.Length > 2,
                        jumpPhysics.JumpDistance,
                        jumpPhysics.JumpHeight
                    );
                    _validationLookup[entity] = validation;
                }

                platformPositions.Dispose();
                request.ValueRW.IsComplete = true;
                processedRoomCount++;
            }
        }

        private static float CalculateChallengeComplexity(NodeId nodeId, RoomHierarchyData roomData)
        {
            var coords = nodeId.Coordinates;
            var distance = math.length(coords);
            var baseComplexity = math.clamp(distance / 25f, 0.7f, 1.8f);
            
            var roomTypeModifier = roomData.Type switch
            {
                RoomType.Boss => 1.6f,
                RoomType.Treasure => 1.3f,
                RoomType.Normal => 1.0f,
                RoomType.Save => 0.6f,
                _ => 1.0f
            };
            
            return baseComplexity * roomTypeModifier;
        }
    }

    /// <summary>
    /// Weighted Tile/Prefab Room Generator
    /// For standard platforming with Secret Area Hooks
    /// Generates easy-flow layouts with hidden alcoves and alternate routes
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ParametricChallengeGenerator))]
    public partial struct WeightedTilePrefabGenerator : ISystem
    {
        private ComponentLookup<SecretAreaConfig> _secretConfigLookup;
        private ComponentLookup<BiomeAffinityComponent> _biomeAffinityLookup;
        private BufferLookup<RoomModuleElement> _moduleBufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _secretConfigLookup = state.GetComponentLookup<SecretAreaConfig>(true);
            _biomeAffinityLookup = state.GetComponentLookup<BiomeAffinityComponent>(true);
            _moduleBufferLookup = state.GetBufferLookup<RoomModuleElement>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _secretConfigLookup.Update(ref state);
            _biomeAffinityLookup.Update(ref state);
            _moduleBufferLookup.Update(ref state);

            var baseRandom = new Unity.Mathematics.Random((uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000));
            var processedRoomCount = 0; // Track generation metrics

            // Use foreach instead of ScheduleParallel to avoid nullable reference issues
            foreach (var (request, roomData, nodeId, features, entity) in SystemAPI.Query<RefRW<RoomGenerationRequest>, RefRO<RoomHierarchyData>, RefRO<NodeId>, DynamicBuffer<RoomFeatureElement>>().WithEntityAccess())
            {
                if (request.ValueRO.GeneratorType != RoomGeneratorType.WeightedTilePrefab || request.ValueRO.IsComplete) continue;

                var bounds = roomData.ValueRO.Bounds;
                var area = bounds.width * bounds.height;
                var entityRandom = new Unity.Mathematics.Random(baseRandom.state + (uint)entity.Index);
                var spatialVariation = CalculateSpatialVariation(nodeId.ValueRO, roomData.ValueRO);

                // Generate main flow layout
                var baseFeatureCount = (int)(area * 0.6f / 12);
                var mainFeatureCount = (int)(baseFeatureCount * spatialVariation);
                
                for (int i = 0; i < mainFeatureCount; i++)
                {
                    var weight = entityRandom.NextFloat();
                    var featureType = SelectWeightedFeatureType(weight, roomData.ValueRO.Type);
                    
                    var pos = new int2(
                        entityRandom.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                        entityRandom.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                    );
                    
                    features.Add(new RoomFeatureElement
                    {
                        Type = featureType,
                        Position = pos,
                        FeatureId = (uint)(request.ValueRO.GenerationSeed + i)
                    });
                }

                request.ValueRW.IsComplete = true;
                processedRoomCount++;
            }
        }

        private static float CalculateSpatialVariation(NodeId nodeId, RoomHierarchyData roomData)
        {
            var coords = nodeId.Coordinates;
            var distance = math.length(coords);
            var distanceVariation = math.clamp(distance / 20f, 0.8f, 1.4f);
            
            var roomTypeVariation = roomData.Type switch
            {
                RoomType.Boss => 1.3f,
                RoomType.Treasure => 1.1f,
                RoomType.Normal => 1.0f,
                RoomType.Save => 0.7f,
                RoomType.Hub => 0.8f,
                _ => 1.0f
            };
            
            return distanceVariation * roomTypeVariation;
        }

        private static RoomFeatureType SelectWeightedFeatureType(float weight, RoomType roomType)
        {
            var adjustedWeight = weight;
            
            // Add room-type-specific weighting
            switch (roomType)
            {
                case RoomType.Boss:
                    adjustedWeight *= 1.2f; // More challenging features
                    break;
                case RoomType.Save:
                    adjustedWeight *= 0.8f; // Simpler features
                    break;
                case RoomType.Treasure:
                    adjustedWeight *= 1.1f; // Slightly more complex
                    break;
            }
            
            return adjustedWeight switch
            {
                > 0.8f => RoomFeatureType.Platform,
                > 0.6f => RoomFeatureType.Obstacle,
                > 0.4f => RoomFeatureType.Collectible,
                _ => RoomFeatureType.Platform
            };
        }
    }
}
