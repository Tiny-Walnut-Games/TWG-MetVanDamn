using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
    /// <summary>
    /// Managed variant used in Editor & test builds to simplify unit testing.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ProceduralRoomGeneratorSystem))]
    public partial class RoomNavigationGeneratorSystem : SystemBase
    {
        private EntityQuery _roomsWithContentQuery;

        protected override void OnCreate()
        {
            _roomsWithContentQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NodeId, RoomHierarchyData, RoomTemplate, ProceduralRoomGenerated>()
                .WithAll<RoomNavigationElement>()
                .Build(EntityManager);
            RequireForUpdate(_roomsWithContentQuery);
        }

        [BurstCompile]
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
                var genStatus = EntityManager.GetComponentData<ProceduralRoomGenerated>(roomEntity);
                if (genStatus.NavigationGenerated) continue;
                GenerateRoomNavigation(EntityManager, roomEntity, hierarchy, template, nodeId, ref genStatus);
                genStatus.NavigationGenerated = true;
                EntityManager.SetComponentData(roomEntity, genStatus);
            }
        }

        // Share static helper with player variant
        [BurstCompile]
        private static void GenerateRoomNavigation(EntityManager entityManager, Entity roomEntity,
            RoomHierarchyData hierarchy, RoomTemplate template, NodeId nodeId, ref ProceduralRoomGenerated genStatus)
        {
            var navBuffer = entityManager.GetBuffer<RoomNavigationElement>(roomEntity);
            navBuffer.Clear();
            var bounds = hierarchy.Bounds;
            var random = new Unity.Mathematics.Random(genStatus.GenerationSeed == 0 ? 1u : genStatus.GenerationSeed);
            var physics = GeneratePhysicsForRoom(template, ref random);
            var tilemap = SimulateRoomTilemap(bounds, template, ref random);
            ApplyEmptyAboveTraversableRule(bounds, tilemap, navBuffer);
            CalculateJumpVectorConnections(bounds, tilemap, physics, template.CapabilityTags.RequiredSkills, navBuffer);
            if (template.SecretAreaPercentage > 0)
                AddSecretRouteConnections(bounds, tilemap, physics, template.CapabilityTags.OptionalSkills, navBuffer, ref random);
            tilemap.Dispose();
        }
#else
    /// <summary>
    /// Lightweight player build variant (unmanaged) for performance.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ProceduralRoomGeneratorSystem))]
    public partial struct RoomNavigationGeneratorSystem : ISystem
    {
        private EntityQuery _roomsWithContentQuery;

        public void OnCreate(ref SystemState state)
        {
            _roomsWithContentQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NodeId, RoomHierarchyData, RoomTemplate, ProceduralRoomGenerated>()
                .WithAll<RoomNavigationElement>()
                .Build(ref state);
            state.RequireForUpdate(_roomsWithContentQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
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
                var genStatus = state.EntityManager.GetComponentData<ProceduralRoomGenerated>(roomEntity);
                if (genStatus.NavigationGenerated) continue;
                GenerateRoomNavigation(state.EntityManager, roomEntity, hierarchy, template, nodeId, ref genStatus);
                genStatus.NavigationGenerated = true;
                state.EntityManager.SetComponentData(roomEntity, genStatus);
            }
        }

        [BurstCompile]
        private static void GenerateRoomNavigation(EntityManager entityManager, Entity roomEntity,
            RoomHierarchyData hierarchy, RoomTemplate template, NodeId nodeId, ref ProceduralRoomGenerated genStatus)
        {
            var navBuffer = entityManager.GetBuffer<RoomNavigationElement>(roomEntity);
            navBuffer.Clear();
            var bounds = hierarchy.Bounds;
            var random = new Unity.Mathematics.Random(genStatus.GenerationSeed == 0 ? 1u : genStatus.GenerationSeed);
            var physics = GeneratePhysicsForRoom(template, ref random);
            var tilemap = SimulateRoomTilemap(bounds, template, ref random);
            ApplyEmptyAboveTraversableRule(bounds, tilemap, navBuffer);
            CalculateJumpVectorConnections(bounds, tilemap, physics, template.CapabilityTags.RequiredSkills, navBuffer);
            if (template.SecretAreaPercentage > 0)
                AddSecretRouteConnections(bounds, tilemap, physics, template.CapabilityTags.OptionalSkills, navBuffer, ref random);
            tilemap.Dispose();
        }
#endif
        // -------------------------------- Shared static helper methods (unchanged) --------------------------------
        [BurstCompile]
        private static JumpArcPhysics GeneratePhysicsForRoom(RoomTemplate template, ref Unity.Mathematics.Random random)
        {
            var physics = new JumpArcPhysics();
            switch (template.GeneratorType)
            {
                case RoomGeneratorType.ParametricChallenge:
                    physics.JumpHeight = 2.5f + random.NextFloat(-0.5f, 0.5f);
                    physics.JumpDistance = 3.5f + random.NextFloat(-0.5f, 0.5f);
                    break;
                case RoomGeneratorType.SkyBiomePlatform:
                    physics.JumpHeight = 4.0f; physics.JumpDistance = 5.0f; physics.GlideSpeed = 8.0f; break;
                case RoomGeneratorType.VerticalSegment:
                    physics.WallJumpHeight = 3.0f; physics.JumpHeight = 2.0f; break;
            }
            return physics;
        }
        [BurstCompile]
        private static NativeArray<TileType> SimulateRoomTilemap(RectInt bounds, RoomTemplate template, ref Unity.Mathematics.Random random)
        {
            int tileCount = bounds.width * bounds.height;
            var tilemap = new NativeArray<TileType>(tileCount, Allocator.Temp);
            var config = GetTilemapGenerationConfig(template);
            for (int y = 0; y < bounds.height; y++)
            {
                for (int x = 0; x < bounds.width; x++)
                {
                    int index = y * bounds.width + x;
                    if ((config.HasGroundLevel && y == 0) || (config.HasWalls && (x == 0 || x == bounds.width - 1)))
                        tilemap[index] = TileType.Solid;
                    else if (config.HasGroundLevel && y == 1)
                        tilemap[index] = TileType.Platform;
                    else if (random.NextFloat() < config.PlatformProbability)
                        tilemap[index] = TileType.Platform;
                    else if (random.NextFloat() < config.ClimbableProbability)
                        tilemap[index] = TileType.Climbable;
                    else
                        tilemap[index] = TileType.Empty;
                }
            }
            return tilemap;
        }
        [BurstCompile]
        private static void ApplyEmptyAboveTraversableRule(RectInt bounds, NativeArray<TileType> tilemap, DynamicBuffer<RoomNavigationElement> navBuffer)
        {
            for (int y = 1; y < bounds.height; y++)
            {
                for (int x = 0; x < bounds.width; x++)
                {
                    int currentIndex = y * bounds.width + x;
                    int belowIndex = (y - 1) * bounds.width + x;
                    var currentTile = tilemap[currentIndex];
                    var belowTile = tilemap[belowIndex];
                    if (currentTile == TileType.Empty && IsTraversable(belowTile))
                    {
                        var fromPos = new int2(x, y - 1);
                        var toPos = new int2(x, y);
                        navBuffer.Add(new RoomNavigationElement(fromPos, toPos, Ability.Jump, 1.0f, false));
                        navBuffer.Add(new RoomNavigationElement(toPos, fromPos, Ability.None, 0.5f, false));
                    }
                }
            }
        }
        [BurstCompile]
        private static void CalculateJumpVectorConnections(RectInt bounds, NativeArray<TileType> tilemap, JumpArcPhysics physics, Ability requiredSkills, DynamicBuffer<RoomNavigationElement> navBuffer)
        {
            for (int y = 0; y < bounds.height; y++)
            for (int x = 0; x < bounds.width; x++)
            {
                int index = y * bounds.width + x; var currentTile = tilemap[index]; if (!IsTraversable(currentTile)) continue; var fromPos = new int2(x, y);
                AddJumpConnections(fromPos, bounds, tilemap, physics, requiredSkills, navBuffer);
                AddDashConnections(fromPos, bounds, tilemap, physics, requiredSkills, navBuffer);
                AddWallJumpConnections(fromPos, bounds, tilemap, physics, requiredSkills, navBuffer);
            }
        }
        [BurstCompile]
        private static void AddJumpConnections(int2 fromPos, RectInt bounds, NativeArray<TileType> tilemap, JumpArcPhysics physics, Ability requiredSkills, DynamicBuffer<RoomNavigationElement> navBuffer)
        {
            if ((requiredSkills & Ability.Jump) == 0) return; int jumpRange = (int)physics.JumpDistance; int jumpHeight = (int)physics.JumpHeight;
            for (int dx = -jumpRange; dx <= jumpRange; dx++)
            for (int dy = -1; dy <= jumpHeight; dy++)
            {
                if (dx == 0 && dy == 0) continue; var toPos = fromPos + new int2(dx, dy); if (!IsWithinBounds(toPos, bounds)) continue; int toIndex = toPos.y * bounds.width + toPos.x; var toTile = tilemap[toIndex];
                if (IsTraversable(toTile) || toTile == TileType.Empty)
                {
                    Ability movement = Ability.Jump; float cost = math.length(new float2(dx, dy));
                    if (math.abs(dx) > physics.JumpDistance * 0.7f || dy > physics.JumpHeight * 0.7f) { movement |= Ability.DoubleJump; cost *= 1.5f; }
                    navBuffer.Add(new RoomNavigationElement(fromPos, toPos, movement, cost, false));
                }
            }
        }
        [BurstCompile]
        private static void AddDashConnections(int2 fromPos, RectInt bounds, NativeArray<TileType> tilemap, JumpArcPhysics physics, Ability requiredSkills, DynamicBuffer<RoomNavigationElement> navBuffer)
        {
            if ((requiredSkills & Ability.Dash) == 0) return; int dashRange = (int)physics.DashDistance;
            for (int dx = -dashRange; dx <= dashRange; dx++)
            {
                if (dx == 0) continue; var toPos = fromPos + new int2(dx, 0); if (!IsWithinBounds(toPos, bounds)) continue; int toIndex = toPos.y * bounds.width + toPos.x; var toTile = tilemap[toIndex];
                if (IsTraversable(toTile) || toTile == TileType.Empty)
                    navBuffer.Add(new RoomNavigationElement(fromPos, toPos, Ability.Dash, math.abs(dx), false));
            }
        }
        [BurstCompile]
        private static void AddWallJumpConnections(int2 fromPos, RectInt bounds, NativeArray<TileType> tilemap, JumpArcPhysics physics, Ability requiredSkills, DynamicBuffer<RoomNavigationElement> navBuffer)
        {
            if ((requiredSkills & Ability.WallJump) == 0) return; var leftWall = fromPos + new int2(-1, 0); var rightWall = fromPos + new int2(1, 0);
            bool hasLeftWall = IsWall(leftWall, bounds, tilemap); bool hasRightWall = IsWall(rightWall, bounds, tilemap); if (!hasLeftWall && !hasRightWall) return; int wallJumpHeight = (int)physics.WallJumpHeight;
            for (int dy = 1; dy <= wallJumpHeight; dy++)
            {
                var toPos = fromPos + new int2(0, dy); if (!IsWithinBounds(toPos, bounds)) continue; int toIndex = toPos.y * bounds.width + toPos.x; var toTile = tilemap[toIndex];
                if (IsTraversable(toTile) || toTile == TileType.Empty)
                    navBuffer.Add(new RoomNavigationElement(fromPos, toPos, Ability.WallJump, dy, false));
            }
        }
        [BurstCompile]
        private static void AddSecretRouteConnections(RectInt bounds, NativeArray<TileType> tilemap, JumpArcPhysics physics, Ability optionalSkills, DynamicBuffer<RoomNavigationElement> navBuffer, ref Unity.Mathematics.Random random)
        {
            int secretCount = (int)(bounds.width * bounds.height * 0.05f);
            for (int i = 0; i < secretCount; i++)
            {
                var fromPos = new int2(random.NextInt(0, bounds.width), random.NextInt(0, bounds.height));
                var toPos = new int2(random.NextInt(0, bounds.width), random.NextInt(0, bounds.height));
                if (math.all(fromPos == toPos)) continue; Ability secretMovement = optionalSkills != Ability.None ? optionalSkills : Ability.Grapple; float distance = math.length(new float2(toPos - fromPos));
                navBuffer.Add(new RoomNavigationElement(fromPos, toPos, secretMovement, distance, true));
            }
        }
        [BurstCompile]
        private static bool IsTraversable(TileType tile) => tile == TileType.Platform || tile == TileType.Climbable;
        [BurstCompile]
        private static bool IsWall(int2 position, RectInt bounds, NativeArray<TileType> tilemap)
        { if (!IsWithinBounds(position, bounds)) return true; int index = position.y * bounds.width + position.x; return tilemap[index] == TileType.Solid; }
        [BurstCompile]
        private static bool IsWithinBounds(int2 position, RectInt bounds) => position.x >= 0 && position.x < bounds.width && position.y >= 0 && position.y < bounds.height;
        private static TilemapConfig GetTilemapGenerationConfig(RoomTemplate template)
        { bool vertical = template.GeneratorType == RoomGeneratorType.VerticalSegment; return new TilemapConfig { HasGroundLevel = true, HasWalls = true, PlatformProbability = vertical ? 0.15f : 0.10f, ClimbableProbability = vertical ? 0.08f : 0.04f, GroundPercentage = vertical ? 0.3f : 0.6f, PlatformPercentage = 0.2f, EmptyPercentage = template.SecretAreaPercentage + 0.1f, WallThickness = 1 }; }
    }

    public struct TilemapConfig
    {
        public bool HasGroundLevel; public bool HasWalls; public float PlatformProbability; public float ClimbableProbability; public float GroundPercentage; public float PlatformPercentage; public float EmptyPercentage; public int WallThickness;
    }

    public enum TileType : byte { Empty = 0, Solid = 1, Platform = 2, Climbable = 3, Hazard = 4 }
}
