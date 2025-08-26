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

    #region PatternDrivenModularGenerator
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

            var random = new Unity.Mathematics.Random((uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000));

            var patternJob = new PatternDrivenGenerationJob
            {
                SkillTagLookup = _skillTagLookup,
                PatternBufferLookup = _patternBufferLookup,
                ModuleBufferLookup = _moduleBufferLookup,
                Random = random
            };

            state.Dependency = patternJob.ScheduleParallel(state.Dependency);
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

            patterns.Clear();

            if ((request.AvailableSkills & Ability.Dash) != 0)
                GenerateDashGaps(patterns, bounds, request.GenerationSeed);

            if ((request.AvailableSkills & Ability.WallJump) != 0)
                GenerateWallClimbShafts(patterns, bounds, request.GenerationSeed);

            if ((request.AvailableSkills & Ability.Grapple) != 0)
                GenerateGrapplePoints(patterns, bounds, request.GenerationSeed);

            GenerateSkillGates(patterns, bounds, request.AvailableSkills, request.GenerationSeed);
        }

        private readonly void GenerateDashGaps(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed + 1);
            var gapCount = math.max(1, bounds.width / 8);

            for (int i = 0; i < gapCount; i++)
            {
                var gapStart = new int2(
                    random.NextInt(bounds.x + 2, bounds.x + bounds.width - 4),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );

                var gapWidth = random.NextInt(3, 5);

                patterns.Add(new RoomPatternElement(gapStart, RoomFeatureType.Platform, (uint)(seed + i * 10), Ability.Dash));
                patterns.Add(new RoomPatternElement(new int2(gapStart.x + gapWidth, gapStart.y), RoomFeatureType.Platform, (uint)(seed + i * 10 + 1), Ability.Dash));
            }
        }

        private readonly void GenerateWallClimbShafts(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed + 2);
            var shaftCount = math.max(1, bounds.height / 8);

            for (int i = 0; i < shaftCount; i++)
            {
                var shaftX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
                var shaftBottom = random.NextInt(bounds.y + 1, bounds.y + bounds.height / 2);
                var shaftHeight = random.NextInt(4, bounds.height - shaftBottom + bounds.y);

                for (int y = shaftBottom; y < shaftBottom + shaftHeight; y += 2)
                {
                    patterns.Add(new RoomPatternElement(new int2(shaftX, y), RoomFeatureType.Obstacle, (uint)(seed + i * 20), Ability.WallJump));
                }
            }
        }

        private readonly void GenerateGrapplePoints(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed + 3);
            var pointCount = math.max(1, (bounds.width * bounds.height) / 32);

            for (int i = 0; i < pointCount; i++)
            {
                var grapplePoint = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + bounds.height / 2, bounds.y + bounds.height - 1)
                );

                patterns.Add(new RoomPatternElement(grapplePoint, RoomFeatureType.Platform, (uint)(seed + i * 30), Ability.Grapple));

                if (grapplePoint.y > bounds.y + 2)
                {
                    patterns.Add(new RoomPatternElement(new int2(grapplePoint.x, grapplePoint.y - 2), RoomFeatureType.Obstacle, (uint)(seed + i * 30 + 1)));
                }
            }
        }

        private readonly void GenerateSkillGates(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, Ability availableSkills, uint seed)
        {
            if ((availableSkills & (Ability.Dash | Ability.WallJump)) == (Ability.Dash | Ability.WallJump))
            {
                var centerX = bounds.x + bounds.width / 2;
                var centerY = bounds.y + bounds.height / 2;

                patterns.Add(new RoomPatternElement(new int2(centerX - 3, centerY), RoomFeatureType.Platform, (uint)(seed + 100), Ability.Dash));
                patterns.Add(new RoomPatternElement(new int2(centerX, centerY + 2), RoomFeatureType.Obstacle, (uint)(seed + 101), Ability.WallJump));
                patterns.Add(new RoomPatternElement(new int2(centerX + 3, centerY), RoomFeatureType.Platform, (uint)(seed + 102), Ability.Dash));
            }
        }
    }

#endregion

    #region ParametricChallengeGenerator
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

            var parametricJob = new ParametricChallengeJob
            {
                JumpPhysicsLookup = _jumpPhysicsLookup,
                ValidationLookup = _validationLookup,
                JumpConnectionLookup = _jumpConnectionLookup
            };

            state.Dependency = parametricJob.ScheduleParallel(state.Dependency);
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

            var minSpacing = JumpArcSolver.CalculateMinimumPlatformSpacing(new JumpArcPhysics
            {
                JumpHeight = jumpPhysics.JumpHeight,
                JumpDistance = jumpPhysics.JumpDistance,
                GravityScale = jumpPhysics.GravityScale,
                DashDistance = 6.0f
            });

            var platformPositions = new NativeList<float2>(Allocator.Temp)
            {
                new(bounds.x + 1, bounds.y + 1)
            };

            var currentX = bounds.x + 1 + minSpacing.x;
            while (currentX < bounds.x + bounds.width - 1)
            {
                var targetY = bounds.y + 1 + (platformPositions.Length % 2) * minSpacing.y;
                var platformPos = new float2(currentX, targetY);

                if (platformPositions.Length > 0)
                {
                    var lastPlatform = platformPositions[^1];
                    var physics = new JumpArcPhysics
                    {
                        JumpHeight = jumpPhysics.JumpHeight,
                        JumpDistance = jumpPhysics.JumpDistance,
                        GravityScale = jumpPhysics.GravityScale,
                        DashDistance = 6.0f
                    };
                    if (JumpArcSolver.IsReachable((int2)lastPlatform, (int2)platformPos, Ability.Jump, physics))
                        platformPositions.Add(platformPos);
                }

                currentX += minSpacing.x;
            }

            platformPositions.Add(new float2(bounds.x + bounds.width - 1, bounds.y + 1));

            if (JumpConnectionLookup.HasBuffer(entity))
            {
                var connections = JumpConnectionLookup[entity];
                connections.Clear();

                for (int i = 0; i < platformPositions.Length - 1; i++)
                {
                    var from = platformPositions[i];
                    var to = platformPositions[i + 1];

                    var physics = new JumpArcPhysics
                    {
                        JumpHeight = jumpPhysics.JumpHeight,
                        JumpDistance = jumpPhysics.JumpDistance,
                        GravityScale = jumpPhysics.GravityScale,
                        DashDistance = 6.0f
                    };
                    var arcData = JumpArcSolver.CalculateJumpArc((int2)from, (int2)to, physics);
                    float angle = math.atan2(arcData.InitialVelocity.y, arcData.InitialVelocity.x);
                    float velocity = math.length(arcData.InitialVelocity);

                    connections.Add(new JumpConnectionElement((int2)from, (int2)to, angle, velocity));
                }
            }

            if (ValidationLookup.HasComponent(entity))
            {
                var validation = new JumpArcValidation(
                    platformPositions.Length > 2,
                    jumpPhysics.JumpDistance,
                    jumpPhysics.JumpHeight
                );
                ValidationLookup[entity] = validation;
            }

            platformPositions.Dispose();
        }
    }

#endregion

    #region WeightedTilePrefabGenerator
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

            var random = new Unity.Mathematics.Random((uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000));

            var weightedJob = new WeightedTilePrefabJob
            {
                SecretConfigLookup = _secretConfigLookup,
                BiomeAffinityLookup = _biomeAffinityLookup,
                ModuleBufferLookup = _moduleBufferLookup,
                Random = random
            };

            state.Dependency = weightedJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct WeightedTilePrefabJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<SecretAreaConfig> SecretConfigLookup;
        [ReadOnly] public ComponentLookup<BiomeAffinityComponent> BiomeAffinityLookup;
        [ReadOnly] public BufferLookup<RoomModuleElement> ModuleBufferLookup;
        public Unity.Mathematics.Random Random;

        /*
         PSEUDOCODE (Implementation Plan for Secret Generation)
         1. Execute: generate main features then call GenerateSecretAreas if config exists.
         2. GenerateSecretAreas:
            - Derive min/ max secret size with safe defaults.
            - Compute secretCount based on percentage or probability (fallback).
            - Iterate secretCount:
                a. If alternate routes enabled & probability hit => GenerateAlternateRoute.
                b. If destructible walls enabled & probability hit => GenerateDestructibleWall.
         3. GenerateAlternateRoute:
            - Pick start (left third) and end (right third) Y within bounds.
            - Determine path length (#segments between 3..6).
            - Interpolate positions; add Secret features (or Platform flagged logically as secret type).
            - Insert a "connector gap" to hint optional path (Obstacle or Platform sequence).
         4. GenerateDestructibleWall:
            - Choose interior position not near edges.
            - Place an Obstacle feature representing a destructible tile.
            - Place a Secret feature one tile behind to represent reward space.
            - Tag via FeatureId offset for deterministic grouping (seed + index * stride).
         5. All randomization deterministic via request.GenerationSeed + offsets.
        */

        public void Execute(Entity entity, ref RoomGenerationRequest request, ref RoomHierarchyData roomData,
                            in NodeId nodeId, DynamicBuffer<RoomFeatureElement> features)
        {
            if (request.GeneratorType != RoomGeneratorType.WeightedTilePrefab || request.IsComplete) return;

            var bounds = roomData.Bounds;
            var area = bounds.width * bounds.height;

            var mainFeatureCount = (int)(area * 0.6f / 12);
            for (int i = 0; i < mainFeatureCount; i++)
            {
                var weight = Random.NextFloat();
                var featureType = SelectWeightedFeatureType(weight, request.TargetBiome);

                var pos = new int2(
                    Random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    Random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );

                features.Add(new RoomFeatureElement
                {
                    Type = featureType,
                    Position = pos,
                    FeatureId = (uint)(request.GenerationSeed + i)
                });
            }

            if (SecretConfigLookup.HasComponent(entity))
            {
                var secretConfig = SecretConfigLookup[entity];
                GenerateSecretAreas(bounds, secretConfig, request, features);
            }
        }

        private readonly RoomFeatureType SelectWeightedFeatureType(float weight, BiomeType biome)
        {
            return biome switch
            {
                BiomeType.ShadowRealms => weight > 0.4f ? RoomFeatureType.Obstacle : RoomFeatureType.Platform,
                BiomeType.SkyGardens => weight > 0.7f ? RoomFeatureType.Platform : RoomFeatureType.PowerUp,
                BiomeType.HubArea => weight > 0.8f ? RoomFeatureType.SaveStation : RoomFeatureType.Platform,
                _ => weight > 0.6f ? RoomFeatureType.Platform : RoomFeatureType.Obstacle
            };
        }

        private void GenerateSecretAreas(RectInt bounds, SecretAreaConfig config, RoomGenerationRequest request, DynamicBuffer<RoomFeatureElement> features)
        {
            // Fallback safe min size if config provides zero (defensive)
            int2 minSize = config.MinSecretSize.x <= 0 || config.MinSecretSize.y <= 0 ? new int2(3, 2) : config.MinSecretSize;
            int2 maxSize = config.MaxSecretSize.x <= 0 || config.MaxSecretSize.y <= 0 ? new int2(math.max(5, minSize.x + 1), math.max(4, minSize.y + 1)) : config.MaxSecretSize;

            // Basic proportional secret count; clamp to avoid explosion on large rooms
            int capacityPerSecret = math.max(1, minSize.x * minSize.y);
            int maxPossible = (bounds.width * bounds.height) / (capacityPerSecret * 12);
            int desired = math.min(config.MaxSecretsPerRoom > 0 ? config.MaxSecretsPerRoom : 2, maxPossible);
            int secretCount = desired;

            var baseSeed = request.GenerationSeed ^ 0xA1B2u;

            for (int i = 0; i < secretCount; i++)
            {
                var localRand = new Unity.Mathematics.Random(baseSeed + (uint)i * 17u);

                if (config.UseAlternateRoutes && localRand.NextFloat() < 0.6f)
                    GenerateAlternateRoute(bounds, config, request, i, features, localRand);

                if (config.UseDestructibleWalls && localRand.NextFloat() < 0.4f)
                    GenerateDestructibleWall(bounds, config, request, i, features, localRand);
            }
        }

        private void GenerateAlternateRoute(RectInt bounds, SecretAreaConfig config, RoomGenerationRequest request, int index,
                                            DynamicBuffer<RoomFeatureElement> features, Unity.Mathematics.Random rand)
        {
            if (bounds.width < 8) return;

            int segmentCount = rand.NextInt(3, 7); // 3..6 segments
            int startX = rand.NextInt(bounds.x + 1, bounds.x + bounds.width / 3);
            int endX = rand.NextInt(bounds.x + bounds.width * 2 / 3, bounds.x + bounds.width - 1);

            int startY = rand.NextInt(bounds.y + 1, bounds.y + bounds.height - 2);
            int endY = math.clamp(startY + rand.NextInt(-2, 3), bounds.y + 1, bounds.y + bounds.height - 2);

            float dx = (endX - startX) / (float)segmentCount;
            float dy = (endY - startY) / (float)segmentCount;

            uint baseId = (uint)(request.GenerationSeed + 0x2000 + index * 97);

            // Path platforms (Secret route)
            for (int s = 0; s <= segmentCount; s++)
            {
                int2 pos = new(
                    math.clamp((int)math.round(startX + dx * s), bounds.x + 1, bounds.x + bounds.width - 2),
                    math.clamp((int)math.round(startY + dy * s), bounds.y + 1, bounds.y + bounds.height - 2)
                );

                features.Add(new RoomFeatureElement
                {
                    Type = RoomFeatureType.Secret,
                    Position = pos,
                    FeatureId = baseId + (uint)s
                });
            }

            // Add entrance hint (an obstacle to break or a platform)
            int2 entrance = new(startX - 1, startY);
            if (entrance.x > bounds.x + 1)
            {
                features.Add(new RoomFeatureElement
                {
                    Type = RoomFeatureType.Obstacle,
                    Position = entrance,
                    FeatureId = baseId + 0xEE
                });
            }
        }

        private void GenerateDestructibleWall(RectInt bounds, SecretAreaConfig config, RoomGenerationRequest request, int index,
                                              DynamicBuffer<RoomFeatureElement> features, Unity.Mathematics.Random rand)
        {
            // Choose interior wall candidate
            int2 wallPos = new(
                rand.NextInt(bounds.x + 2, bounds.x + bounds.width - 2),
                rand.NextInt(bounds.y + 2, bounds.y + bounds.height - 2)
            );

            uint baseId = (uint)(request.GenerationSeed + 0x4000 + index * 131);

            // Wall segment (Obstacle)
            features.Add(new RoomFeatureElement
            {
                Type = RoomFeatureType.Obstacle,
                Position = wallPos,
                FeatureId = baseId
            });

            // Behind-wall secret reward (could be power-up or secret tile)
            int2 rewardPos = new(wallPos.x + (rand.NextBool() ? 1 : -1), wallPos.y);
            if (rewardPos.x > bounds.x + 1 && rewardPos.x < bounds.x + bounds.width - 1)
            {
                features.Add(new RoomFeatureElement
                {
                    Type = RoomFeatureType.Secret,
                    Position = rewardPos,
                    FeatureId = baseId + 1
                });
            }

            // Optional vertical hint tile above
            if (rand.NextFloat() < 0.3f)
            {
                int2 hintPos = new(wallPos.x, wallPos.y + 1);
                if (hintPos.y < bounds.y + bounds.height - 1)
                {
                    features.Add(new RoomFeatureElement
                    {
                        Type = RoomFeatureType.Obstacle,
                        Position = hintPos,
                        FeatureId = baseId + 2
                    });
                }
            }
        }

        private static RoomFeatureType ConvertToObjectType(RoomFeatureType featureType)
        {
            return featureType switch
            {
                RoomFeatureType.Platform => RoomFeatureType.Platform,
                RoomFeatureType.Obstacle => RoomFeatureType.Obstacle,
                RoomFeatureType.Secret => RoomFeatureType.Secret,
                RoomFeatureType.PowerUp => RoomFeatureType.PowerUp,
                RoomFeatureType.HealthPickup => RoomFeatureType.HealthPickup,
                RoomFeatureType.SaveStation => RoomFeatureType.SaveStation,
                RoomFeatureType.Switch => RoomFeatureType.Switch,
                _ => RoomFeatureType.Platform
            };
        }
    }
#endregion
}
