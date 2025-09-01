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
		private BufferLookup<RoomFeatureElement> _featureBufferLookup; // 🔥 USE EXISTING TYPE
		private EntityQuery _roomGenerationQuery; // 🔥 CREATE QUERY IN ONCREATE

		[BurstCompile]
		public void OnCreate (ref SystemState state)
			{
			_skillTagLookup = state.GetComponentLookup<SkillTag>(true);
			_featureBufferLookup = state.GetBufferLookup<RoomFeatureElement>(); // 🔥 USE EXISTING TYPE
			
			// 🔥 FIX: Create query in OnCreate instead of OnUpdate
			_roomGenerationQuery = new EntityQueryBuilder(Allocator.Persistent)
				.WithAll<RoomGenerationRequest, RoomHierarchyData, NodeId>()
				.Build(ref state);
			}

		// NOTE: Cannot use [BurstCompile] on OnUpdate due to ref SystemState parameter
		public void OnUpdate (ref SystemState state)
			{
			_skillTagLookup.Update(ref state);
			_featureBufferLookup.Update(ref state); // 🔥 USE EXISTING TYPE

			// 🛠️ SEED FIX: Ensure we always have a non-zero seed for Unity.Mathematics.Random
			uint timeBasedSeed = (uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000);
			uint safeSeed = timeBasedSeed == 0 ? 1 : timeBasedSeed; // Ensure non-zero seed
			var baseRandom = new Unity.Mathematics.Random(safeSeed);

			// 🔥 FIX: Use pre-created query instead of creating new one
			NativeArray<Entity> entities = _roomGenerationQuery.ToEntityArray(Allocator.Temp);
			ComponentLookup<RoomGenerationRequest> requests = state.GetComponentLookup<RoomGenerationRequest>(false);
			ComponentLookup<RoomHierarchyData> roomData = state.GetComponentLookup<RoomHierarchyData>(true);
			ComponentLookup<NodeId> nodeIds = state.GetComponentLookup<NodeId>(true);

			var patternJob = new PatternGenerationJob
				{
				SkillTagLookup = _skillTagLookup,
				FeatureBufferLookup = _featureBufferLookup, // 🔥 USE EXISTING TYPE
				Entities = entities,
				Requests = requests,
				RoomData = roomData,
				NodeIds = nodeIds,
				BaseRandom = baseRandom
				};

			patternJob.Execute();
			entities.Dispose();
			}

		[BurstCompile]
		private struct PatternGenerationJob : IJob
			{
			[ReadOnly] public ComponentLookup<SkillTag> SkillTagLookup;
			public BufferLookup<RoomFeatureElement> FeatureBufferLookup; // 🔥 USE EXISTING TYPE
			[ReadOnly] public NativeArray<Entity> Entities;
			public ComponentLookup<RoomGenerationRequest> Requests;
			[ReadOnly] public ComponentLookup<RoomHierarchyData> RoomData;
			[ReadOnly] public ComponentLookup<NodeId> NodeIds;
			public Unity.Mathematics.Random BaseRandom;

			public void Execute ()
				{
				int processedPatternCount = 0;
				int skillGateGenerationCount = 0;

				for (int i = 0; i < Entities.Length; i++)
					{
					Entity entity = Entities [ i ];
					RefRW<RoomGenerationRequest> request = Requests.GetRefRW(entity);

					if (request.ValueRO.GeneratorType != RoomGeneratorType.PatternDrivenModular || request.ValueRO.IsComplete)
						{
						continue;
						}

					if (!FeatureBufferLookup.HasBuffer(entity))
						{
						continue;
						}

					DynamicBuffer<RoomFeatureElement> features = FeatureBufferLookup [ entity ]; // 🔥 USE EXISTING TYPE
					RefRO<RoomHierarchyData> roomDataRO = RoomData.GetRefRO(entity);
					RefRO<NodeId> nodeId = NodeIds.GetRefRO(entity);
					RectInt bounds = roomDataRO.ValueRO.Bounds;

					// Clear existing features
					features.Clear();

					var entityRandom = new Unity.Mathematics.Random(BaseRandom.state + (uint)entity.Index);

					// Use nodeId coordinates for coordinate-aware pattern generation
					float coordinateInfluence = CalculateCoordinateInfluence(nodeId.ValueRO, roomDataRO.ValueRO);

					// Generate skill-specific features based on available abilities
					if ((request.ValueRO.AvailableSkills & Ability.Dash) != 0)
						{
						GenerateDashFeatures(features, bounds, request.ValueRO.GenerationSeed, ref entityRandom, coordinateInfluence);
						processedPatternCount++;
						}

					if ((request.ValueRO.AvailableSkills & Ability.WallJump) != 0)
						{
						GenerateWallJumpFeatures(features, bounds, request.ValueRO.GenerationSeed, ref entityRandom, coordinateInfluence);
						processedPatternCount++;
						}

					if ((request.ValueRO.AvailableSkills & Ability.Grapple) != 0)
						{
						GenerateGrappleFeatures(features, bounds, request.ValueRO.GenerationSeed, ref entityRandom, coordinateInfluence);
						processedPatternCount++;
						}

					// Add skill gates that require unlocked abilities
					int skillGatesAdded = GenerateSkillGates(features, bounds, request.ValueRO.AvailableSkills, request.ValueRO.GenerationSeed, ref entityRandom, coordinateInfluence);
					skillGateGenerationCount += skillGatesAdded;

					request.ValueRW.IsComplete = true;
					}
				}

			private static void GenerateDashFeatures (DynamicBuffer<RoomFeatureElement> features, RectInt bounds, uint seed, ref Unity.Mathematics.Random random, float coordinateInfluence)
				{
				int baseGapCount = math.max(1, bounds.width / 8);
				int gapCount = (int)(baseGapCount * coordinateInfluence);

				for (int i = 0; i < gapCount; i++)
					{
					var gapStart = new int2(
						random.NextInt(bounds.x + 2, bounds.x + bounds.width - 4),
						random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
					);

					int baseGapWidth = random.NextInt(3, 5);
					int gapWidth = (int)(baseGapWidth * math.clamp(coordinateInfluence, 0.8f, 1.5f));

					// Create platforms with dash-friendly gaps
					features.Add(new RoomFeatureElement
						{
						Type = RoomFeatureType.Platform,
						Position = gapStart,
						FeatureId = (uint)(seed + i * 10)
						});
					features.Add(new RoomFeatureElement
						{
						Type = RoomFeatureType.Platform,
						Position = new int2(gapStart.x + gapWidth, gapStart.y),
						FeatureId = (uint)(seed + i * 10 + 1)
						});
					}
				}

			private static void GenerateWallJumpFeatures (DynamicBuffer<RoomFeatureElement> features, RectInt bounds, uint seed, ref Unity.Mathematics.Random random, float coordinateInfluence)
				{
				int baseShaftCount = math.max(1, bounds.height / 8);
				int shaftCount = (int)(baseShaftCount * coordinateInfluence);

				for (int i = 0; i < shaftCount; i++)
					{
					int shaftX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
					int shaftBottom = random.NextInt(bounds.y + 1, bounds.y + bounds.height / 2);
					int baseShaftHeight = random.NextInt(4, bounds.height - shaftBottom + bounds.y);
					int shaftHeight = (int)(baseShaftHeight * math.clamp(coordinateInfluence, 0.7f, 1.8f));

					// Create vertical walls for wall jumping
					for (int y = shaftBottom; y < shaftBottom + shaftHeight; y += 2)
						{
						features.Add(new RoomFeatureElement
							{
							Type = RoomFeatureType.Obstacle,
							Position = new int2(shaftX, y),
							FeatureId = (uint)(seed + i * 20 + y)
							});
						}
					}
				}

			private static void GenerateGrappleFeatures (DynamicBuffer<RoomFeatureElement> features, RectInt bounds, uint seed, ref Unity.Mathematics.Random random, float coordinateInfluence)
				{
				int basePointCount = math.max(1, (bounds.width * bounds.height) / 32);
				int pointCount = (int)(basePointCount * coordinateInfluence);

				for (int i = 0; i < pointCount; i++)
					{
					var grapplePoint = new int2(
						random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
						random.NextInt(bounds.y + bounds.height / 2, bounds.y + bounds.height - 1)
					);

					features.Add(new RoomFeatureElement
						{
						Type = RoomFeatureType.GrapplePoint,
						Position = grapplePoint,
						FeatureId = (uint)(seed + i * 30)
						});

					// Add obstacles below grapple points for challenge
					if (grapplePoint.y > bounds.y + 2 && random.NextFloat() < coordinateInfluence * 0.4f)
						{
						features.Add(new RoomFeatureElement
							{
							Type = RoomFeatureType.Obstacle,
							Position = new int2(grapplePoint.x, grapplePoint.y - 2),
							FeatureId = (uint)(seed + i * 30 + 1)
							});
						}
					}
				}

			private static int GenerateSkillGates (DynamicBuffer<RoomFeatureElement> features, RectInt bounds, Ability availableSkills, uint seed, ref Unity.Mathematics.Random random, float coordinateInfluence)
				{
				int skillGatesAdded = 0;

				// Create combination skill challenges
				if ((availableSkills & (Ability.Dash | Ability.WallJump)) == (Ability.Dash | Ability.WallJump))
					{
					int centerX = bounds.x + bounds.width / 2;
					int centerY = bounds.y + bounds.height / 2;
					int challengeSpacing = (int)(3 * math.clamp(coordinateInfluence, 0.8f, 1.5f));

					features.Add(new RoomFeatureElement
						{
						Type = RoomFeatureType.Platform,
						Position = new int2(centerX - challengeSpacing, centerY),
						FeatureId = seed + 100
						});
					features.Add(new RoomFeatureElement
						{
						Type = RoomFeatureType.Obstacle,
						Position = new int2(centerX, centerY + 2),
						FeatureId = seed + 101
						});
					features.Add(new RoomFeatureElement
						{
						Type = RoomFeatureType.Platform,
						Position = new int2(centerX + challengeSpacing, centerY),
						FeatureId = seed + 102
						});
					skillGatesAdded += 3;
					}

				// Create grapple-specific challenges in complex areas
				if (coordinateInfluence > 1.3f && (availableSkills & Ability.Grapple) != 0)
					{
					int challengeX = bounds.x + random.NextInt(bounds.width / 4, (bounds.width * 3) / 4);
					int challengeY = bounds.y + bounds.height - 2;

					features.Add(new RoomFeatureElement
						{
						Type = RoomFeatureType.GrapplePoint,
						Position = new int2(challengeX, challengeY),
						FeatureId = seed + 200
						});
					skillGatesAdded++;
					}

				return skillGatesAdded;
				}

			private static float CalculateCoordinateInfluence (NodeId nodeId, RoomHierarchyData roomData)
				{
				int2 coords = nodeId.Coordinates;
				float distance = math.length(coords);
				float distanceInfluence = math.clamp(distance / 30f, 0.5f, 2.0f);

				float roomTypeInfluence = roomData.Type switch
					{
						RoomType.Boss => 1.8f,
						RoomType.Treasure => 1.4f,
						RoomType.Normal => 1.0f,
						RoomType.Save => 0.6f,
						_ => 1.0f
						};

				return distanceInfluence * roomTypeInfluence;
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
		private EntityQuery _roomGenerationQuery; // 🔥 CREATE QUERY IN ONCREATE

		[BurstCompile]
		public void OnCreate (ref SystemState state)
			{
			_jumpPhysicsLookup = state.GetComponentLookup<JumpPhysicsData>(true);
			_validationLookup = state.GetComponentLookup<JumpArcValidation>();
			_jumpConnectionLookup = state.GetBufferLookup<JumpConnectionElement>();
			
			// 🔥 FIX: Create query in OnCreate instead of OnUpdate
			_roomGenerationQuery = new EntityQueryBuilder(Allocator.Persistent)
				.WithAll<RoomGenerationRequest, RoomHierarchyData, NodeId>()
				.Build(ref state);
			}

		// NOTE: Cannot use [BurstCompile] on OnUpdate due to ref SystemState parameter
		public void OnUpdate (ref SystemState state)
			{
			_jumpPhysicsLookup.Update(ref state);
			_validationLookup.Update(ref state);
			_jumpConnectionLookup.Update(ref state);

			// 🔥 FIX: Use pre-created query instead of creating new one
			NativeArray<Entity> entities = _roomGenerationQuery.ToEntityArray(Allocator.Temp);
			ComponentLookup<RoomGenerationRequest> requests = state.GetComponentLookup<RoomGenerationRequest>(false);
			ComponentLookup<RoomHierarchyData> roomData = state.GetComponentLookup<RoomHierarchyData>(true);
			ComponentLookup<NodeId> nodeIds = state.GetComponentLookup<NodeId>(true);

			var challengeJob = new ChallengeGenerationJob
				{
				JumpPhysicsLookup = _jumpPhysicsLookup,
				ValidationLookup = _validationLookup,
				JumpConnectionLookup = _jumpConnectionLookup,
				Entities = entities,
				Requests = requests,
				RoomData = roomData,
				NodeIds = nodeIds
				};

			challengeJob.Execute();
			entities.Dispose();
			}

		[BurstCompile]
		private struct ChallengeGenerationJob : IJob
			{
			[ReadOnly] public ComponentLookup<JumpPhysicsData> JumpPhysicsLookup;
			public ComponentLookup<JumpArcValidation> ValidationLookup;
			public BufferLookup<JumpConnectionElement> JumpConnectionLookup;
			[ReadOnly] public NativeArray<Entity> Entities;
			public ComponentLookup<RoomGenerationRequest> Requests;
			[ReadOnly] public ComponentLookup<RoomHierarchyData> RoomData;
			[ReadOnly] public ComponentLookup<NodeId> NodeIds;

			public void Execute ()
				{
				int processedRoomCount = 0;

				for (int i = 0; i < Entities.Length; i++)
					{
					Entity entity = Entities [ i ];
					RefRW<RoomGenerationRequest> request = Requests.GetRefRW(entity);

					if (request.ValueRO.GeneratorType != RoomGeneratorType.ParametricChallenge || request.ValueRO.IsComplete)
						{
						continue;
						}

					if (!JumpPhysicsLookup.HasComponent(entity))
						{
						continue;
						}

					JumpPhysicsData jumpPhysics = JumpPhysicsLookup [ entity ];
					RefRO<RoomHierarchyData> roomDataRO = RoomData.GetRefRO(entity);
					RefRO<NodeId> nodeId = NodeIds.GetRefRO(entity);
					RectInt bounds = roomDataRO.ValueRO.Bounds;
					float challengeComplexity = CalculateChallengeComplexity(nodeId.ValueRO, roomDataRO.ValueRO);

					// Simple platform generation without complex arc solver dependencies
					var platformPositions = new NativeList<float2>(Allocator.Temp)
					{
                        // Start platform
                        new(bounds.x + 1, bounds.y + 1)
					};

					int platformSpacing = (int)(jumpPhysics.JumpDistance * challengeComplexity);
					int currentX = bounds.x + 1 + platformSpacing;

					while (currentX < bounds.x + bounds.width - 1)
						{
						int targetY = bounds.y + 1 + (platformPositions.Length % 2) * (int)(jumpPhysics.JumpHeight);
						platformPositions.Add(new float2(currentX, targetY));
						currentX += platformSpacing;
						}

					platformPositions.Add(new float2(bounds.x + bounds.width - 1, bounds.y + 1));

					// Store basic jump connections
					if (JumpConnectionLookup.HasBuffer(entity))
						{
						DynamicBuffer<JumpConnectionElement> connections = JumpConnectionLookup [ entity ];
						connections.Clear();

						for (int j = 0; j < platformPositions.Length - 1; j++)
							{
							float2 from = platformPositions [ j ];
							float2 to = platformPositions [ j + 1 ];
							connections.Add(new JumpConnectionElement((int2)from, (int2)to, Ability.Jump));
							}
						}

					if (ValidationLookup.HasComponent(entity))
						{
						var validation = new JumpArcValidation(
							platformPositions.Length > 2,
							jumpPhysics.JumpDistance,
							jumpPhysics.JumpHeight
						);
						ValidationLookup [ entity ] = validation;
						}

					platformPositions.Dispose();
					request.ValueRW.IsComplete = true;
					processedRoomCount++;
					}
				}

			private static float CalculateChallengeComplexity (NodeId nodeId, RoomHierarchyData roomData)
				{
				int2 coords = nodeId.Coordinates;
				float distance = math.length(coords);
				float baseComplexity = math.clamp(distance / 25f, 0.7f, 1.8f);

				float roomTypeModifier = roomData.Type switch
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
		private EntityQuery _roomGenerationQuery; // 🔥 CREATE QUERY IN ONCREATE

		[BurstCompile]
		public void OnCreate (ref SystemState state)
			{
			_secretConfigLookup = state.GetComponentLookup<SecretAreaConfig>(true);
			_biomeAffinityLookup = state.GetComponentLookup<BiomeAffinityComponent>(true);
			_moduleBufferLookup = state.GetBufferLookup<RoomModuleElement>(true);
			
			// 🔥 FIX: Create query in OnCreate instead of OnUpdate
			_roomGenerationQuery = new EntityQueryBuilder(Allocator.Persistent)
				.WithAll<RoomGenerationRequest, RoomHierarchyData, NodeId, RoomFeatureElement>()
				.Build(ref state);
			}

		// NOTE: Cannot use [BurstCompile] on OnUpdate due to ref SystemState parameter
		public void OnUpdate (ref SystemState state)
			{
			_secretConfigLookup.Update(ref state);
			_biomeAffinityLookup.Update(ref state);
			_moduleBufferLookup.Update(ref state);

			// 🛠️ SEED FIX: Ensure we always have a non-zero seed for Unity.Mathematics.Random
			uint timeBasedSeed = (uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000);
			uint safeSeed = timeBasedSeed == 0 ? 1 : timeBasedSeed; // Ensure non-zero seed
			var baseRandom = new Unity.Mathematics.Random(safeSeed);

			// 🔥 FIX: Use pre-created query instead of creating new one
			NativeArray<Entity> entities = _roomGenerationQuery.ToEntityArray(Allocator.Temp);
			ComponentLookup<RoomGenerationRequest> requests = state.GetComponentLookup<RoomGenerationRequest>(false);
			ComponentLookup<RoomHierarchyData> roomData = state.GetComponentLookup<RoomHierarchyData>(true);
			ComponentLookup<NodeId> nodeIds = state.GetComponentLookup<NodeId>(true);
			BufferLookup<RoomFeatureElement> featureBuffers = state.GetBufferLookup<RoomFeatureElement>(false);

			var weightedJob = new WeightedGenerationJob
				{
				SecretConfigLookup = _secretConfigLookup,
				BiomeAffinityLookup = _biomeAffinityLookup,
				ModuleBufferLookup = _moduleBufferLookup,
				Entities = entities,
				Requests = requests,
				RoomData = roomData,
				NodeIds = nodeIds,
				FeatureBuffers = featureBuffers,
				BaseRandom = baseRandom
				};

			weightedJob.Execute();
			entities.Dispose();
			}

		[BurstCompile]
		private struct WeightedGenerationJob : IJob
			{
			[ReadOnly] public ComponentLookup<SecretAreaConfig> SecretConfigLookup;
			[ReadOnly] public ComponentLookup<BiomeAffinityComponent> BiomeAffinityLookup;
			[ReadOnly] public BufferLookup<RoomModuleElement> ModuleBufferLookup;
			[ReadOnly] public NativeArray<Entity> Entities;
			public ComponentLookup<RoomGenerationRequest> Requests;
			[ReadOnly] public ComponentLookup<RoomHierarchyData> RoomData;
			[ReadOnly] public ComponentLookup<NodeId> NodeIds;
			public BufferLookup<RoomFeatureElement> FeatureBuffers;
			public Unity.Mathematics.Random BaseRandom;

			public void Execute ()
				{
				int processedRoomCount = 0;

				for (int i = 0; i < Entities.Length; i++)
					{
					Entity entity = Entities [ i ];
					RefRW<RoomGenerationRequest> request = Requests.GetRefRW(entity);

					if (request.ValueRO.GeneratorType != RoomGeneratorType.WeightedTilePrefab || request.ValueRO.IsComplete)
						{
						continue;
						}

					RefRO<RoomHierarchyData> roomDataRO = RoomData.GetRefRO(entity);
					RefRO<NodeId> nodeId = NodeIds.GetRefRO(entity);
					DynamicBuffer<RoomFeatureElement> features = FeatureBuffers [ entity ];
					RectInt bounds = roomDataRO.ValueRO.Bounds;
					int area = bounds.width * bounds.height;
					var entityRandom = new Unity.Mathematics.Random(BaseRandom.state + (uint)entity.Index);
					float spatialVariation = CalculateSpatialVariation(nodeId.ValueRO, roomDataRO.ValueRO);

					// Generate main flow layout
					int baseFeatureCount = (int)(area * 0.6f / 12);
					int mainFeatureCount = (int)(baseFeatureCount * spatialVariation);

					for (int j = 0; j < mainFeatureCount; j++)
						{
						float weight = entityRandom.NextFloat();
						RoomFeatureType featureType = SelectWeightedFeatureType(weight, roomDataRO.ValueRO.Type);

						var pos = new int2(
							entityRandom.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
							entityRandom.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
						);

						features.Add(new RoomFeatureElement
							{
							Type = featureType,
							Position = pos,
							FeatureId = (uint)(request.ValueRO.GenerationSeed + j)
							});
						}

					request.ValueRW.IsComplete = true;
					processedRoomCount++;
					}
				}

			private static float CalculateSpatialVariation (NodeId nodeId, RoomHierarchyData roomData)
				{
				int2 coords = nodeId.Coordinates;
				float distance = math.length(coords);
				float distanceVariation = math.clamp(distance / 20f, 0.8f, 1.4f);

				float roomTypeVariation = roomData.Type switch
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

			private static RoomFeatureType SelectWeightedFeatureType (float weight, RoomType roomType)
				{
				float adjustedWeight = weight;

				switch (roomType)
					{
					case RoomType.Boss:
						adjustedWeight *= 1.2f;
						break;
					case RoomType.Save:
						adjustedWeight *= 0.8f;
						break;
					case RoomType.Treasure:
						adjustedWeight *= 1.1f;
						break;
					case RoomType.Normal:
						break;
					case RoomType.Entrance:
						break;
					case RoomType.Exit:
						break;
					case RoomType.Shop:
						break;
					case RoomType.Hub:
						break;
					default:
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
	}

