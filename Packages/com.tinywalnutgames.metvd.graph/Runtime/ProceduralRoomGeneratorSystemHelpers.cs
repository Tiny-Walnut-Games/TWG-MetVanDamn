using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Graph
	{
	internal static partial class ProceduralRoomGeneratorSystemHelpers
		{
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
			var minSize = (int2)math.max((float2)baseMinSize, (float2)baseMinSize * biomeSizeModifier);
			var maxSize = (int2)math.max((float2)minSize, (float2)baseMaxSize * biomeSizeModifier);
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

			return math.abs(coords.x) > 40
				? BiomeAffinity.Desert
				: coords.y > 20
				? BiomeAffinity.Mountain
				: random.NextFloat() > 0.7f ? (BiomeAffinity)random.NextInt(1, 5) : BiomeAffinity.Forest;
			}
		private static bool DetermineLayoutOrientation(RoomHierarchyData hierarchy, BiomeAffinity biome, ref Unity.Mathematics.Random random)
			{
			RectInt bounds = hierarchy.Bounds;
			bool isVertical = bounds.height > bounds.width;

			return biome switch
				{
					BiomeAffinity.Sky => true, // Sky biomes always prefer vertical layouts
					BiomeAffinity.Underground => random.NextFloat() > 0.6f, // Underground prefers horizontal but can be vertical
					BiomeAffinity.Mountain => random.NextFloat() > 0.3f, // Mountain biomes favor vertical layouts
					BiomeAffinity.Forest => isVertical || random.NextFloat() > 0.4f, // Forest adapts to room shape with bias toward vertical
					BiomeAffinity.Desert => random.NextFloat() > 0.7f, // Desert strongly prefers horizontal layouts
					BiomeAffinity.Ocean => random.NextFloat() > 0.8f, // Ocean heavily favors horizontal movement
					BiomeAffinity.TechZone => random.NextFloat() > 0.5f, // TechZone is neutral orientation
					BiomeAffinity.Volcanic => random.NextFloat() > 0.6f, // Volcanic prefers horizontal escape routes
					BiomeAffinity.Any => isVertical || random.NextFloat() > 0.5f, // Fallback to room shape with random variation
					_ => isVertical || random.NextFloat() > 0.5f // Default fallback
					};
			}
		private static MovementCapabilityTags GenerateMovementCapabilities(RoomGeneratorType generatorType, RoomType roomType, ref Unity.Mathematics.Random random)
			{
			Ability required = Ability.Jump; // Initialize required
			Ability optional = Ability.None; // Initialize optional
			float difficulty = 0.5f; // Initialize difficulty

			switch (generatorType)
				{
				case RoomGeneratorType.PatternDrivenModular:
					Ability [ ] skillChoices = new [ ] { Ability.Dash, Ability.WallJump, Ability.Grapple, Ability.DoubleJump };
					required = skillChoices [ random.NextInt(0, skillChoices.Length) ];
					optional = skillChoices [ random.NextInt(0, skillChoices.Length) ];
					difficulty = random.NextFloat(0.6f, 0.9f);
					break;
				case RoomGeneratorType.ParametricChallenge:
					required = random.NextFloat() > 0.5f ? Ability.Jump : Ability.DoubleJump;
					optional = Ability.WallJump;
					difficulty = random.NextFloat(0.4f, 0.8f);
					break;
				case RoomGeneratorType.SkyBiomePlatform:
					required = random.NextFloat() > 0.7f ? Ability.DoubleJump : Ability.Jump;
					optional = Ability.GlideSpeed | Ability.Dash;
					difficulty = random.NextFloat(0.5f, 0.8f);
					break;
				case RoomGeneratorType.WeightedTilePrefab:
					required = Ability.Jump;
					optional = Ability.None;
					difficulty = random.NextFloat(0.2f, 0.5f);
					break;
				case RoomGeneratorType.VerticalSegment:
					required = Ability.Jump;
					optional = Ability.DoubleJump;
					difficulty = random.NextFloat(0.3f, 0.6f);
					break;
				case RoomGeneratorType.HorizontalCorridor:
					required = Ability.Jump;
					optional = random.NextFloat() > 0.5f ? Ability.Dash : Ability.None;
					difficulty = random.NextFloat(0.2f, 0.5f);
					break;
				case RoomGeneratorType.BiomeWeightedTerrain:
				case RoomGeneratorType.LinearBranchingCorridor:
				case RoomGeneratorType.StackedSegment:
				case RoomGeneratorType.LayeredPlatformCloud:
				case RoomGeneratorType.BiomeWeightedHeightmap:
					required = Ability.Jump;
					optional = random.NextFloat() > 0.6f ? Ability.DoubleJump : Ability.None;
					difficulty = random.NextFloat(0.2f, 0.6f);
					break;
				default:
					required = Ability.Jump;
					optional = random.NextFloat() > 0.6f ? Ability.DoubleJump : Ability.None;
					difficulty = random.NextFloat(0.2f, 0.6f);
					break;
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
				using NativeArray<Entity> roomEntities = _roomsToGenerateQuery.ToEntityArray(Allocator.Temp);
				using NativeArray<NodeId> nodeIds = _roomsToGenerateQuery.ToComponentDataArray<NodeId>(Allocator.Temp);
				using NativeArray<RoomHierarchyData> roomData = _roomsToGenerateQuery.ToComponentDataArray<RoomHierarchyData>(Allocator.Temp);
				for (int i = 0; i < roomEntities.Length; i++)
					{
					Entity roomEntity = roomEntities [ i ];
					NodeId nodeId = nodeIds [ i ];
					RoomHierarchyData hierarchy = roomData [ i ];
					uint roomSeed = GenerateRoomSeed(worldConfig.Seed, nodeId);
					var random = new Unity.Mathematics.Random(roomSeed == 0 ? 1u : roomSeed);
					BiomeAffinity biomeAffinity = DetermineBiomeAffinity(nodeId, ref random);
					bool layoutOrientation = DetermineLayoutOrientation(hierarchy, biomeAffinity, ref random);
					RoomGeneratorType generatorType = SelectRoomGenerator(hierarchy.Type, biomeAffinity, layoutOrientation, ref random);
					RoomTemplate roomTemplate = CreateRoomTemplate(generatorType, hierarchy, biomeAffinity, ref random);
					state.EntityManager.AddComponentData(roomEntity, roomTemplate);
					state.EntityManager.AddComponentData(roomEntity, new ProceduralRoomGenerated(roomSeed));
					if (!state.EntityManager.HasBuffer<RoomNavigationElement>(roomEntity))
						state.EntityManager.AddBuffer<RoomNavigationElement>(roomEntity);
					}
				}

			// Shared static logic -------------------------------------------------
			private static uint GenerateRoomSeed(int worldSeed, NodeId nodeId)
				{
				var hash = new Unity.Mathematics.Random((uint)(worldSeed == 0 ? 1 : worldSeed));
				hash.NextUInt();
				return hash.NextUInt() ^ nodeId._value ^ ((uint)nodeId.Coordinates.x << 16) ^ ((uint)nodeId.Coordinates.y << 8);
				}

			private static RoomGeneratorType SelectRoomGenerator(RoomType roomType, BiomeAffinity biome, bool isVertical, ref Unity.Mathematics.Random random)
				{
				switch (roomType)
					{
					case RoomType.Boss:
						return RoomGeneratorType.PatternDrivenModular;
					case RoomType.Treasure:
						return random.NextFloat() > 0.6f ? RoomGeneratorType.ParametricChallenge : RoomGeneratorType.PatternDrivenModular;
					case RoomType.Hub:
						return RoomGeneratorType.WeightedTilePrefab;
					case RoomType.Normal:
					case RoomType.Entrance:
					case RoomType.Exit:
					case RoomType.Shop:
					case RoomType.Save:
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
			}
		}
	}
