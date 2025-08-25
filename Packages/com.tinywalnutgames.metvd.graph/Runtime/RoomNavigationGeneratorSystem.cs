using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Navigation generation system implementing the Master Spec's post-content nav generation:
    /// - Empty-Above-Traversable Rule: If empty tile is directly above walkable/climbable tile, mark navigable
    /// - Jump Vector Calculation: For each traversable tile, calculate reachable empty tiles with movement tags
    /// 
    /// Runs AFTER room content generation but BEFORE AI pathfinding systems
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ProceduralRoomGeneratorSystem))]
    public partial class RoomNavigationGeneratorSystem : SystemBase
    {
        private EntityQuery _roomsWithContentQuery;
        
        protected override void OnCreate()
        {
            // Rooms that have content generated but no navigation generated
            _roomsWithContentQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NodeId, RoomHierarchyData, RoomTemplate, ProceduralRoomGenerated>()
                .WithAll<RoomNavigationElement>() // Has navigation buffer
                .Build(this);
                
            RequireForUpdate(_roomsWithContentQuery);
        }

        protected override void OnUpdate()
        {
            using var roomEntities = _roomsWithContentQuery.ToEntityArray(Allocator.Temp);
            using var nodeIds = _roomsWithContentQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            using var roomData = _roomsWithContentQuery.ToComponentDataArray<RoomHierarchyData>(Allocator.Temp);
            using var templates = _roomsWithContentQuery.ToComponentDataArray<RoomTemplate>(Allocator.Temp);
            
            for (int i = 0; i < roomEntities.Length; i++)
            {
                var roomEntity = roomEntities[i];
                var nodeId = nodeIds[i];
                var hierarchy = roomData[i];
                var template = templates[i];
                
                // Check if navigation already generated
                var genStatus = EntityManager.GetComponentData<ProceduralRoomGenerated>(roomEntity);
                if (genStatus.NavigationGenerated) continue;
                
                // Generate navigation for this room
                GenerateRoomNavigation(EntityManager, roomEntity, hierarchy, template, nodeId, ref genStatus);
                
                // Update generation status
                genStatus.NavigationGenerated = true;
                EntityManager.SetComponentData(roomEntity, genStatus);
            }
        }

        [BurstCompile]
        private static void GenerateRoomNavigation(EntityManager entityManager, Entity roomEntity, 
                                                  RoomHierarchyData hierarchy, RoomTemplate template, 
                                                  NodeId nodeId, ref ProceduralRoomGenerated genStatus)
        {
            var navBuffer = entityManager.GetBuffer<RoomNavigationElement>(roomEntity);
            navBuffer.Clear();
            
            var bounds = hierarchy.Bounds;
            var random = new Unity.Mathematics.Random(genStatus.GenerationSeed);
            
            // Generate basic physics parameters for this room type
            var physics = GeneratePhysicsForRoom(template, ref random);
            
            // Simulate a simple tilemap for demonstration - in real implementation this would
            // read from actual tilemap data or content generation results
            var tilemap = SimulateRoomTilemap(bounds, template, ref random);
            
            // Phase 1: Apply Empty-Above-Traversable Rule
            ApplyEmptyAboveTraversableRule(bounds, tilemap, navBuffer);
            
            // Phase 2: Calculate Jump Vector Connections
            CalculateJumpVectorConnections(bounds, tilemap, physics, template.CapabilityTags.RequiredSkills, navBuffer);
            
            // Phase 3: Add Secret/Alternate Route Connections
            if (template.SecretAreaPercentage > 0)
            {
                AddSecretRouteConnections(bounds, tilemap, physics, template.CapabilityTags.OptionalSkills, navBuffer, ref random);
            }
            
            tilemap.Dispose();
        }

        [BurstCompile]
        private static JumpArcPhysics GeneratePhysicsForRoom(RoomTemplate template, ref Unity.Mathematics.Random random)
        {
            // Base physics - could be read from configuration or determined by room type
            var physics = new JumpArcPhysics();
            
            // Adjust physics based on room generator type
            switch (template.GeneratorType)
            {
                case RoomGeneratorType.ParametricChallenge:
                    // Testing rooms have more precise physics
                    physics.JumpHeight = 2.5f + random.NextFloat(-0.5f, 0.5f);
                    physics.JumpDistance = 3.5f + random.NextFloat(-0.5f, 0.5f);
                    break;
                    
                case RoomGeneratorType.SkyBiomePlatform:
                    // Sky biome allows for longer jumps
                    physics.JumpHeight = 4.0f;
                    physics.JumpDistance = 5.0f;
                    physics.GlideSpeed = 8.0f; // Enhanced glide capability
                    break;
                    
                case RoomGeneratorType.VerticalSegment:
                    // Vertical rooms emphasize climbing and wall jumps
                    physics.WallJumpHeight = 3.0f;
                    physics.JumpHeight = 2.0f;
                    break;
                    
                default:
                    // Standard physics remain unchanged
                    break;
            }
            
            return physics;
        }

        [BurstCompile]
        private static NativeArray<TileType> SimulateRoomTilemap(RectInt bounds, RoomTemplate template, ref Unity.Mathematics.Random random)
        {
            // Simulate a tilemap for navigation generation
            // In real implementation, this would read from actual tilemap or content generation results
            int tileCount = bounds.width * bounds.height;
            var tilemap = new NativeArray<TileType>(tileCount, Allocator.Temp);

            // Get configurable parameters for this room type
            var config = GetTilemapGenerationConfig(template);

            // Fill with basic layout based on config
            for (int y = 0; y < bounds.height; y++)
            {
                for (int x = 0; x < bounds.width; x++)
                {
                    int index = y * bounds.width + x;

                    // Ground level and walls
                    if ((config.HasGroundLevel && y == 0) ||
                        (config.HasWalls && (x == 0 || x == bounds.width - 1)))
                        tilemap[index] = TileType.Solid;
                    else if (config.HasGroundLevel && y == 1)
                        tilemap[index] = TileType.Platform; // Ground level platforms
                    else if (random.NextFloat() < config.PlatformProbability) // Random platforms
                        tilemap[index] = TileType.Platform;
                    else if (random.NextFloat() < config.ClimbableProbability) // Climbable surfaces
                        tilemap[index] = TileType.Climbable;
                    else
                        tilemap[index] = TileType.Empty;
                }
            }
            return tilemap;
        }

        [BurstCompile]
        private static void ApplyEmptyAboveTraversableRule(RectInt bounds, NativeArray<TileType> tilemap, 
                                                          DynamicBuffer<RoomNavigationElement> navBuffer)
        {
            // Rule: If an empty tile is directly above a walkable/climbable/stickable tile, mark it navigable
            for (int y = 1; y < bounds.height; y++) // Start from second row
            {
                for (int x = 0; x < bounds.width; x++)
                {
                    int currentIndex = y * bounds.width + x;
                    int belowIndex = (y - 1) * bounds.width + x;
                    
                    var currentTile = tilemap[currentIndex];
                    var belowTile = tilemap[belowIndex];
                    
                    // If current is empty and below is traversable
                    if (currentTile == TileType.Empty && IsTraversable(belowTile))
                    {
                        var fromPos = new int2(x, y - 1); // Standing position
                        var toPos = new int2(x, y);       // Air position above
                        
                        // Add navigation connection with basic jump
                        navBuffer.Add(new RoomNavigationElement(fromPos, toPos, Ability.Jump, 1.0f, false));
                        
                        // Also add reverse connection (falling down)
                        navBuffer.Add(new RoomNavigationElement(toPos, fromPos, Ability.None, 0.5f, false));
                    }
                }
            }
        }

        [BurstCompile]
        private static void CalculateJumpVectorConnections(RectInt bounds, NativeArray<TileType> tilemap, 
                                                          JumpArcPhysics physics, Ability requiredSkills,
                                                          DynamicBuffer<RoomNavigationElement> navBuffer)
        {
            // For each traversable tile, calculate reachable empty tiles based on agent capabilities
            for (int y = 0; y < bounds.height; y++)
            {
                for (int x = 0; x < bounds.width; x++)
                {
                    int index = y * bounds.width + x;
                    var currentTile = tilemap[index];
                    
                    if (!IsTraversable(currentTile)) continue;
                    
                    var fromPos = new int2(x, y);
                    
                    // Check reachable positions within jump/movement range
                    AddJumpConnections(fromPos, bounds, tilemap, physics, requiredSkills, navBuffer);
                    AddDashConnections(fromPos, bounds, tilemap, physics, requiredSkills, navBuffer);
                    AddWallJumpConnections(fromPos, bounds, tilemap, physics, requiredSkills, navBuffer);
                }
            }
        }

        [BurstCompile]
        private static void AddJumpConnections(int2 fromPos, RectInt bounds, NativeArray<TileType> tilemap,
                                             JumpArcPhysics physics, Ability requiredSkills,
                                             DynamicBuffer<RoomNavigationElement> navBuffer)
        {
            if ((requiredSkills & Ability.Jump) == 0) return;
            
            int jumpRange = (int)physics.JumpDistance;
            int jumpHeight = (int)physics.JumpHeight;
            
            for (int dx = -jumpRange; dx <= jumpRange; dx++)
            {
                for (int dy = -1; dy <= jumpHeight; dy++) // Can jump up or fall down
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    var toPos = fromPos + new int2(dx, dy);
                    
                    if (!IsWithinBounds(toPos, bounds)) continue;
                    
                    int toIndex = toPos.y * bounds.width + toPos.x;
                    var toTile = tilemap[toIndex];
                    
                    // Can land on traversable tiles or move through empty space
                    if (IsTraversable(toTile) || toTile == TileType.Empty)
                    {
                        // Determine required movement type
                        Ability movement = Ability.Jump;
                        float cost = math.length(new float2(dx, dy));
                        
                        // Double jump for longer/higher jumps
                        if (math.abs(dx) > physics.JumpDistance * 0.7f || dy > physics.JumpHeight * 0.7f)
                        {
                            movement |= Ability.DoubleJump;
                            cost *= 1.5f;
                        }
                        
                        navBuffer.Add(new RoomNavigationElement(fromPos, toPos, movement, cost, false));
                    }
                }
            }
        }

        [BurstCompile]
        private static void AddDashConnections(int2 fromPos, RectInt bounds, NativeArray<TileType> tilemap,
                                             JumpArcPhysics physics, Ability requiredSkills,
                                             DynamicBuffer<RoomNavigationElement> navBuffer)
        {
            if ((requiredSkills & Ability.Dash) == 0) return;
            
            int dashRange = (int)physics.DashDistance;
            
            // Horizontal dashes
            for (int dx = -dashRange; dx <= dashRange; dx++)
            {
                if (dx == 0) continue;
                
                var toPos = fromPos + new int2(dx, 0);
                
                if (!IsWithinBounds(toPos, bounds)) continue;
                
                int toIndex = toPos.y * bounds.width + toPos.x;
                var toTile = tilemap[toIndex];
                
                if (IsTraversable(toTile) || toTile == TileType.Empty)
                {
                    navBuffer.Add(new RoomNavigationElement(fromPos, toPos, Ability.Dash, math.abs(dx), false));
                }
            }
        }

        [BurstCompile]
        private static void AddWallJumpConnections(int2 fromPos, RectInt bounds, NativeArray<TileType> tilemap,
                                                 JumpArcPhysics physics, Ability requiredSkills,
                                                 DynamicBuffer<RoomNavigationElement> navBuffer)
        {
            if ((requiredSkills & Ability.WallJump) == 0) return;
            
            // Check for walls adjacent to current position
            var leftWall = fromPos + new int2(-1, 0);
            var rightWall = fromPos + new int2(1, 0);
            
            bool hasLeftWall = IsWall(leftWall, bounds, tilemap);
            bool hasRightWall = IsWall(rightWall, bounds, tilemap);
            
            if (!hasLeftWall && !hasRightWall) return;
            
            int wallJumpHeight = (int)physics.WallJumpHeight;
            
            // Wall jump upward
            for (int dy = 1; dy <= wallJumpHeight; dy++)
            {
                var toPos = fromPos + new int2(0, dy);
                
                if (!IsWithinBounds(toPos, bounds)) continue;
                
                int toIndex = toPos.y * bounds.width + toPos.x;
                var toTile = tilemap[toIndex];
                
                if (IsTraversable(toTile) || toTile == TileType.Empty)
                {
                    navBuffer.Add(new RoomNavigationElement(fromPos, toPos, Ability.WallJump, dy, false));
                }
            }
        }

        [BurstCompile]
        private static void AddSecretRouteConnections(RectInt bounds, NativeArray<TileType> tilemap,
                                                     JumpArcPhysics physics, Ability optionalSkills,
                                                     DynamicBuffer<RoomNavigationElement> navBuffer,
                                                     ref Unity.Mathematics.Random random)
        {
            // Add connections that require optional skills for secret areas
            int secretCount = (int)(bounds.width * bounds.height * 0.05f); // 5% chance for secret connections
            
            for (int i = 0; i < secretCount; i++)
            {
                var fromPos = new int2(random.NextInt(0, bounds.width), random.NextInt(0, bounds.height));
                var toPos = new int2(random.NextInt(0, bounds.width), random.NextInt(0, bounds.height));
                
                if (math.all(fromPos == toPos)) continue;
                
                // Use advanced movement for secret routes
                Ability secretMovement = optionalSkills != Ability.None ? optionalSkills : Ability.Grapple;
                float distance = math.length(new float2(toPos - fromPos));
                
                navBuffer.Add(new RoomNavigationElement(fromPos, toPos, secretMovement, distance, true));
            }
        }

        [BurstCompile]
        private static bool IsTraversable(TileType tile)
        {
            return tile == TileType.Platform || tile == TileType.Climbable;
        }

        /// <summary>
        /// Get tilemap generation configuration for a template
        /// TODO: Implement proper configuration system
        /// </summary>
        private static TilemapConfig GetTilemapGenerationConfig(Entity template)
        {
            return new TilemapConfig
            {
                HasGroundLevel = true,
                GroundThickness = 2,
                WallThickness = 1,
                PlatformFrequency = 0.3f
            };
        }

        [BurstCompile]
        private static bool IsWall(int2 position, RectInt bounds, NativeArray<TileType> tilemap)
        {
            if (!IsWithinBounds(position, bounds)) return true; // Boundary counts as wall
            
            int index = position.y * bounds.width + position.x;
            return tilemap[index] == TileType.Solid;
        }

        [BurstCompile]
        private static bool IsWithinBounds(int2 position, RectInt bounds)
        {
            return position.x >= 0 && position.x < bounds.width &&
                   position.y >= 0 && position.y < bounds.height;
        }
    }

    /// <summary>
    /// Simple tile types for navigation generation
    /// In real implementation, this would map to actual tilemap tile types
    /// </summary>
    public enum TileType : byte
    {
        Empty = 0,      // Air/empty space
        Solid = 1,      // Solid wall/ground
        Platform = 2,   // Walkable platform
        Climbable = 3,  // Wall that can be climbed
        Hazard = 4      // Dangerous tile to avoid
    }

    /// <summary>
    /// Configuration for tilemap generation
    /// TODO: Move to proper configuration system
    /// </summary>
    public struct TilemapConfig
    {
        public bool HasGroundLevel;
        public int GroundThickness;
        public int WallThickness;
        public float PlatformFrequency;
    }
}