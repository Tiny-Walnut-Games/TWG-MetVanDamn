using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared; // Added for WorldConfiguration

namespace TinyWalnutGames.MetVD.Graph
{
#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
    // Editor/Test: expose SystemBase for CreateSystemManaged API
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(RoomManagementSystem))]
    public partial class ProceduralRoomGeneratorSystem : SystemBase
    {
        private EntityQuery _roomsToGenerateQuery;
        private EntityQuery _worldConfigQuery;

        protected override void OnCreate()
        {
            _roomsToGenerateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NodeId, RoomHierarchyData>()
                .WithNone<ProceduralRoomGenerated>()
                .Build(EntityManager);
            _worldConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<WorldConfiguration>()
                .Build(EntityManager);
            RequireForUpdate(_roomsToGenerateQuery);
        }

        protected override void OnUpdate()
        {
            if (_roomsToGenerateQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            WorldConfiguration worldConfig = default;
            if (!_worldConfigQuery.IsEmptyIgnoreFilter)
            {
                worldConfig = _worldConfigQuery.GetSingleton<WorldConfiguration>();
            }

            using NativeArray<Entity> roomEntities = _roomsToGenerateQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<NodeId> nodeIds = _roomsToGenerateQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            using NativeArray<RoomHierarchyData> roomData = _roomsToGenerateQuery.ToComponentDataArray<RoomHierarchyData>(Allocator.Temp);

            for (int i = 0; i < roomEntities.Length; i++)
            {
                Entity roomEntity = roomEntities[i];
                NodeId nodeId = nodeIds[i];
                RoomHierarchyData hierarchy = roomData[i];
                uint roomSeed = GenerateRoomSeed(worldConfig.Seed, nodeId);
                var random = new Unity.Mathematics.Random(roomSeed == 0 ? 1u : roomSeed);
                BiomeAffinity biomeAffinity = DetermineBiomeAffinity(nodeId, ref random);
                bool layoutOrientation = DetermineLayoutOrientation(hierarchy, biomeAffinity, ref random);
                RoomGeneratorType generatorType = SelectRoomGenerator(hierarchy.Type, biomeAffinity, layoutOrientation, ref random);
                RoomTemplate roomTemplate = CreateRoomTemplate(generatorType, hierarchy, biomeAffinity, ref random);
                EntityManager.AddComponentData(roomEntity, roomTemplate);
                EntityManager.AddComponentData(roomEntity, new ProceduralRoomGenerated(roomSeed));
                if (!EntityManager.HasBuffer<RoomNavigationElement>(roomEntity))
                {
                    EntityManager.AddBuffer<RoomNavigationElement>(roomEntity);
                }
            }
        }
#else
    // Player build: keep lightweight ISystem (internal to avoid duplicate public type exposure)
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(RoomManagementSystem))]
    internal partial struct ProceduralRoomGeneratorSystem : ISystem
    {
        private EntityQuery _roomsToGenerateQuery;
        private EntityQuery _worldConfigQuery;
        public void OnCreate(ref SystemState state)
        {
            _roomsToGenerateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NodeId, RoomHierarchyData>()
                .WithNone<ProceduralRoomGenerated>()
                .Build(ref state);
            _worldConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<WorldConfiguration>()
                .Build(ref state);
            state.RequireForUpdate(_roomsToGenerateQuery);
        }
        public void OnUpdate(ref SystemState state)
        {
            if (_roomsToGenerateQuery.IsEmpty) return;
            WorldConfiguration worldConfig = default;
            if (!_worldConfigQuery.IsEmpty)
                worldConfig = _worldConfigQuery.GetSingleton<WorldConfiguration>();
            using var roomEntities = _roomsToGenerateQuery.ToEntityArray(Allocator.Temp);
            using var nodeIds = _roomsToGenerateQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            using var roomData = _roomsToGenerateQuery.ToComponentDataArray<RoomHierarchyData>(Allocator.Temp);
            for (int i = 0; i < roomEntities.Length; i++)
            {
                var roomEntity = roomEntities[i];
                var nodeId = nodeIds[i];
                var hierarchy = roomData[i];
                uint roomSeed = GenerateRoomSeed(worldConfig.Seed, nodeId);
                var random = new Unity.Mathematics.Random(roomSeed == 0 ? 1u : roomSeed);
                var biomeAffinity = DetermineBiomeAffinity(nodeId, ref random);
                bool layoutOrientation = DetermineLayoutOrientation(hierarchy, biomeAffinity, ref random);
                var generatorType = SelectRoomGenerator(hierarchy.Type, biomeAffinity, layoutOrientation, ref random);
                var roomTemplate = CreateRoomTemplate(generatorType, hierarchy, biomeAffinity, ref random);
                state.EntityManager.AddComponentData(roomEntity, roomTemplate);
                state.EntityManager.AddComponentData(roomEntity, new ProceduralRoomGenerated(roomSeed));
                if (!state.EntityManager.HasBuffer<RoomNavigationElement>(roomEntity))
                    state.EntityManager.AddBuffer<RoomNavigationElement>(roomEntity);
            }
        }
#endif
        // Shared static logic -------------------------------------------------
        private static uint GenerateRoomSeed(int worldSeed, NodeId nodeId)
        {
            var hash = new Unity.Mathematics.Random((uint)(worldSeed == 0 ? 1 : worldSeed));
            hash.NextUInt();
            return hash.NextUInt() ^ nodeId._value ^ ((uint)nodeId.Coordinates.x << 16) ^ ((uint)nodeId.Coordinates.y << 8);
        }
        private static BiomeAffinity DetermineBiomeAffinity(NodeId nodeId, ref Unity.Mathematics.Random random)
        {
            int2 coords = nodeId.Coordinates;
            if (coords.y > 50)
            {
                return BiomeAffinity.Sky;
            }

            if (coords.y < -20)
            {
                return BiomeAffinity.Underground;
            }

            if (math.abs(coords.x) > 40)
            {
                return BiomeAffinity.Desert;
            }

            if (coords.y > 20)
            {
                return BiomeAffinity.Mountain;
            }

            return random.NextFloat() > 0.7f ? (BiomeAffinity)(random.NextInt(1, 5)) : BiomeAffinity.Forest;
        }
        private static bool DetermineLayoutOrientation(RoomHierarchyData hierarchy, BiomeAffinity biome, ref Unity.Mathematics.Random random)
        {
            RectInt bounds = hierarchy.Bounds; bool isVertical = bounds.height > bounds.width;
            return biome switch
            {
                BiomeAffinity.Sky => true,
                BiomeAffinity.Underground => random.NextFloat() > 0.6f,
                BiomeAffinity.Mountain => random.NextFloat() > 0.3f,
                _ => isVertical || random.NextFloat() > 0.5f
            };
        }
        private static RoomGeneratorType SelectRoomGenerator(RoomType roomType, BiomeAffinity biome, bool isVertical, ref Unity.Mathematics.Random random)
        {
            switch (roomType)
            {
                case RoomType.Boss: return RoomGeneratorType.PatternDrivenModular;
                case RoomType.Treasure: return random.NextFloat() > 0.6f ? RoomGeneratorType.ParametricChallenge : RoomGeneratorType.PatternDrivenModular;
                case RoomType.Hub: return RoomGeneratorType.WeightedTilePrefab;
                default:
                    if (biome == BiomeAffinity.Sky)
                    {
                        return RoomGeneratorType.SkyBiomePlatform;
                    }

                    if (isVertical)
                    {
                        return random.NextFloat() > 0.4f ? RoomGeneratorType.VerticalSegment : RoomGeneratorType.WeightedTilePrefab;
                    }

                    return random.NextFloat() > 0.4f ? RoomGeneratorType.HorizontalCorridor : RoomGeneratorType.WeightedTilePrefab;
            }
        }
        private static RoomTemplate CreateRoomTemplate(RoomGeneratorType generatorType, RoomHierarchyData hierarchy, BiomeAffinity biome, ref Unity.Mathematics.Random random)
        {
            float biomeSizeModifier = biome switch
            {
                BiomeAffinity.Desert => 0.95f, // Harsh but navigable, less forgiving than it looks
                BiomeAffinity.Forest => 1.15f, // Dense, resource-rich, full of traversal options
                BiomeAffinity.Mountain => 1.05f, // Vertical challenge, but stable terrain.
                BiomeAffinity.Ocean => 0.6f, // Movement constrained, requires special traversal.
                BiomeAffinity.Sky => 1.25f, // High mobility, rare access, peak affinity.
                BiomeAffinity.TechZone => 0.7f, // Controlled chaos. High risk, low natural flow.
                BiomeAffinity.Underground => 0.85f, // Tight corridors, limited visibility, but stable.
                BiomeAffinity.Volcanic => 0.5f, // Hostile, unstable, traversal punished.
                BiomeAffinity.Any => 0f, // 	Null glyph. Should never be used directly.
                _ => throw new System.NotImplementedException() // When adding biomes, update: DetermineLayoutOrientation, SelectRoomGenerator, and ConvertBiomeTypeToAffinity in ProceduralRoomGeneration.cs
            };                                                  // Also check TerrainAndSkyGenerators.cs for BiomeType switches if adding new terrain types.
            RectInt bounds = hierarchy.Bounds;
            int2 baseMinSize = new(math.max(2, bounds.width / 2), math.max(2, bounds.height / 2));
            int2 baseMaxSize = new(bounds.width, bounds.height);

            // Apply biome size modifier while respecting minimums
            int2 minSize = (int2)math.max((float2)baseMinSize, (float2)baseMinSize * biomeSizeModifier);
            int2 maxSize = (int2)math.max((float2)minSize, (float2)baseMaxSize * biomeSizeModifier);
            MovementCapabilityTags movementTags = GenerateMovementCapabilities(generatorType, hierarchy.Type, ref random);
            float secretPercent = hierarchy.Type switch
            {
                RoomType.Treasure => 0.3f,
                RoomType.Boss => 0.1f,
                RoomType.Hub => 0.2f,
                _ => 0.15f
            };
            bool needsJumpValidation = generatorType is RoomGeneratorType.PatternDrivenModular or RoomGeneratorType.ParametricChallenge;
            return new RoomTemplate(generatorType, movementTags, minSize, maxSize, secretPercent, needsJumpValidation, random.NextUInt());
        }
        private static MovementCapabilityTags GenerateMovementCapabilities(RoomGeneratorType generatorType, RoomType roomType, ref Unity.Mathematics.Random random)
        {
            Ability required; Ability optional; float difficulty;
            switch (generatorType)
            {
                case RoomGeneratorType.PatternDrivenModular:
                    Ability[] skillChoices = new[] { Ability.Dash, Ability.WallJump, Ability.Grapple, Ability.DoubleJump };
                    required = skillChoices[random.NextInt(0, skillChoices.Length)];
                    optional = skillChoices[random.NextInt(0, skillChoices.Length)];
                    difficulty = random.NextFloat(0.6f, 0.9f);
                    break;
                case RoomGeneratorType.ParametricChallenge:
                    required = random.NextFloat() > 0.5f ? Ability.Jump : Ability.DoubleJump;
                    optional = Ability.WallJump; difficulty = random.NextFloat(0.4f, 0.8f); break;
                case RoomGeneratorType.SkyBiomePlatform:
                    required = random.NextFloat() > 0.7f ? Ability.DoubleJump : Ability.Jump;
                    optional = Ability.GlideSpeed | Ability.Dash; difficulty = random.NextFloat(0.5f, 0.8f); break;
                default:
                    required = Ability.Jump; optional = random.NextFloat() > 0.6f ? Ability.DoubleJump : Ability.None; difficulty = random.NextFloat(0.2f, 0.6f); break;
            }
            if (roomType == RoomType.Boss)
            {
                difficulty = math.max(difficulty, 0.7f);
                if (optional == Ability.None)
                {
                    optional = Ability.Dash;
                }
            }
            return new MovementCapabilityTags(required, optional, BiomeAffinity.Any, difficulty);
        }
    }
}
