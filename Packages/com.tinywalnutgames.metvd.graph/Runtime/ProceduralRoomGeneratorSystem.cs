using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Core procedural room generation system implementing the Master Spec pipeline flow:
    /// 1. Biome Selection → 2. Layout Type → 3. Room Generator Choice → 4. Content Pass → 
    /// 5. Biome-Specific Overrides → 6. Nav Generation → 7. Cinemachine Zone Generation
    /// 
    /// Runs after room hierarchy creation but before navigation/camera systems
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(RoomManagementSystem))]
    public partial class ProceduralRoomGeneratorSystem : SystemBase
    {
        private EntityQuery _roomsToGenerateQuery;
        private EntityQuery _worldConfigQuery;
        
        protected override void OnCreate()
        {
            // Rooms that need procedural generation
            _roomsToGenerateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NodeId, RoomHierarchyData>()
                .WithNone<ProceduralRoomGenerated>()
                .Build(this);
                
            // World configuration for biome and rule access
            _worldConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<WorldConfiguration>()
                .Build(this);
                
            RequireForUpdate(_roomsToGenerateQuery);
        }

        protected override void OnUpdate()
        {
            if (_roomsToGenerateQuery.IsEmpty) return;
            
            // Get world configuration for seeding and biome rules
            WorldConfiguration worldConfig = default;
            if (!_worldConfigQuery.IsEmpty)
            {
                worldConfig = _worldConfigQuery.GetSingleton<WorldConfiguration>();
            }

            using var roomEntities = _roomsToGenerateQuery.ToEntityArray(Allocator.Temp);
            using var nodeIds = _roomsToGenerateQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
            using var roomData = _roomsToGenerateQuery.ToComponentDataArray<RoomHierarchyData>(Allocator.Temp);

            for (int i = 0; i < roomEntities.Length; i++)
            {
                var roomEntity = roomEntities[i];
                var nodeId = nodeIds[i];
                var hierarchy = roomData[i];
                
                // Generate deterministic seed for this room
                var roomSeed = GenerateRoomSeed(worldConfig.Seed, nodeId);
                var random = new Unity.Mathematics.Random(roomSeed);

                // Phase 1: Biome Selection (get from parent or determine from world rules)
                var biomeAffinity = DetermineBiomeAffinity(nodeId, ref random);
                
                // Phase 2: Layout Type (vertical vs horizontal based on room bounds and biome)
                var layoutOrientation = DetermineLayoutOrientation(hierarchy, biomeAffinity, ref random);
                
                // Phase 3: Room Generator Choice (select generator based on room type and biome constraints)
                var generatorType = SelectRoomGenerator(hierarchy.Type, biomeAffinity, layoutOrientation, ref random);
                
                // Phase 4: Content Pass - Create room template and mark for content generation
                var roomTemplate = CreateRoomTemplate(generatorType, hierarchy, biomeAffinity, ref random);
                
                // Add components to mark this room for generation pipeline
                EntityManager.AddComponentData(roomEntity, roomTemplate);
                EntityManager.AddComponentData(roomEntity, new ProceduralRoomGenerated(roomSeed));
                
                // Add navigation buffer for future nav generation
                if (!EntityManager.HasBuffer<RoomNavigationElement>(roomEntity))
                {
                    EntityManager.AddBuffer<RoomNavigationElement>(roomEntity);
                }
            }
        }

        [BurstCompile]
        private static uint GenerateRoomSeed(int worldSeed, NodeId nodeId)
        {
            // Create deterministic seed from world seed and room ID
            var hash = new Unity.Mathematics.Random((uint)worldSeed);
            hash.NextUInt(); // Advance state
            return hash.NextUInt() ^ nodeId.Value ^ ((uint)nodeId.Coordinates.x << 16) ^ ((uint)nodeId.Coordinates.y << 8);
        }

        [BurstCompile]
        private static BiomeAffinity DetermineBiomeAffinity(NodeId nodeId, ref Unity.Mathematics.Random random)
        {
            // Use spatial coordinates to determine biome affinity
            // This could be extended to read from parent district's biome data
            var coords = nodeId.Coordinates;
            
            // Simple biome determination based on position - can be made more sophisticated
            if (coords.y > 50) return BiomeAffinity.Sky;
            if (coords.y < -20) return BiomeAffinity.Underground;
            if (math.abs(coords.x) > 40) return BiomeAffinity.Desert;
            if (coords.y > 20) return BiomeAffinity.Mountain;
            
            // Default to forest with some randomization
            return random.NextFloat() > 0.7f ? (BiomeAffinity)(random.NextInt(1, 5)) : BiomeAffinity.Forest;
        }

        [BurstCompile]
        private static bool DetermineLayoutOrientation(RoomHierarchyData hierarchy, BiomeAffinity biome, ref Unity.Mathematics.Random random)
        {
            // true = vertical, false = horizontal
            var bounds = hierarchy.Bounds;
            bool isVertical = bounds.height > bounds.width;
            
            // Biome influences layout preference
            switch (biome)
            {
                case BiomeAffinity.Sky:
                    return true; // Sky biome favors verticality
                case BiomeAffinity.Underground:
                    return random.NextFloat() > 0.6f; // Underground slightly prefers horizontal
                case BiomeAffinity.Mountain:
                    return random.NextFloat() > 0.3f; // Mountains prefer vertical
                default:
                    return isVertical || random.NextFloat() > 0.5f;
            }
        }

        [BurstCompile]
        private static RoomGeneratorType SelectRoomGenerator(RoomType roomType, BiomeAffinity biome, bool isVertical, ref Unity.Mathematics.Random random)
        {
            // Select generator based on room type, biome constraints, and layout preference
            switch (roomType)
            {
                case RoomType.Boss:
                    // Boss rooms typically use pattern-driven generation for skill challenges
                    return RoomGeneratorType.PatternDrivenModular;
                    
                case RoomType.Treasure:
                    // Treasure rooms often have skill-based access challenges
                    return random.NextFloat() > 0.6f ? RoomGeneratorType.ParametricChallenge : RoomGeneratorType.PatternDrivenModular;
                    
                case RoomType.Hub:
                    // Hub rooms use standard platforming with good connectivity
                    return RoomGeneratorType.WeightedTilePrefab;
                    
                case RoomType.Normal:
                default:
                    // Normal rooms vary based on biome and layout
                    if (biome == BiomeAffinity.Sky)
                        return RoomGeneratorType.SkyBiomePlatform;
                    
                    if (isVertical)
                        return random.NextFloat() > 0.4f ? RoomGeneratorType.VerticalSegment : RoomGeneratorType.WeightedTilePrefab;
                    else
                        return random.NextFloat() > 0.4f ? RoomGeneratorType.HorizontalCorridor : RoomGeneratorType.WeightedTilePrefab;
            }
        }

        [BurstCompile]
        private static RoomTemplate CreateRoomTemplate(RoomGeneratorType generatorType, RoomHierarchyData hierarchy, BiomeAffinity biome, ref Unity.Mathematics.Random random)
        {
            var bounds = hierarchy.Bounds;
            var minSize = new int2(math.max(2, bounds.width / 2), math.max(2, bounds.height / 2));
            var maxSize = new int2(bounds.width, bounds.height);
            
            // Generate movement capability requirements based on generator type
            var movementTags = GenerateMovementCapabilities(generatorType, hierarchy.Type, ref random);
            
            // Secret area percentage varies by room type
            float secretPercent = hierarchy.Type switch
            {
                RoomType.Treasure => 0.3f,  // Treasure rooms have more secrets
                RoomType.Boss => 0.1f,      // Boss rooms focus on main challenge
                RoomType.Hub => 0.2f,       // Hub rooms have moderate secrets
                _ => 0.15f                   // Normal rooms have standard secrets
            };
            
            // Skill-based rooms need jump validation
            bool needsJumpValidation = generatorType is RoomGeneratorType.PatternDrivenModular or RoomGeneratorType.ParametricChallenge;
            
            return new RoomTemplate(
                generatorType,
                movementTags,
                minSize,
                maxSize,
                secretPercent,
                needsJumpValidation,
                random.NextUInt()
            );
        }

        [BurstCompile]
        private static MovementCapabilityTags GenerateMovementCapabilities(RoomGeneratorType generatorType, RoomType roomType, ref Unity.Mathematics.Random random)
        {
            Ability required = Ability.None;
            Ability optional = Ability.None;
            float difficulty = 0.5f;
            
            switch (generatorType)
            {
                case RoomGeneratorType.PatternDrivenModular:
                    // Movement skill puzzle rooms require specific abilities
                    var skillChoices = new[] { Ability.Dash, Ability.WallJump, Ability.Grapple, Ability.DoubleJump };
                    required = skillChoices[random.NextInt(0, skillChoices.Length)];
                    optional = skillChoices[random.NextInt(0, skillChoices.Length)];
                    difficulty = random.NextFloat(0.6f, 0.9f);
                    break;
                    
                case RoomGeneratorType.ParametricChallenge:
                    // Testing grounds focus on jump mechanics
                    required = random.NextFloat() > 0.5f ? Ability.Jump : Ability.DoubleJump;
                    optional = Ability.WallJump;
                    difficulty = random.NextFloat(0.4f, 0.8f);
                    break;
                    
                case RoomGeneratorType.SkyBiomePlatform:
                    // Sky biome emphasizes movement abilities
                    required = random.NextFloat() > 0.7f ? Ability.DoubleJump : Ability.Jump;
                    optional = Ability.GlideSpeed | Ability.Dash;
                    difficulty = random.NextFloat(0.5f, 0.8f);
                    break;
                    
                default:
                    // Standard rooms have basic requirements
                    required = Ability.Jump;
                    optional = random.NextFloat() > 0.6f ? Ability.DoubleJump : Ability.None;
                    difficulty = random.NextFloat(0.2f, 0.6f);
                    break;
            }
            
            // Boss rooms are generally harder
            if (roomType == RoomType.Boss)
            {
                difficulty = math.max(difficulty, 0.7f);
                if (optional == Ability.None)
                    optional = Ability.Dash; // Boss rooms often have dash requirements
            }
            
            return new MovementCapabilityTags(required, optional, BiomeAffinity.Any, difficulty);
        }
    }
}