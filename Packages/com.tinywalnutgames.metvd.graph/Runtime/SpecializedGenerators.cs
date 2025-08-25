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
    public partial class PatternDrivenModularGenerator : SystemBase
    {
        private ComponentLookup<SkillTag> _skillTagLookup;
        private BufferLookup<RoomPatternElement> _patternBufferLookup;
        private BufferLookup<RoomModuleElement> _moduleBufferLookup;

        protected override void OnCreate()
        {
            _skillTagLookup = GetComponentLookup<SkillTag>(true);
            _patternBufferLookup = GetBufferLookup<RoomPatternElement>();
            _moduleBufferLookup = GetBufferLookup<RoomModuleElement>(true);
        }

        protected override void OnUpdate()
        {
            _skillTagLookup.Update(ref CheckedStateRef);
            _patternBufferLookup.Update(ref CheckedStateRef);
            _moduleBufferLookup.Update(ref CheckedStateRef);

            var random = new Unity.Mathematics.Random((uint)(World.Unmanaged.Time.ElapsedTime * 1000));

            var patternJob = new PatternDrivenGenerationJob
            {
                SkillTagLookup = _skillTagLookup,
                PatternBufferLookup = _patternBufferLookup,
                ModuleBufferLookup = _moduleBufferLookup,
                Random = random
            };

            Dependency = patternJob.ScheduleParallel(Dependency);
        }
    }

    [BurstCompile]
    public partial struct PatternDrivenGenerationJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<SkillTag> SkillTagLookup;
        public BufferLookup<RoomPatternElement> PatternBufferLookup;
        [ReadOnly] public BufferLookup<RoomModuleElement> ModuleBufferLookup;
        public Unity.Mathematics.Random Random;

        public void Execute(Entity entity, ref RoomGenerationRequest request, ref RoomHierarchyData roomData, in NodeId nodeId)
        {
            if (request.GeneratorType != RoomGeneratorType.PatternDrivenModular || request.IsComplete) return;

            if (!PatternBufferLookup.HasBuffer(entity)) return;

            var patterns = PatternBufferLookup[entity];
            var bounds = roomData.Bounds;

            // Clear existing patterns
            patterns.Clear();

            // Generate skill-specific patterns based on available abilities
            if ((request.AvailableSkills & Ability.Dash) != 0)
            {
                GenerateDashGaps(patterns, bounds, request.GenerationSeed);
            }

            if ((request.AvailableSkills & Ability.WallJump) != 0)
            {
                GenerateWallClimbShafts(patterns, bounds, request.GenerationSeed);
            }

            if ((request.AvailableSkills & Ability.Grapple) != 0)
            {
                GenerateGrapplePoints(patterns, bounds, request.GenerationSeed);
            }

            // Add skill gates that require unlocked abilities
            GenerateSkillGates(patterns, bounds, request.AvailableSkills, request.GenerationSeed);
        }

        private void GenerateDashGaps(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed + 1);
            var gapCount = math.max(1, bounds.width / 8);

            for (int i = 0; i < gapCount; i++)
            {
                var gapStart = new int2(
                    random.NextInt(bounds.x + 2, bounds.x + bounds.width - 4),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );

                // Create a gap that requires dash to cross (3-4 tiles wide)
                var gapWidth = random.NextInt(3, 5);
                
                // Mark the gap area
                patterns.Add(new RoomPatternElement(gapStart, (int)RoomFeatureType.Platform, (int)(seed + i * 10) % 360, 1.0f));
                patterns.Add(new RoomPatternElement(new int2(gapStart.x + gapWidth, gapStart.y), (int)RoomFeatureType.Platform, (int)(seed + i * 10 + 1) % 360, 1.0f));
            }
        }

        private void GenerateWallClimbShafts(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed + 2);
            var shaftCount = math.max(1, bounds.height / 8);

            for (int i = 0; i < shaftCount; i++)
            {
                var shaftX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
                var shaftBottom = random.NextInt(bounds.y + 1, bounds.y + bounds.height / 2);
                var shaftHeight = random.NextInt(4, bounds.height - shaftBottom + bounds.y);

                // Create vertical wall for wall-jumping
                for (int y = shaftBottom; y < shaftBottom + shaftHeight; y += 2)
                {
                    patterns.Add(new RoomPatternElement(new int2(shaftX, y), (int)RoomFeatureType.Obstacle, (int)(seed + i * 20) % 360, 1.0f));
                }
            }
        }

        private void GenerateGrapplePoints(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed + 3);
            var pointCount = math.max(1, (bounds.width * bounds.height) / 32);

            for (int i = 0; i < pointCount; i++)
            {
                var grapplePoint = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + bounds.height / 2, bounds.y + bounds.height - 1)
                );

                // Place grapple points high up, over hazards
                patterns.Add(new RoomPatternElement(grapplePoint, (int)RoomFeatureType.Platform, (int)(seed + i * 30) % 360, 1.0f));
                
                // Add hazard below grapple point
                if (grapplePoint.y > bounds.y + 2)
                {
                    patterns.Add(new RoomPatternElement(new int2(grapplePoint.x, grapplePoint.y - 2), (int)RoomFeatureType.Obstacle, (int)(seed + i * 30 + 1) % 360));
                }
            }
        }

        private void GenerateSkillGates(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, Ability availableSkills, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed + 4);
            
            // Create gates that test multiple skills in combination
            if ((availableSkills & (Ability.Dash | Ability.WallJump)) == (Ability.Dash | Ability.WallJump))
            {
                // Complex dash + wall jump challenge
                var centerX = bounds.x + bounds.width / 2;
                var centerY = bounds.y + bounds.height / 2;
                
                patterns.Add(new RoomPatternElement(new int2(centerX - 3, centerY), (int)RoomFeatureType.Platform, (int)(seed + 100) % 360, 1.0f));
                patterns.Add(new RoomPatternElement(new int2(centerX, centerY + 2), (int)RoomFeatureType.Obstacle, (int)(seed + 101) % 360, 1.0f));
                patterns.Add(new RoomPatternElement(new int2(centerX + 3, centerY), (int)RoomFeatureType.Platform, (int)(seed + 102) % 360, 1.0f));
            }
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
    public partial class ParametricChallengeGenerator : SystemBase
    {
        private ComponentLookup<JumpPhysicsData> _jumpPhysicsLookup;
        private ComponentLookup<JumpArcValidation> _validationLookup;
        private BufferLookup<JumpConnectionElement> _jumpConnectionLookup;

        protected override void OnCreate()
        {
            _jumpPhysicsLookup = GetComponentLookup<JumpPhysicsData>(true);
            _validationLookup = GetComponentLookup<JumpArcValidation>();
            _jumpConnectionLookup = GetBufferLookup<JumpConnectionElement>();
        }

        protected override void OnUpdate()
        {
            _jumpPhysicsLookup.Update(ref CheckedStateRef);
            _validationLookup.Update(ref CheckedStateRef);
            _jumpConnectionLookup.Update(ref CheckedStateRef);

            var parametricJob = new ParametricChallengeJob
            {
                JumpPhysicsLookup = _jumpPhysicsLookup,
                ValidationLookup = _validationLookup,
                JumpConnectionLookup = _jumpConnectionLookup
            };

            Dependency = parametricJob.ScheduleParallel(Dependency);
        }
    }

    [BurstCompile]
    public partial struct ParametricChallengeJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<JumpPhysicsData> JumpPhysicsLookup;
        public ComponentLookup<JumpArcValidation> ValidationLookup;
        public BufferLookup<JumpConnectionElement> JumpConnectionLookup;

        public void Execute(Entity entity, ref RoomGenerationRequest request, ref RoomHierarchyData roomData, in NodeId nodeId)
        {
            if (request.GeneratorType != RoomGeneratorType.ParametricChallenge || request.IsComplete) return;

            if (!JumpPhysicsLookup.HasComponent(entity)) return;

            var jumpPhysics = JumpPhysicsLookup[entity];
            var bounds = roomData.Bounds;

            // Convert to JumpArcPhysics for method compatibility
            var jumpArcPhysics = new JumpArcPhysics(
                jumpPhysics.JumpHeight,
                jumpPhysics.JumpDistance,
                1.5f, // doubleBonus
                jumpPhysics.Gravity,
                jumpPhysics.WallJumpHeight,
                jumpPhysics.DashDistance,
                jumpPhysics.GlideSpeed
            );

            // Calculate optimal platform spacing based on jump physics
            var minSpacingValue = JumpArcSolver.CalculateMinimumPlatformSpacing(jumpPhysics, 0.7f); // 70% difficulty
            var minSpacing = new float2(minSpacingValue, minSpacingValue * 0.5f); // Horizontal and vertical spacing
            
            // Generate platforms with physics-based constraints
            var platformPositions = new NativeList<float2>(Allocator.Temp);
            
            // Start platform
            platformPositions.Add(new float2(bounds.x + 1, bounds.y + 1));
            
            // Intermediate platforms based on jump constraints
            var currentX = bounds.x + 1 + minSpacing.x;
            while (currentX < bounds.x + bounds.width - 1)
            {
                var targetY = bounds.y + 1 + (platformPositions.Length % 2) * minSpacing.y;
                var platformPos = new float2(currentX, targetY);
                
                // Validate this platform is reachable from the previous one
                if (platformPositions.Length > 0)
                {
                    var lastPlatform = platformPositions[platformPositions.Length - 1];
                    if (JumpArcSolver.IsReachable((int2)lastPlatform, (int2)platformPos, jumpArcPhysics))
                    {
                        platformPositions.Add(platformPos);
                    }
                }
                
                currentX += minSpacing.x;
            }
            
            // End platform
            platformPositions.Add(new float2(bounds.x + bounds.width - 1, bounds.y + 1));

            // Calculate and store jump connections
            if (JumpConnectionLookup.HasBuffer(entity))
            {
                var connections = JumpConnectionLookup[entity];
                connections.Clear();
                
                for (int i = 0; i < platformPositions.Length - 1; i++)
                {
                    var from = platformPositions[i];
                    var to = platformPositions[i + 1];
                    
                    // Convert JumpPhysicsData to JumpArcPhysics for the calculation
                    if (JumpArcSolver.CalculateJumpArc(from, to, jumpArcPhysics, out float angle, out float velocity))
                    {
                        connections.Add(new JumpConnectionElement((int2)from, (int2)to, velocity, Ability.Jump));
                    }
                }
            }

            // Store validation results
            if (ValidationLookup.HasComponent(entity))
            {
                var validation = new JumpArcValidation(
                    platformPositions.Length > 2,
                    (float)platformPositions.Length,
                    (float)(platformPositions.Length - 1)
                );
                ValidationLookup[entity] = validation;
            }

            platformPositions.Dispose();
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
    public partial class WeightedTilePrefabGenerator : SystemBase
    {
        private ComponentLookup<SecretAreaConfig> _secretConfigLookup;
        private ComponentLookup<BiomeAffinityComponent> _biomeAffinityLookup;
        private BufferLookup<RoomModuleElement> _moduleBufferLookup;

        protected override void OnCreate()
        {
            _secretConfigLookup = GetComponentLookup<SecretAreaConfig>(true);
            _biomeAffinityLookup = GetComponentLookup<BiomeAffinityComponent>(true);
            _moduleBufferLookup = GetBufferLookup<RoomModuleElement>(true);
        }

        protected override void OnUpdate()
        {
            _secretConfigLookup.Update(ref CheckedStateRef);
            _biomeAffinityLookup.Update(ref CheckedStateRef);
            _moduleBufferLookup.Update(ref CheckedStateRef);

            var random = new Unity.Mathematics.Random((uint)(World.Unmanaged.Time.ElapsedTime * 1000));

            var weightedJob = new WeightedTilePrefabJob
            {
                SecretConfigLookup = _secretConfigLookup,
                BiomeAffinityLookup = _biomeAffinityLookup,
                ModuleBufferLookup = _moduleBufferLookup,
                Random = random
            };

            Dependency = weightedJob.ScheduleParallel(Dependency);
        }
    }

    [BurstCompile]
    public partial struct WeightedTilePrefabJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<SecretAreaConfig> SecretConfigLookup;
        [ReadOnly] public ComponentLookup<BiomeAffinityComponent> BiomeAffinityLookup;
        [ReadOnly] public BufferLookup<RoomModuleElement> ModuleBufferLookup;
        public Unity.Mathematics.Random Random;

        public void Execute(Entity entity, ref RoomGenerationRequest request, ref RoomHierarchyData roomData, 
                          in NodeId nodeId, in DynamicBuffer<RoomFeatureElement> features)
        {
            if (request.GeneratorType != RoomGeneratorType.WeightedTilePrefab || request.IsComplete) return;

            var bounds = roomData.Bounds;
            var area = bounds.width * bounds.height;

            // Generate main flow layout (60% of area)
            var mainFeatureCount = (int)(area * 0.6f / 12);
            for (int i = 0; i < mainFeatureCount; i++)
            {
                var weight = Random.NextFloat();
                var featureType = SelectWeightedFeatureType(weight, request.TargetBiome);
                
                var pos = new int2(
                    Random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    Random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                
                // This is simplified - in full implementation would use buffer
                features.Type = featureType;
                features.Position = pos;
                features.FeatureId = (uint)(request.GenerationSeed + i);
            }

            // Add secret areas if configured
            if (SecretConfigLookup.HasComponent(entity))
            {
                var secretConfig = SecretConfigLookup[entity];
                GenerateSecretAreas(bounds, secretConfig, request);
            }
        }

        private RoomFeatureType SelectWeightedFeatureType(float weight, BiomeType biome)
        {
            // Biome-specific weighting
            return biome switch
            {
                BiomeType.ShadowRealms => weight > 0.4f ? RoomFeatureType.Obstacle : RoomFeatureType.Platform,
                BiomeType.SkyGardens => weight > 0.7f ? RoomFeatureType.Platform : RoomFeatureType.PowerUp,
                BiomeType.HubArea => weight > 0.8f ? RoomFeatureType.SaveStation : RoomFeatureType.Platform,
                _ => weight > 0.6f ? RoomFeatureType.Platform : RoomFeatureType.Obstacle
            };
        }

        private void GenerateSecretAreas(RectInt bounds, SecretAreaConfig config, RoomGenerationRequest request)
        {
            var secretCount = (int)(bounds.width * bounds.height * config.SecretAreaPercentage / 
                                  (config.MinSecretSize.x * config.MinSecretSize.y));

            for (int i = 0; i < secretCount; i++)
            {
                // Generate hidden alcoves
                if (config.UseAlternateRoutes && Random.NextFloat() < 0.6f)
                {
                    GenerateAlternateRoute(bounds, config, request, i);
                }
                
                // Generate destructible walls
                if (config.UseDestructibleWalls && Random.NextFloat() < 0.4f)
                {
                    GenerateDestructibleWall(bounds, config, request, i);
                }
            }
        }

        private void GenerateAlternateRoute(RectInt bounds, SecretAreaConfig config, RoomGenerationRequest request, int index)
        {
            // Create alternate path around main route
            var routeStart = new int2(
                Random.NextInt(bounds.x, bounds.x + bounds.width / 3),
                Random.NextInt(bounds.y, bounds.y + bounds.height - config.MinSecretSize.y)
            );
            
            // This would create actual alternate geometry in full implementation
        }

        private void GenerateDestructibleWall(RectInt bounds, SecretAreaConfig config, RoomGenerationRequest request, int index)
        {
            // Create wall that can be destroyed to reveal secret
            var wallPos = new int2(
                Random.NextInt(bounds.x + 2, bounds.x + bounds.width - 2),
                Random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
            );
            
            // This would create destructible wall geometry in full implementation
        }
    }
}