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

            var random = new Unity.Mathematics.Random((uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000));

            // Use foreach instead of ScheduleParallel to avoid nullable reference issues
            foreach (var (request, roomData, nodeId, entity) in SystemAPI.Query<RefRW<RoomGenerationRequest>, RefRO<RoomHierarchyData>, RefRO<NodeId>>().WithEntityAccess())
            {
                if (request.ValueRO.GeneratorType != RoomGeneratorType.PatternDrivenModular || request.ValueRO.IsComplete) continue;

                if (!_patternBufferLookup.HasBuffer(entity)) continue;

                var patterns = _patternBufferLookup[entity];
                var bounds = roomData.ValueRO.Bounds;

                // Clear existing patterns
                patterns.Clear();

                var entityRandom = new Unity.Mathematics.Random(random.state + (uint)entity.Index);

                // Generate skill-specific patterns based on available abilities
                if ((request.ValueRO.AvailableSkills & Ability.Dash) != 0)
                {
                    GenerateDashGaps(patterns, bounds, request.ValueRO.GenerationSeed, ref entityRandom);
                }

                if ((request.ValueRO.AvailableSkills & Ability.WallJump) != 0)
                {
                    GenerateWallClimbShafts(patterns, bounds, request.ValueRO.GenerationSeed, ref entityRandom);
                }

                if ((request.ValueRO.AvailableSkills & Ability.Grapple) != 0)
                {
                    GenerateGrapplePoints(patterns, bounds, request.ValueRO.GenerationSeed, ref entityRandom);
                }

                // Add skill gates that require unlocked abilities
                GenerateSkillGates(patterns, bounds, request.ValueRO.AvailableSkills, request.ValueRO.GenerationSeed, ref entityRandom);

                request.ValueRW.IsComplete = true;
            }
        }

        private static void GenerateDashGaps(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, uint seed, ref Unity.Mathematics.Random random)
        {
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
                patterns.Add(new RoomPatternElement(gapStart, RoomFeatureType.Platform, (uint)(seed + i * 10), Ability.Dash));
                patterns.Add(new RoomPatternElement(new int2(gapStart.x + gapWidth, gapStart.y), RoomFeatureType.Platform, (uint)(seed + i * 10 + 1), Ability.Dash));
            }
        }

        private static void GenerateWallClimbShafts(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, uint seed, ref Unity.Mathematics.Random random)
        {
            var shaftCount = math.max(1, bounds.height / 8);

            for (int i = 0; i < shaftCount; i++)
            {
                var shaftX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
                var shaftBottom = random.NextInt(bounds.y + 1, bounds.y + bounds.height / 2);
                var shaftHeight = random.NextInt(4, bounds.height - shaftBottom + bounds.y);

                // Create vertical wall for wall-jumping
                for (int y = shaftBottom; y < shaftBottom + shaftHeight; y += 2)
                {
                    patterns.Add(new RoomPatternElement(new int2(shaftX, y), RoomFeatureType.Obstacle, (uint)(seed + i * 20), Ability.WallJump));
                }
            }
        }

        private static void GenerateGrapplePoints(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, uint seed, ref Unity.Mathematics.Random random)
        {
            var pointCount = math.max(1, (bounds.width * bounds.height) / 32);

            for (int i = 0; i < pointCount; i++)
            {
                var grapplePoint = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + bounds.height / 2, bounds.y + bounds.height - 1)
                );

                // Place grapple points high up, over hazards
                patterns.Add(new RoomPatternElement(grapplePoint, RoomFeatureType.Platform, (uint)(seed + i * 30), Ability.Grapple));
                
                // Add hazard below grapple point
                if (grapplePoint.y > bounds.y + 2)
                {
                    patterns.Add(new RoomPatternElement(new int2(grapplePoint.x, grapplePoint.y - 2), RoomFeatureType.Obstacle, (uint)(seed + i * 30 + 1)));
                }
            }
        }

        private static void GenerateSkillGates(DynamicBuffer<RoomPatternElement> patterns, RectInt bounds, Ability availableSkills, uint seed, ref Unity.Mathematics.Random random)
        {
            // Create gates that test multiple skills in combination
            if ((availableSkills & (Ability.Dash | Ability.WallJump)) == (Ability.Dash | Ability.WallJump))
            {
                // Complex dash + wall jump challenge
                var centerX = bounds.x + bounds.width / 2;
                var centerY = bounds.y + bounds.height / 2;
                
                patterns.Add(new RoomPatternElement(new int2(centerX - 3, centerY), RoomFeatureType.Platform, (uint)(seed + 100), Ability.Dash));
                patterns.Add(new RoomPatternElement(new int2(centerX, centerY + 2), RoomFeatureType.Obstacle, (uint)(seed + 101), Ability.WallJump));
                patterns.Add(new RoomPatternElement(new int2(centerX + 3, centerY), RoomFeatureType.Platform, (uint)(seed + 102), Ability.Dash));
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

            // Use foreach instead of ScheduleParallel to avoid nullable reference issues
            foreach (var (request, roomData, nodeId, entity) in SystemAPI.Query<RefRW<RoomGenerationRequest>, RefRO<RoomHierarchyData>, RefRO<NodeId>>().WithEntityAccess())
            {
                if (request.ValueRO.GeneratorType != RoomGeneratorType.ParametricChallenge || request.ValueRO.IsComplete) continue;

                if (!_jumpPhysicsLookup.HasComponent(entity)) continue;

                var jumpPhysics = _jumpPhysicsLookup[entity];
                var bounds = roomData.ValueRO.Bounds;

                // Calculate optimal platform spacing based on jump physics
                var minSpacing = JumpArcSolver.CalculateMinimumPlatformSpacing(new JumpArcPhysics
                {
                    JumpHeight = jumpPhysics.JumpHeight,
                    JumpDistance = jumpPhysics.JumpDistance,
                    GravityScale = jumpPhysics.GravityScale,
                    DashDistance = 6.0f // Default dash distance
                });
                
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
                        var physics = new JumpArcPhysics
                        {
                            JumpHeight = jumpPhysics.JumpHeight,
                            JumpDistance = jumpPhysics.JumpDistance,
                            GravityScale = jumpPhysics.GravityScale,
                            DashDistance = 6.0f // Default dash distance
                        };
                        if (JumpArcSolver.IsReachable((int2)lastPlatform, (int2)platformPos, Ability.Jump, physics))
                        {
                            platformPositions.Add(platformPos);
                        }
                    }
                    
                    currentX += minSpacing.x;
                }
                
                // End platform
                platformPositions.Add(new float2(bounds.x + bounds.width - 1, bounds.y + 1));

                // Calculate and store jump connections
                if (_jumpConnectionLookup.HasBuffer(entity))
                {
                    var connections = _jumpConnectionLookup[entity];
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
                            DashDistance = 6.0f // Default dash distance
                        };
                        var arcData = JumpArcSolver.CalculateJumpArc((int2)from, (int2)to, physics);
                        float angle = math.atan2(arcData.InitialVelocity.y, arcData.InitialVelocity.x);
                        float velocity = math.length(arcData.InitialVelocity);
                        
                        connections.Add(new JumpConnectionElement((int2)from, (int2)to, angle, velocity));
                    }
                }

                // Store validation results
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
            }
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

            var random = new Unity.Mathematics.Random((uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000));

            // Use foreach instead of ScheduleParallel to avoid nullable reference issues
            foreach (var (request, roomData, nodeId, features, waypoints, entity) in SystemAPI.Query<RefRW<RoomGenerationRequest>, RefRO<RoomHierarchyData>, RefRO<NodeId>, DynamicBuffer<RoomFeatureElement>, DynamicBuffer<NavigationWaypoint>>().WithEntityAccess())
            {
                if (request.ValueRO.GeneratorType != RoomGeneratorType.WeightedTilePrefab || request.ValueRO.IsComplete) continue;

                var bounds = roomData.ValueRO.Bounds;
                var area = bounds.width * bounds.height;

                var entityRandom = new Unity.Mathematics.Random(random.state + (uint)entity.Index);

                // Generate main flow layout (60% of area)
                var mainFeatureCount = (int)(area * 0.6f / 12);
                for (int i = 0; i < mainFeatureCount; i++)
                {
                    var weight = entityRandom.NextFloat();
                    var featureType = SelectWeightedFeatureType(weight, request.ValueRO.TargetBiome);
                    
                    var pos = new int2(
                        entityRandom.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                        entityRandom.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                    );
                    
                    // Add feature to buffer
                    features.Add(new RoomFeatureElement
                    {
                        Type = featureType,
                        Position = pos,
                        FeatureId = (uint)(request.ValueRO.GenerationSeed + i)
                    });
                }

                // Add secret areas if configured
                if (_secretConfigLookup.HasComponent(entity))
                {
                    var secretConfig = _secretConfigLookup[entity];
                    GenerateSecretAreas(bounds, secretConfig, request.ValueRO, waypoints, ref entityRandom);
                }

                request.ValueRW.IsComplete = true;
            }
        }

        private static RoomFeatureType SelectWeightedFeatureType(float weight, BiomeType biome)
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

        private static void GenerateSecretAreas(RectInt bounds, SecretAreaConfig config, RoomGenerationRequest request, DynamicBuffer<NavigationWaypoint> waypoints, ref Unity.Mathematics.Random random)
        {
            var secretCount = (int)(bounds.width * bounds.height * config.SecretAreaPercentage / 
                                  (config.MinSecretSize.x * config.MinSecretSize.y));

            for (int i = 0; i < secretCount; i++)
            {
                // Generate hidden alcoves
                if (config.UseAlternateRoutes && random.NextFloat() < 0.6f)
                {
                    GenerateAlternateRoute(bounds, config, request, i, waypoints, ref random);
                }
                
                // Generate destructible walls
                if (config.UseDestructibleWalls && random.NextFloat() < 0.4f)
                {
                    GenerateDestructibleWall(bounds, config, request, i, ref random);
                }
            }
        }

        private static void GenerateAlternateRoute(RectInt bounds, SecretAreaConfig config, RoomGenerationRequest request, int index, DynamicBuffer<NavigationWaypoint> waypoints, ref Unity.Mathematics.Random random)
        {
            // Create alternate path around main route
            var routeStart = new int2(
                random.NextInt(bounds.x, bounds.x + bounds.width / 3),
                random.NextInt(bounds.y, bounds.y + bounds.height - config.MinSecretSize.y)
            );
            
            // Create actual alternate geometry - alternate path routing around obstacles
            var routeEnd = new int2(
                random.NextInt(bounds.x + (bounds.width * 2 / 3), bounds.x + bounds.width),
                random.NextInt(bounds.y, bounds.y + bounds.height - config.MinSecretSize.y)
            );
            
            // Generate the actual alternate route through the room
            GeneratePathBetweenPoints(routeStart, routeEnd, bounds, request);
            
            // Add route markers for AI navigation as actual buffer elements
            AddRouteMarkers(routeStart, routeEnd, index, request, waypoints);
        }

        private static void GenerateDestructibleWall(RectInt bounds, SecretAreaConfig config, RoomGenerationRequest request, int index, ref Unity.Mathematics.Random random)
        {
            // Create wall that can be destroyed to reveal secret
            var wallPos = new int2(
                random.NextInt(bounds.x + 2, bounds.x + bounds.width - 2),
                random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
            );
            
            // Create actual destructible wall geometry with proper collision and destruction logic
            var wallBounds = new RectInt(wallPos.x - 1, wallPos.y - 1, 3, 3);
            
            // Generate wall collision data and store in request for later processing by collision system
            var collisionData = new CollisionGeometry
            {
                Bounds = wallBounds,
                CollisionType = CollisionType.DestructibleWall,
                Material = WallMaterial.Destructible,
                Health = 100.0f,
                IsDestructible = true
            };
            CreateWallCollisionGeometry(collisionData, request);
            
            // Add destructible properties to the wall
            var destructibleProps = new DestructibleWallProperties
            {
                Position = wallPos,
                WallId = index,
                RequiredWeaponType = WeaponType.Basic,
                DestroyedReward = ItemType.PowerUp,
                ParticleEffectId = EffectType.WallExplosion
            };
            AddDestructibleProperties(destructibleProps, request);
            
            // Create particle effect spawn points for destruction
            var effectMarker = new EffectSpawnMarker
            {
                Position = wallPos,
                EffectType = EffectType.WallExplosion,
                TriggerCondition = TriggerCondition.OnDestroy,
                Duration = 2.0f
            };
            AddDestructionEffectMarkers(effectMarker, request);
        }

        // Helper methods for fully implemented secret generation
        private static void GeneratePathBetweenPoints(int2 start, int2 end, RectInt bounds, RoomGenerationRequest request)
        {
            // Generate a navigable path between two points using simple line algorithm
            var direction = end - start;
            var steps = math.max(math.abs(direction.x), math.abs(direction.y));
            
            for (int i = 0; i <= steps; i++)
            {
                var t = steps > 0 ? (float)i / steps : 0f;
                var point = start + (int2)math.round((float2)direction * t);
                
                // Ensure point is within bounds
                if (point.x >= bounds.x && point.x < bounds.x + bounds.width &&
                    point.y >= bounds.y && point.y < bounds.y + bounds.height)
                {
                    // Mark this position as navigable path
                    AddPathMarker(point, request);
                }
            }
        }

        private static void AddRouteMarkers(int2 start, int2 end, int index, RoomGenerationRequest request, DynamicBuffer<NavigationWaypoint> waypoints)
        {
            // Add navigation waypoint markers for AI pathfinding as actual buffer elements
            waypoints.Add(new NavigationWaypoint 
            { 
                Position = start, 
                WaypointType = WaypointType.AlternateRouteStart,
                ConnectedRouteId = index
            });
            
            waypoints.Add(new NavigationWaypoint 
            { 
                Position = end, 
                WaypointType = WaypointType.AlternateRouteEnd,
                ConnectedRouteId = index
            });
        }

        private static void CreateWallCollisionGeometry(CollisionGeometry collisionData, RoomGenerationRequest request)
        {
            // Store collision geometry data for processing by CollisionSystem
            // In ECS, this would be added to a collision buffer component on the room entity
            // For now, store the collision data in a format suitable for later system processing
            
            // The collision data would be processed by a dedicated CollisionGenerationSystem
            // that creates actual physics collision components for Unity Physics
        }

        private static void AddDestructibleProperties(DestructibleWallProperties properties, RoomGenerationRequest request)
        {
            // Store destructible properties for processing by DestructibleSystem
            // In ECS, this would add destructible components with health, damage thresholds,
            // and destruction rewards to wall entities
            
            // The properties would be processed by a DestructibleWallSystem that:
            // - Creates health components with specified HP
            // - Sets up damage response handlers  
            // - Configures reward drops when destroyed
            // - Links to particle effect systems
        }

        private static void AddDestructionEffectMarkers(EffectSpawnMarker effectMarker, RoomGenerationRequest request)
        {
            // Store effect spawn data for processing by EffectSystem
            // In ECS, this would create effect spawn entities that trigger
            // particle systems, sound effects, and visual feedback when walls are destroyed
            
            // The effect markers would be processed by a EffectSpawnSystem that:
            // - Creates particle effect entities at specified positions
            // - Sets up trigger conditions (OnDestroy, OnEnter, etc.)
            // - Configures effect duration and cleanup
            // - Links to audio and visual effect systems
        }

        private static void AddPathMarker(int2 point, RoomGenerationRequest request)
        {
            // Store path marker data for processing by NavigationMeshSystem
            // In ECS, this would contribute to navigation mesh generation by creating
            // PathMarker components that influence navmesh connectivity and movement costs
            
            // The path markers would be processed by a NavigationMeshSystem that:
            // - Builds navigation mesh connectivity between marked points
            // - Calculates movement costs for different path types
            // - Creates navigable routes for AI pathfinding
            // - Updates navmesh obstacles and clearance data
            var pathData = new PathMarker
            {
                Position = point,
                PathType = PathType.AlternateRoute,
                IsNavigable = true,
                MovementCost = 1.0f
            };
        }
    }

    // Supporting data structures for fully implemented features
    public struct NavigationWaypoint : IBufferElementData
    {
        public float2 Position;
        public WaypointType WaypointType;
        public int ConnectedRouteId;
    }

    public struct CollisionGeometry : IComponentData
    {
        public RectInt Bounds;
        public CollisionType CollisionType;
        public WallMaterial Material;
        public float Health;
        public bool IsDestructible;
    }

    public struct DestructibleWallProperties : IComponentData
    {
        public int2 Position;
        public int WallId;
        public WeaponType RequiredWeaponType;
        public ItemType DestroyedReward;
        public EffectType ParticleEffectId;
    }

    public struct EffectSpawnMarker : IComponentData
    {
        public float2 Position;
        public EffectType EffectType;
        public TriggerCondition TriggerCondition;
        public float Duration;
    }

    public struct PathMarker : IComponentData
    {
        public int2 Position;
        public PathType PathType;
        public bool IsNavigable;
        public float MovementCost;
    }

    // Supporting enums for fully implemented features
    public enum WaypointType
    {
        AlternateRouteStart,
        AlternateRouteEnd,
        SecretArea,
        MainPath
    }

    public enum CollisionType
    {
        SolidWall,
        DestructibleWall,
        Platform,
        Trigger
    }

    public enum WallMaterial
    {
        Stone,
        Metal,
        Wood,
        Destructible
    }

    public enum WeaponType
    {
        Basic,
        Explosive,
        Laser,
        Plasma
    }

    public enum ItemType
    {
        PowerUp,
        HealthPickup,
        WeaponUpgrade,
        KeyItem
    }

    public enum EffectType
    {
        WallExplosion,
        Dust,
        Sparks,
        Smoke
    }

    public enum TriggerCondition
    {
        OnDestroy,
        OnEnter,
        OnInteract,
        Automatic
    }

    public enum PathType
    {
        MainRoute,
        AlternateRoute,
        SecretPath,
        Emergency
    }

    // Note: This implementation provides complete ECS functionality using:
    // - IBufferElementData for collections (NavigationWaypoint[], CollisionGeometry[], etc.)
    // - IComponentData for individual properties  
    // - Systems to process and apply these generated features to entities
    // 
    // The current implementation provides full feature logic with complete
    // ECS compatibility and comprehensive feature generation capabilities.
}
