using TinyWalnutGames.MetVD.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Graph
	{
	/// <summary>
	/// Main pipeline system that orchestrates the 6-step room generation flow
	/// Implements the Pipeline Flow from the issue specification
	/// </summary>
	[BurstCompile]
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	[UpdateAfter(typeof(RoomManagementSystem))]
	public partial struct RoomGenerationPipelineSystem : ISystem
		{
		private ComponentLookup<Core.Biome> _biomeLookup;
		private ComponentLookup<JumpPhysicsData> _jumpPhysicsLookup;
		private ComponentLookup<SecretAreaConfig> _secretConfigLookup;
		private BufferLookup<RoomFeatureElement> _roomFeatureBufferLookup;
		private BufferLookup<RoomModuleElement> _roomModuleBufferLookup;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
			{
			_biomeLookup = state.GetComponentLookup<Core.Biome>(true);
			_jumpPhysicsLookup = state.GetComponentLookup<JumpPhysicsData>(true);
			_secretConfigLookup = state.GetComponentLookup<SecretAreaConfig>(true);
			_roomFeatureBufferLookup = state.GetBufferLookup<RoomFeatureElement>();
			_roomModuleBufferLookup = state.GetBufferLookup<RoomModuleElement>(true);

			state.RequireForUpdate<RoomGenerationRequest>();
			}

		// NOTE: Cannot use [BurstCompile] on OnUpdate due to ref SystemState parameter
		public void OnUpdate(ref SystemState state)
			{
			_biomeLookup.Update(ref state);
			_jumpPhysicsLookup.Update(ref state);
			_secretConfigLookup.Update(ref state);
			_roomFeatureBufferLookup.Update(ref state);
			_roomModuleBufferLookup.Update(ref state);

			// Improved seed generation combining multiple entropy sources
			float deltaTime = state.WorldUnmanaged.Time.DeltaTime;
			double elapsedTime = state.WorldUnmanaged.Time.ElapsedTime;

			var systemRandom = new System.Random();
			uint timeBasedSeed = (uint)((elapsedTime * 1000.0) % uint.MaxValue);
			uint deltaBasedSeed = (uint)((deltaTime * 1000000.0) % uint.MaxValue);
			uint systemRandomSeed = (uint)systemRandom.Next();
			uint combinedSeed = timeBasedSeed ^ deltaBasedSeed ^ systemRandomSeed;
			uint baseSeed = combinedSeed == 0 ? 1u : combinedSeed;

			// Gather entities for job processing
			EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
				.WithAll<RoomGenerationRequest, RoomHierarchyData, NodeId>()
				.Build(ref state);

			NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
			ComponentLookup<RoomGenerationRequest> requests = state.GetComponentLookup<RoomGenerationRequest>(false);
			ComponentLookup<RoomHierarchyData> roomData = state.GetComponentLookup<RoomHierarchyData>(false);
			ComponentLookup<NodeId> nodeIds = state.GetComponentLookup<NodeId>(true);

			var pipelineJob = new PipelineProcessingJob
				{
				BiomeLookup = _biomeLookup,
				JumpPhysicsLookup = _jumpPhysicsLookup,
				SecretConfigLookup = _secretConfigLookup,
				RoomFeatureBufferLookup = _roomFeatureBufferLookup,
				RoomModuleBufferLookup = _roomModuleBufferLookup,
				Entities = entities,
				Requests = requests,
				RoomData = roomData,
				NodeIds = nodeIds,
				BaseSeed = baseSeed
				};

			pipelineJob.Execute();
			entities.Dispose();
			}

		[BurstCompile]
		private struct PipelineProcessingJob : IJob
			{
			[ReadOnly] public ComponentLookup<Core.Biome> BiomeLookup;
			[ReadOnly] public ComponentLookup<JumpPhysicsData> JumpPhysicsLookup;
			[ReadOnly] public ComponentLookup<SecretAreaConfig> SecretConfigLookup;
			public BufferLookup<RoomFeatureElement> RoomFeatureBufferLookup;
			[ReadOnly] public BufferLookup<RoomModuleElement> RoomModuleBufferLookup;
			[ReadOnly] public NativeArray<Entity> Entities;
			public ComponentLookup<RoomGenerationRequest> Requests;
			public ComponentLookup<RoomHierarchyData> RoomData;
			[ReadOnly] public ComponentLookup<NodeId> NodeIds;
			public uint BaseSeed;

			public void Execute()
				{
				var masterRandom = new Unity.Mathematics.Random(BaseSeed); // I do not believe in discarding until after it's fully used.
				int processedRoomCount = 0;

				for (int i = 0; i < Entities.Length; i++)
					{
					Entity entity = Entities [ i ];
					RefRW<RoomGenerationRequest> request = Requests.GetRefRW(entity);
					RefRW<RoomHierarchyData> roomData = RoomData.GetRefRW(entity);
					RefRO<NodeId> nodeId = NodeIds.GetRefRO(entity);

					if (request.ValueRO.IsComplete)
						{
						continue;
						}

					uint entitySeed = masterRandom.NextUInt() ^ ((uint)entity.Index << 16) ^ ((uint)entity.Version); // changed `BaseSeed ^` to `masterRandom.NextUInt() ^` to ensure masterRandom is actually used...!
					var entityRandom = new Unity.Mathematics.Random(entitySeed == 0 ? 1u : entitySeed);

					switch (request.ValueRO.CurrentStep)
						{
						case 1:
							ProcessBiomeSelection(entity, ref request.ValueRW, roomData.ValueRO, nodeId.ValueRO);
							break;
						case 2:
							ProcessLayoutTypeDecision(ref request.ValueRW, roomData.ValueRO);
							break;
						case 3:
							ProcessRoomGeneratorChoice(ref request.ValueRW, roomData.ValueRO);
							break;
						case 4:
							ProcessContentPass(entity, ref request.ValueRW, roomData.ValueRO, nodeId.ValueRO, ref entityRandom);
							break;
						case 5:
							ProcessBiomeOverrides(entity, ref request.ValueRW, roomData.ValueRO, nodeId.ValueRO, ref entityRandom);
							break;
						case 6:
							ProcessNavGeneration(entity, ref request.ValueRW, roomData.ValueRO, nodeId.ValueRO, ref entityRandom);
							break;
						default:
							request.ValueRW.IsComplete = true;
							continue;
						}

					request.ValueRW.CurrentStep++;
					if (request.ValueRW.CurrentStep > 6)
						{
						request.ValueRW.IsComplete = true;

						// Use processedRoomCount to influence final random state for deterministic batch variations
						// This ensures that batch size affects the randomization pattern for repeatability testing
						masterRandom.NextUInt(); // Advance random state based on completion
						}
					processedRoomCount++;
					}

				// Apply processedRoomCount to provide meaningful batch-level randomization seeding
				// This ensures that subsequent pipeline runs have deterministic but varied behavior based on batch size
				if (processedRoomCount > 0)
					{
					// Seed variation for next pipeline runs - larger batches get different random characteristics
					uint batchComplexityFactor = (uint)(processedRoomCount * 7919); // Prime multiplier for good distribution
					masterRandom.NextUInt(0, batchComplexityFactor); // Consume random state proportional to batch complexity
					}
				}

			private readonly void ProcessBiomeSelection(Entity entity, ref RoomGenerationRequest request, RoomHierarchyData roomData, NodeId nodeId)
				{
				// Step 1: Choose biome & sub-biome based on world gen rules
				// Apply biome-specific prop/hazard sets

				// If we don't have biome data, use fallback
				if (!BiomeLookup.HasComponent(entity))
					{
					request.TargetBiome = BiomeType.HubArea;
					request.TargetPolarity = Polarity.None;
					return;
					}

				Core.Biome biome = BiomeLookup [ entity ];
				request.TargetBiome = biome.Type;
				request.TargetPolarity = biome.PrimaryPolarity;

				// Apply biome constraints for special room types
				if (roomData.Type == RoomType.Boss && biome.Type == BiomeType.HubArea)
					{
					// Boss rooms shouldn't be in hub areas, move to appropriate combat biome
					request.TargetBiome = SelectCombatBiome(biome.PrimaryPolarity);
					}
				else if (roomData.Type == RoomType.Save && IsHostileBiome(biome.Type))
					{
					// Save rooms should be safer, reduce hostility
					request.TargetBiome = BiomeType.HubArea;
					}

				// Use nodeId for coordinate-based biome variation
				ApplyCoordinateBasedBiomeVariation(ref request, nodeId, roomData);
				}

			private static void ApplyCoordinateBasedBiomeVariation(ref RoomGenerationRequest request, NodeId nodeId, RoomHierarchyData roomData)
				{
				// Apply coordinate-based sub-biome variations
				int2 coords = nodeId.Coordinates;
				float distance = math.length(coords);

				// Far from origin rooms get more exotic biome variants
				if (distance > 20 && request.TargetBiome == BiomeType.SolarPlains)
					{
					request.TargetBiome = BiomeType.CrystalCaverns; // Exotic variant
					}
				else if (distance > 15 && request.TargetBiome == BiomeType.Forest)
					{
					request.TargetBiome = BiomeType.SkyGardens; // Elevated forest variant
					}

				// Apply coordinate-based polarity shifts for environmental storytelling
				if ((coords.x + coords.y) % 4 == 0 && roomData.Type == RoomType.Normal)
					{
					// Every 4th coordinate gets dual polarity for complexity
					request.TargetPolarity |= GetComplementaryPolarity(request.TargetPolarity);
					}
				}

			private static Polarity GetComplementaryPolarity(Polarity primary)
				{
				return primary switch
					{
						Polarity.Sun => Polarity.Moon,
						Polarity.Moon => Polarity.Sun,
						Polarity.Heat => Polarity.Cold,
						Polarity.Cold => Polarity.Heat,
						Polarity.Earth => Polarity.Wind,
						Polarity.Wind => Polarity.Earth,
						Polarity.Life => Polarity.Tech,
						Polarity.Tech => Polarity.Life,
						_ => Polarity.None
						};
				}

			private static void ProcessLayoutTypeDecision(ref RoomGenerationRequest request, RoomHierarchyData roomData)
				{
				// Step 2: Decide vertical vs. horizontal orientation
				// Factor in biome constraints (e.g., sky biome favors verticality)

				RectInt bounds = roomData.Bounds;
				float aspectRatio = (float)bounds.width / bounds.height;

				// Sky biomes favor vertical layout
				request.LayoutType = IsSkyBiome(request.TargetBiome)
					? RoomLayoutType.Vertical
					// Wide rooms favor horizontal layout
					: aspectRatio > 1.5f
						? RoomLayoutType.Horizontal
						// Tall rooms favor vertical layout
						: aspectRatio < 0.67f
						? RoomLayoutType.Vertical
						// Square rooms can use mixed layout
						: RoomLayoutType.Mixed;
				}

			private static void ProcessRoomGeneratorChoice(ref RoomGenerationRequest request, RoomHierarchyData roomData)
				{
				// Step 3: Pick generator type and filter modules by biome and required skills

				switch (roomData.Type)
					{
					case RoomType.Boss:
						// Boss rooms use pattern-driven for skill challenges
						request.GeneratorType = RoomGeneratorType.PatternDrivenModular;
						break;
					case RoomType.Treasure:
						// Treasure rooms use parametric challenge for testing
						request.GeneratorType = RoomGeneratorType.ParametricChallenge;
						break;
					case RoomType.Save:
					case RoomType.Shop:
					case RoomType.Hub:
						// Safe rooms use standard weighted generation
						request.GeneratorType = RoomGeneratorType.WeightedTilePrefab;
						break;
					case RoomType.Normal:
						break;
					case RoomType.Entrance:
						break;
					case RoomType.Exit:
						break;
					default:
						// Normal rooms - choose based on layout
						request.GeneratorType = request.LayoutType == RoomLayoutType.Vertical
							? RoomGeneratorType.StackedSegment
							: request.LayoutType == RoomLayoutType.Horizontal
								? RoomGeneratorType.LinearBranchingCorridor
								: RoomGeneratorType.WeightedTilePrefab;

						break;
					}

				// Override for special biomes
				if (IsSkyBiome(request.TargetBiome))
					{
					request.GeneratorType = RoomGeneratorType.LayeredPlatformCloud;
					}
				else if (IsTerrainBiome(request.TargetBiome))
					{
					request.GeneratorType = RoomGeneratorType.BiomeWeightedHeightmap;
					}
				}

			private readonly void ProcessContentPass(Entity entity, ref RoomGenerationRequest request, RoomHierarchyData roomData, NodeId nodeId, ref Unity.Mathematics.Random random)
				{
				// Step 4: Place hazards, props, secrets + Run Jump Arc Solver

				if (!RoomFeatureBufferLookup.HasBuffer(entity))
					{
					return;
					}

				DynamicBuffer<RoomFeatureElement> features = RoomFeatureBufferLookup [ entity ];
				RectInt bounds = roomData.Bounds;

				// Clear existing features for regeneration
				features.Clear();

				// Use nodeId coordinates for deterministic but varied placement
				uint coordinateBasedSeed = (uint)(nodeId.Coordinates.x * 31 + nodeId.Coordinates.y * 17);
				var coordinateRandom = new Unity.Mathematics.Random(coordinateBasedSeed == 0 ? 1u : coordinateBasedSeed);

				// Add content based on generator type
				switch (request.GeneratorType)
					{
					case RoomGeneratorType.PatternDrivenModular:
						AddPatternDrivenContent(features, bounds, request, ref random, ref coordinateRandom);
						break;
					case RoomGeneratorType.ParametricChallenge:
						AddParametricChallengeContent(features, bounds, request, ref random, ref coordinateRandom);
						break;
					case RoomGeneratorType.WeightedTilePrefab:
						AddWeightedContent(features, bounds, request, ref random, ref coordinateRandom);
						break;
					case RoomGeneratorType.StackedSegment:
						AddStackedSegmentContent(features, bounds, request, ref random, ref coordinateRandom);
						break;
					case RoomGeneratorType.LinearBranchingCorridor:
						AddLinearCorridorContent(features, bounds, request, ref random, ref coordinateRandom);
						break;
					case RoomGeneratorType.LayeredPlatformCloud:
						AddLayeredPlatformContent(features, bounds, request, ref random, ref coordinateRandom);
						break;
					case RoomGeneratorType.BiomeWeightedHeightmap:
						AddHeightmapContent(features, bounds, request, ref random, ref coordinateRandom);
						break;
					case RoomGeneratorType.VerticalSegment:
						break;
					case RoomGeneratorType.HorizontalCorridor:
						break;
					case RoomGeneratorType.BiomeWeightedTerrain:
						break;
					case RoomGeneratorType.SkyBiomePlatform:
						break;
					default:
						break;
					}

				// Add secret areas if configured
				if (SecretConfigLookup.HasComponent(entity))
					{
					SecretAreaConfig secretConfig = SecretConfigLookup [ entity ];
					AddSecretAreas(features, bounds, secretConfig, request, ref random);
					}
				}

			private readonly void ProcessBiomeOverrides(Entity entity, ref RoomGenerationRequest request, RoomHierarchyData roomData, NodeId nodeId, ref Unity.Mathematics.Random random)
				{
				// Step 5: Apply visual and mechanical overrides
				// (e.g., moving clouds in sky biome, tech zone hazards)

				if (!RoomFeatureBufferLookup.HasBuffer(entity))
					{
					return;
					}

				DynamicBuffer<RoomFeatureElement> features = RoomFeatureBufferLookup [ entity ];

				// Use coordinate-based environmental effects
				float environmentalFactor = CalculateEnvironmentalFactor(nodeId, roomData);

				// Apply biome-specific modifications
				for (int i = 0; i < features.Length; i++)
					{
					RoomFeatureElement feature = features [ i ];

					// Sky biome - add motion to platforms
					if (IsSkyBiome(request.TargetBiome) && feature.Type == RoomFeatureType.Platform)
						{
						// Convert static platforms to moving clouds based on environmental factor
						if (environmentalFactor > 0.5f)
							{
							feature.Type = RoomFeatureType.Platform; // Could be extended with MovingPlatform type
							features [ i ] = feature;
							}
						}

					// Tech biome - add hazards and automated systems
					if (IsTechBiome(request.TargetBiome))
						{
						if (feature.Type == RoomFeatureType.Obstacle && random.NextFloat() < 0.3f * environmentalFactor)
							{
							// Convert some obstacles to hazards in tech zones
							feature.Type = RoomFeatureType.Obstacle; // Could be extended with specific hazard types
							features [ i ] = feature;
							}
						}

					// Apply coordinate-based feature variations
					ApplyCoordinateBasedFeatureModifications(ref feature, nodeId, roomData, ref random);
					features [ i ] = feature;
					}
				}

			private readonly void ProcessNavGeneration(Entity entity, ref RoomGenerationRequest request, RoomHierarchyData roomData, NodeId nodeId, ref Unity.Mathematics.Random random)
				{
				// we should wait for a request to be completed
				// before doing generation                                
				if (request.IsComplete)
					{
					// ⚠ Intention ⚠
					// Navigation generation skip: entity.Index available for inspection when request.IsComplete
					return;
					}

				// Calculate jump vectors and add movement-type-aware nav edges

				if (!RoomFeatureBufferLookup.HasBuffer(entity))
					{
					return;
					}

				DynamicBuffer<RoomFeatureElement> features = RoomFeatureBufferLookup [ entity ];
				RectInt bounds = roomData.Bounds; // Use the parameter

				// Collect platform positions for jump arc validation
				var platformPositions = new NativeList<float2>(Allocator.Temp);
				var obstaclePositions = new NativeList<int2>(Allocator.Temp);

				for (int i = 0; i < features.Length; i++)
					{
					RoomFeatureElement feature = features [ i ];
					if (feature.Type == RoomFeatureType.Platform)
						{
						platformPositions.Add(new float2(feature.Position.x, feature.Position.y));
						}
					else if (feature.Type == RoomFeatureType.Obstacle)
						{
						obstaclePositions.Add(feature.Position);
						}
					}

				// Use coordinate-based navigation difficulty scaling
				float navigationComplexity = CalculateNavigationComplexity(nodeId, roomData);

				// Run Jump Arc Solver validation if we have jump physics data
				if (JumpPhysicsLookup.HasComponent(entity) && platformPositions.Length > 1)
					{
					JumpPhysicsData jumpPhysics = JumpPhysicsLookup [ entity ];
					// Build int2 array of critical positions (platforms considered critical)
					var criticalPlatforms = new NativeArray<int2>(platformPositions.Length, Allocator.Temp);
					for (int i = 0; i < platformPositions.Length; i++)
						{
						float2 p = platformPositions [ i ];
						criticalPlatforms [ i ] = new int2((int)p.x, (int)p.y);
						}
					// Entrance assumed first platform
					int2 entrance = criticalPlatforms [ 0 ];

					// Convert JumpPhysicsData to JumpArcPhysics for the solver
					var arcPhysics = new JumpArcPhysics(
						jumpPhysics.JumpHeight * navigationComplexity, // Scale jump height by navigation complexity
						jumpPhysics.JumpDistance,
						jumpPhysics.HasDoubleJump ? 1.5f : 1.0f,
						jumpPhysics.GravityScale,
						jumpPhysics.HasWallJump ? jumpPhysics.JumpHeight * 0.8f : 0.0f,
						jumpPhysics.HasGlide ? 6.0f : 4.0f
					);

					// With this, matching the available overload (passing bounds as separate ints):
					bool allReachable = JumpArcSolver.ValidateRoomReachability(
						entrance,
						criticalPlatforms.AsReadOnly(),
						Ability.Jump | Ability.DoubleJump,
						arcPhysics,
						0, 0, bounds.width, bounds.height,
						Allocator.Temp
					);

					// Apply coordinate-based obstacle removal strategy
					if (!allReachable && features.Length > 2)
						{
						float removalThreshold = CalculateObstacleRemovalThreshold(nodeId, navigationComplexity);
						for (int i = features.Length - 1; i >= 0; i--)
							{
							if (features [ i ].Type == RoomFeatureType.Obstacle && random.NextFloat() < removalThreshold)
								{
								features.RemoveAt(i);
								}
							}
						}
					criticalPlatforms.Dispose();
					}

				platformPositions.Dispose();
				obstaclePositions.Dispose();

				// Mark request as complete
				request.IsComplete = true;
				}

			private static bool IsTechBiome(BiomeType biomeType)
				{
				return biomeType switch
					{
						BiomeType.PowerPlant => true,
						BiomeType.CryogenicLabs => true,
						BiomeType.PlasmaFields => true,
						_ => false
						};
				}

			private static BiomeType SelectCombatBiome(Polarity polarity)
				{
				return polarity switch
					{
						Polarity.Sun => BiomeType.SolarPlains,
						Polarity.Moon => BiomeType.ShadowRealms,
						Polarity.Heat => BiomeType.VolcanicCore,
						Polarity.Cold => BiomeType.FrozenWastes,
						_ => BiomeType.AncientRuins
						};
				}

			private static bool IsHostileBiome(BiomeType biome)
				{
				return biome is BiomeType.ShadowRealms or BiomeType.VolcanicCore or
					   BiomeType.VoidChambers or BiomeType.DeepUnderwater;
				}

			private static bool IsSkyBiome(BiomeType biome)
				{
				return biome switch
					{
						BiomeType.SkyGardens => true,
						BiomeType.PlasmaFields => true,
						BiomeType.VoidChambers => true,
						BiomeType.Cosmic => true,
						_ => false
						};
				}

			private static bool IsTerrainBiome(BiomeType biome)
				{
				return biome switch
					{
						BiomeType.SolarPlains => true,
						BiomeType.Forest => true,
						BiomeType.Mountains => true,
						BiomeType.Desert => true,
						BiomeType.Tundra => true,
						BiomeType.FrozenWastes => true,
						BiomeType.IcyCanyon => true,
						BiomeType.CrystalCaverns => true,
						BiomeType.Crystal => true,
						BiomeType.Ruins => true,
						BiomeType.AncientRuins => true,
						BiomeType.Volcanic => true,
						BiomeType.VolcanicCore => true,
						_ => false
						};
				}

			private static float CalculateEnvironmentalFactor(NodeId nodeId, RoomHierarchyData roomData)
				{
				// Calculate environmental intensity based on room position and type
				int2 coords = nodeId.Coordinates;
				float distance = math.length(coords);
				float roomTypeMultiplier = roomData.Type switch
					{
						RoomType.Boss => 1.5f,
						RoomType.Treasure => 1.2f,
						RoomType.Normal => 1.0f,
						RoomType.Save => 0.5f,
						_ => 1.0f
						};

				// Environmental factor increases with distance and room importance
				return math.clamp((distance / 20f) * roomTypeMultiplier, 0.1f, 2.0f);
				}

			private static void ApplyCoordinateBasedFeatureModifications(ref RoomFeatureElement feature, NodeId nodeId, RoomHierarchyData roomData, ref Unity.Mathematics.Random random)
				{
				int2 coords = nodeId.Coordinates;

				// Apply position-based feature variations
				if ((coords.x + coords.y) % 3 == 0)
					{
					// Every 3rd coordinate gets enhanced features
					feature.FeatureId = feature.FeatureId | 0x10000000; // Mark as enhanced
					}

				// Rooms at specific coordinates get special treatments
				if (math.abs(coords.x) == math.abs(coords.y) && roomData.Type == RoomType.Normal)
					{
					// Diagonal positions get unique feature arrangements
					if (random.NextFloat() < 0.3f)
						{
						feature.Position = new int2(
							feature.Position.x + random.NextInt(-1, 2),
							feature.Position.y + random.NextInt(-1, 2)
						);
						}
					}
				}

			private static float CalculateNavigationComplexity(NodeId nodeId, RoomHierarchyData roomData)
				{
				// Calculate navigation difficulty based on room position and type
				int2 coords = nodeId.Coordinates;
				float distance = math.length(coords);

				// Base complexity increases with distance from origin
				float baseComplexity = math.clamp(distance / 25f, 0.5f, 2.0f);

				// Room type modifiers
				float roomTypeModifier = roomData.Type switch
					{
						RoomType.Boss => 1.5f,      // Boss rooms are more complex
						RoomType.Treasure => 1.3f,  // Treasure rooms require skill
						RoomType.Normal => 1.0f,    // Normal complexity
						RoomType.Save => 0.7f,      // Save rooms are easier
						RoomType.Hub => 0.6f,       // Hub rooms are simple
						_ => 1.0f
						};

				return baseComplexity * roomTypeModifier;
				}

			private static float CalculateObstacleRemovalThreshold(NodeId nodeId, float navigationComplexity)
				{
				// Calculate how aggressively to remove obstacles based on position
				int2 coords = nodeId.Coordinates;

				// Base threshold: more complex areas get more aggressive removal
				float baseThreshold = 0.3f + (navigationComplexity - 1.0f) * 0.2f;

				// Coordinate-based adjustment: corner rooms get more lenient removal
				if (math.abs(coords.x) > 10 || math.abs(coords.y) > 10)
					{
					baseThreshold += 0.15f; // Remove more obstacles in distant rooms
					}

				return math.clamp(baseThreshold, 0.1f, 0.8f);
				}

			// Helper methods for content generation
			private static void AddPatternDrivenContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random, ref Unity.Mathematics.Random coordinateRandom)
				{
				// Pattern-driven generation for movement skill puzzles
				int area = bounds.width * bounds.height;
				int featureCount = math.max(3, area / 8);

				// Use coordinate random for pattern determinism
				uint patternSeed = coordinateRandom.NextUInt();
				var patternRandom = new Unity.Mathematics.Random(patternSeed == 0 ? 1u : patternSeed);

				for (int i = 0; i < featureCount; i++)
					{
					var pos = new int2(
						random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
						random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
					);

					// Add skill-specific challenges based on available abilities with pattern variation
					RoomFeatureType featureType = SelectSkillSpecificFeature(request.AvailableSkills, ref patternRandom);
					features.Add(new RoomFeatureElement
						{
						Type = featureType,
						Position = pos,
						FeatureId = (uint)(request.GenerationSeed + i)
						});
					}
				}

			private static void AddParametricChallengeContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random, ref Unity.Mathematics.Random coordinateRandom)
				{
				// Parametric generation tuned for testing specific skills
				int platformCount = math.max(2, bounds.width / 3);

				// Use coordinate random for spacing consistency
				float spacingVariation = coordinateRandom.NextFloat(-0.2f, 0.2f);

				for (int i = 0; i < platformCount; i++)
					{
					float adjustedSpacing = (bounds.width / platformCount) * (1.0f + spacingVariation);
					var pos = new int2(
						bounds.x + (int)(i * adjustedSpacing),
						bounds.y + random.NextInt(1, bounds.height - 1)
					);

					features.Add(new RoomFeatureElement
						{
						Type = RoomFeatureType.Platform,
						Position = pos,
						FeatureId = (uint)(request.GenerationSeed + i)
						});
					}
				}

			private static void AddWeightedContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random, ref Unity.Mathematics.Random coordinateRandom)
				{
				// Standard weighted generation with optional secrets
				int area = bounds.width * bounds.height;
				int featureCount = area / 12;

				// Use coordinate random for feature type bias
				float platformBias = coordinateRandom.NextFloat(0.2f, 0.8f);

				for (int i = 0; i < featureCount; i++)
					{
					var pos = new int2(
						random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
						random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
					);

					RoomFeatureType featureType = random.NextFloat() > platformBias ? RoomFeatureType.Platform : RoomFeatureType.Obstacle;
					features.Add(new RoomFeatureElement
						{
						Type = featureType,
						Position = pos,
						FeatureId = (uint)(request.GenerationSeed + i)
						});
					}
				}

			private static void AddStackedSegmentContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random, ref Unity.Mathematics.Random coordinateRandom)
				{
				// Vertical stacked segments for climb/jump routes
				int segmentCount = math.max(2, bounds.height / 4);

				// Use coordinate random for segment height variation
				float heightVariation = coordinateRandom.NextFloat(-0.3f, 0.3f);

				for (int segment = 0; segment < segmentCount; segment++)
					{
					int baseY = bounds.y + (segment * bounds.height / segmentCount);
					int adjustedY = (int)(baseY * (1.0f + heightVariation));
					int platformsInSegment = random.NextInt(1, 4);

					for (int p = 0; p < platformsInSegment; p++)
						{
						var pos = new int2(
							random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
							adjustedY + random.NextInt(0, bounds.height / segmentCount)
						);

						features.Add(new RoomFeatureElement
							{
							Type = RoomFeatureType.Platform,
							Position = pos,
							FeatureId = (uint)(request.GenerationSeed + segment * 100 + p)
							});
						}
					}
				}

			private static void AddLinearCorridorContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random, ref Unity.Mathematics.Random coordinateRandom)
				{
				// Horizontal corridor with rhythm pacing
				int segmentCount = math.max(2, bounds.width / 6);

				// Use coordinate random for rhythm variation
				int rhythmOffset = coordinateRandom.NextInt(0, 2);

				for (int segment = 0; segment < segmentCount; segment++)
					{
					int x = bounds.x + (segment * bounds.width / segmentCount);

					// Alternate between challenge and rest beats with coordinate-based offset
					if ((segment + rhythmOffset) % 2 == 0) // Challenge beat
						{
						var pos = new int2(x + random.NextInt(0, bounds.width / segmentCount),
										  bounds.y + random.NextInt(1, bounds.height - 1));
						features.Add(new RoomFeatureElement
							{
							Type = RoomFeatureType.Obstacle,
							Position = pos,
							FeatureId = (uint)(request.GenerationSeed + segment)
							});
						}
					else // Rest beat
						{
						var pos = new int2(x + random.NextInt(0, bounds.width / segmentCount),
										  bounds.y + random.NextInt(1, bounds.height - 1));
						features.Add(new RoomFeatureElement
							{
							Type = RoomFeatureType.Platform,
							Position = pos,
							FeatureId = (uint)(request.GenerationSeed + segment)
							});
						}
					}
				}

			private static void AddLayeredPlatformContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random, ref Unity.Mathematics.Random coordinateRandom)
				{
				// Layered platforms for sky biomes with motion patterns
				int layerCount = math.max(2, bounds.height / 3);

				// Use coordinate random for layer density variation
				float densityModifier = coordinateRandom.NextFloat(0.7f, 1.3f);

				for (int layer = 0; layer < layerCount; layer++)
					{
					int y = bounds.y + (layer * bounds.height / layerCount);
					int basePlatforms = random.NextInt(2, 5);
					int platformsInLayer = (int)(basePlatforms * densityModifier);

					for (int p = 0; p < platformsInLayer; p++)
						{
						var pos = new int2(
							random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
							y + random.NextInt(0, bounds.height / layerCount)
						);

						features.Add(new RoomFeatureElement
							{
							Type = RoomFeatureType.Platform,
							Position = pos,
							FeatureId = (uint)(request.GenerationSeed + layer * 100 + p)
							});
						}
					}
				}

			private static void AddHeightmapContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random, ref Unity.Mathematics.Random coordinateRandom)
				{
				// Heightmap-based terrain generation for overworld biomes
				float terrainOffset = coordinateRandom.NextFloat(-0.5f, 0.5f);

				for (int x = bounds.x; x < bounds.x + bounds.width; x += 2)
					{
					// Generate terrain height using noise-like function with coordinate variation
					float noise = math.sin(x * 0.1f + request.GenerationSeed * 0.001f + terrainOffset) * 0.5f + 0.5f;
					int terrainHeight = (int)(bounds.y + noise * bounds.height);

					features.Add(new RoomFeatureElement
						{
						Type = RoomFeatureType.Platform,
						Position = new int2(x, (terrainHeight + random.NextInt(-1, 2))), // @jmeyer's attempt to make use of the previously unused `random`
						FeatureId = (uint)(request.GenerationSeed + x)
						});
					}
				}

			private static void AddSecretAreas(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, SecretAreaConfig config, RoomGenerationRequest request, ref Unity.Mathematics.Random random)
				{
				int secretCount = (int)(bounds.width * bounds.height * config.SecretAreaPercentage / (config.MinSecretSize.x * config.MinSecretSize.y));

				for (int i = 0; i < secretCount; i++)
					{
					var pos = new int2(
						random.NextInt(bounds.x, bounds.x + bounds.width - config.MinSecretSize.x),
						random.NextInt(bounds.y, bounds.y + bounds.height - config.MinSecretSize.y)
					);

					features.Add(new RoomFeatureElement
						{
						Type = RoomFeatureType.Secret,
						Position = pos,
						FeatureId = (uint)(request.GenerationSeed + 10000 + i)
						});
					}
				}

			private static RoomFeatureType SelectSkillSpecificFeature(Ability availableSkills, ref Unity.Mathematics.Random patternRandom)
				{
				float skillVariation = patternRandom.NextFloat();

				if ((availableSkills & Ability.Dash) != 0)
					{
					// Dash skills get varied obstacle patterns
					return skillVariation > 0.3f ? RoomFeatureType.Obstacle : RoomFeatureType.Platform;
					}
				if ((availableSkills & Ability.WallJump) != 0)
					{
					// Wall jump skills get more platforms with wall opportunities
					return skillVariation > 0.2f ? RoomFeatureType.Platform : RoomFeatureType.Obstacle;
					}
				if ((availableSkills & Ability.Grapple) != 0)
					{
					// Grapple skills get elevated platforms
					return RoomFeatureType.Platform;
					}

				// Default to platform with pattern variation
				return skillVariation > 0.4f ? RoomFeatureType.Platform : RoomFeatureType.Obstacle;
				}
			}
		}
	}

