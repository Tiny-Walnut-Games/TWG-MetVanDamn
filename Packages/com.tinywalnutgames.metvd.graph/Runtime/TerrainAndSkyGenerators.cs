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
	/// Stacked Segment Generator
	/// For vertical layout rooms - builds rooms in vertical slices
	/// Ensures climb/jump routes are coherent for towers, shafts, elevator-style challenges
	/// </summary>
	[BurstCompile]
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	[UpdateAfter(typeof(RoomGenerationPipelineSystem))]
	public partial struct StackedSegmentGenerator : ISystem
		{
		private ComponentLookup<JumpPhysicsData> _jumpPhysicsLookup;
		private BufferLookup<RoomFeatureElement> _featureBufferLookup;
		private EntityQuery _roomGenerationQuery; // 🔥 CREATE QUERY IN ONCREATE

		[BurstCompile]
		public void OnCreate (ref SystemState state)
			{
			_jumpPhysicsLookup = state.GetComponentLookup<JumpPhysicsData>(true);
			_featureBufferLookup = state.GetBufferLookup<RoomFeatureElement>();
			
			// 🔥 FIX: Create query in OnCreate instead of OnUpdate
			_roomGenerationQuery = new EntityQueryBuilder(Allocator.Persistent)
				.WithAll<RoomGenerationRequest, RoomHierarchyData, NodeId>()
				.Build(ref state);
			}

		// NOTE: Cannot use [BurstCompile] on OnUpdate due to ref SystemState parameter
		public void OnUpdate (ref SystemState state)
			{
			_jumpPhysicsLookup.Update(ref state);
			_featureBufferLookup.Update(ref state);

			// 🛠️ SEED FIX: Ensure we always have a non-zero seed for Unity.Mathematics.Random
			uint timeBasedSeed = (uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000);
			uint safeSeed = timeBasedSeed == 0 ? 1 : timeBasedSeed; // Ensure non-zero seed
			var random = new Unity.Mathematics.Random(safeSeed);

			// Gather entities for job processing
			//EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
			//	.WithAll<RoomGenerationRequest, RoomHierarchyData, NodeId>()
			//	.Build(ref state);

			//NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
			NativeArray<Entity> entities = _roomGenerationQuery.ToEntityArray(Allocator.Temp); // Use pre-created query
			ComponentLookup<RoomGenerationRequest> requests = state.GetComponentLookup<RoomGenerationRequest>(false);
			ComponentLookup<RoomHierarchyData> roomData = state.GetComponentLookup<RoomHierarchyData>(true);
			ComponentLookup<NodeId> nodeIds = state.GetComponentLookup<NodeId>(true);

			var stackedJob = new StackedGenerationJob
				{
				JumpPhysicsLookup = _jumpPhysicsLookup,
				FeatureBufferLookup = _featureBufferLookup,
				Entities = entities,
				Requests = requests,
				RoomData = roomData,
				NodeIds = nodeIds,
				BaseRandom = random
				};

			stackedJob.Execute();
			entities.Dispose();
			}

		[BurstCompile]
		private struct StackedGenerationJob : IJob
			{
			[ReadOnly] public ComponentLookup<JumpPhysicsData> JumpPhysicsLookup;
			public BufferLookup<RoomFeatureElement> FeatureBufferLookup;
			[ReadOnly] public NativeArray<Entity> Entities;
			public ComponentLookup<RoomGenerationRequest> Requests;
			[ReadOnly] public ComponentLookup<RoomHierarchyData> RoomData;
			[ReadOnly] public ComponentLookup<NodeId> NodeIds;
			public Unity.Mathematics.Random BaseRandom;

			public void Execute ()
				{
				for (int i = 0; i < Entities.Length; i++)
					{
					Entity entity = Entities [ i ];
					RefRW<RoomGenerationRequest> request = Requests.GetRefRW(entity);

					if (request.ValueRO.GeneratorType != RoomGeneratorType.StackedSegment || request.ValueRO.IsComplete)
						{
						continue;
						}

					if (!FeatureBufferLookup.HasBuffer(entity))
						{
						continue;
						}

					DynamicBuffer<RoomFeatureElement> features = FeatureBufferLookup [ entity ];
					RefRO<RoomHierarchyData> roomDataRO = RoomData.GetRefRO(entity);
					RefRO<NodeId> nodeId = NodeIds.GetRefRO(entity);
					RectInt bounds = roomDataRO.ValueRO.Bounds;
					features.Clear();

					var entityRandom = new Unity.Mathematics.Random(BaseRandom.state + (uint)entity.Index);

					int segmentCount = math.max(3, bounds.height / 4);
					int segmentHeight = bounds.height / segmentCount;

					float jumpHeight = 3.0f;
					if (JumpPhysicsLookup.HasComponent(entity))
						{
						jumpHeight = JumpPhysicsLookup [ entity ].JumpHeight;
						}

					// Use nodeId coordinates to influence segment placement and connectivity patterns
					float coordinateComplexity = CalculateCoordinateBasedComplexity(nodeId.ValueRO);

					for (int segment = 0; segment < segmentCount; segment++)
						{
						int segmentY = bounds.y + (segment * segmentHeight);
						GenerateVerticalSegment(features, bounds, segmentY, segmentHeight, segment, jumpHeight, request.ValueRO.GenerationSeed, ref entityRandom, coordinateComplexity);
						}

					EnsureVerticalConnectivity(features, bounds, segmentCount, segmentHeight, jumpHeight);
					request.ValueRW.IsComplete = true;
					}
				}

			// Add meaningful coordinate-based complexity calculation
			private static float CalculateCoordinateBasedComplexity (NodeId nodeId)
				{
				int2 coords = nodeId.Coordinates;
				float distance = math.length(coords);
				// Distance from origin affects segment complexity (farther = more complex)
				float distanceComplexity = math.clamp(distance / 20f, 0.7f, 1.8f);
				// Coordinate parity adds variation
				float parityVariation = ((coords.x ^ coords.y) & 1) == 0 ? 1.1f : 0.9f;
				return distanceComplexity * parityVariation;
				}

			private static void GenerateVerticalSegment (DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
											   int segmentY, int segmentHeight, int segmentIndex, float jumpHeight, uint seed, ref Unity.Mathematics.Random random, float coordinateComplexity)
				{
				// Use coordinate complexity to influence platform count and arrangement
				int basePlatformCount = random.NextInt(1, 4);
				int platformCount = (int)(basePlatformCount * coordinateComplexity); // More complex areas get more platforms
				platformCount = math.max(1, platformCount); // Ensure at least one platform

				for (int p = 0; p < platformCount; p++)
					{
					int platformX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
					int platformY = segmentY + random.NextInt(0, segmentHeight - 1);

					// Ensure platforms support upward movement
					RoomFeatureType featureType = RoomFeatureType.Platform;

					// Add climb assists for tall segments - influenced by coordinate complexity
					if (segmentHeight > jumpHeight + 1)
						{
						if (p == 0 && coordinateComplexity > 1.2f) // Only add walls in more complex areas
							{
							features.Add(new RoomFeatureElement
								{
								Type = RoomFeatureType.Obstacle,
								Position = new int2(platformX + 1, platformY + 1),
								FeatureId = (uint)(seed + segmentIndex * 100 + p * 10 + 1)
								});
							}
						}

					features.Add(new RoomFeatureElement
						{
						Type = featureType,
						Position = new int2(platformX, platformY),
						FeatureId = (uint)(seed + segmentIndex * 100 + p * 10)
						});
					}

				// Add segment-specific challenges - frequency influenced by coordinate complexity
				float challengeThreshold = (segmentIndex % 3 == 0) ? 1.0f : 2.0f; // Every third segment base chance
				if (coordinateComplexity > challengeThreshold)
					{
					AddVerticalChallenge(features, bounds, segmentY, segmentHeight, ref random, seed);
					}
				}

			private static void AddVerticalChallenge (DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
										int segmentY, int segmentHeight, ref Unity.Mathematics.Random random, uint seed)
				{
				int challengeType = random.NextInt(0, 3);

				switch (challengeType)
					{
					case 0: // Moving obstacle
							{
							int obstacleX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
							int obstacleY = segmentY + segmentHeight / 2;
							features.Add(new RoomFeatureElement
								{
								Type = RoomFeatureType.Obstacle,
								Position = new int2(obstacleX, obstacleY),
								FeatureId = seed + 10000
								});
							}
						break;
					case 1: // Power-up placement requiring skill
							{
							int powerUpX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
							int powerUpY = segmentY + segmentHeight - 1;
							features.Add(new RoomFeatureElement
								{
								Type = RoomFeatureType.PowerUp,
								Position = new int2(powerUpX, powerUpY),
								FeatureId = seed + 20000
								});
							}
						break;
					case 2: // Switch/door mechanism
							{
							int switchX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
							int switchY = segmentY + 1;
							features.Add(new RoomFeatureElement
								{
							 Type = RoomFeatureType.Switch,
								Position = new int2(switchX, switchY),
								FeatureId = seed + 30000
								});
							}
						break;
					default:
						break;
					}
				}

			private static void EnsureVerticalConnectivity (DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
												  int segmentCount, int segmentHeight, float jumpHeight)
				{
				// Add connectivity platforms between segments if gaps are too large
				for (int segment = 0; segment < segmentCount - 1; segment++)
					{
					int currentSegmentTop = bounds.y + ((segment + 1) * segmentHeight);
					int nextSegmentBottom = bounds.y + (segment * segmentHeight);
					int gap = currentSegmentTop - nextSegmentBottom;

					if (gap > jumpHeight)
						{
						// Add intermediate platform
						int bridgeX = bounds.x + bounds.width / 2;
						int bridgeY = nextSegmentBottom + gap / 2;

						features.Add(new RoomFeatureElement
							{
							Type = RoomFeatureType.Platform,
							Position = new int2(bridgeX, bridgeY),
							FeatureId = (uint)(segment * 1000 + 999) // Special connectivity ID
							});
						}
					}
				}
			}
		}

	/// <summary>
	/// Linear/Branching Corridor Generator
	/// For horizontal layout rooms - focuses on pacing and rhythm
	/// Alternates challenge, rest, and secret beats for flow platforming
	/// </summary>
	[BurstCompile]
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	[UpdateAfter(typeof(StackedSegmentGenerator))]
	public partial struct LinearBranchingCorridorGenerator : ISystem
		{
		private BufferLookup<RoomFeatureElement> _featureBufferLookup;
		private ComponentLookup<SecretAreaConfig> _secretConfigLookup;
		private EntityQuery _roomGenerationQuery; // 🔥 ADD PRE-CREATED QUERY

		[BurstCompile]
		public void OnCreate (ref SystemState state)
			{
			_featureBufferLookup = state.GetBufferLookup<RoomFeatureElement>();
			_secretConfigLookup = state.GetComponentLookup<SecretAreaConfig>(true);
			
			// 🔥 FIX: Create query in OnCreate instead of OnUpdate
			_roomGenerationQuery = new EntityQueryBuilder(Allocator.Persistent)
				.WithAll<RoomGenerationRequest, RoomHierarchyData, NodeId>()
				.Build(ref state);
			}

		// NOTE: Cannot use [BurstCompile] on OnUpdate due to ref SystemState parameter
		public void OnUpdate (ref SystemState state)
			{
			_featureBufferLookup.Update(ref state);
			_secretConfigLookup.Update(ref state);

			// 🛠️ SEED FIX: Ensure we always have a non-zero seed for Unity.Mathematics.Random
			uint timeBasedSeed = (uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000);
			uint safeSeed = timeBasedSeed == 0 ? 1 : timeBasedSeed; // Ensure non-zero seed
			var random = new Unity.Mathematics.Random(safeSeed);

			// 🔥 FIX: Use pre-created query instead of creating new one
			NativeArray<Entity> entities = _roomGenerationQuery.ToEntityArray(Allocator.Temp);
			ComponentLookup<RoomGenerationRequest> requests = state.GetComponentLookup<RoomGenerationRequest>(false);
			ComponentLookup<RoomHierarchyData> roomData = state.GetComponentLookup<RoomHierarchyData>(true);
			ComponentLookup<NodeId> nodeIds = state.GetComponentLookup<NodeId>(true);

			var corridorJob = new CorridorGenerationJob
				{
				FeatureBufferLookup = _featureBufferLookup,
				SecretConfigLookup = _secretConfigLookup,
				Entities = entities,
				Requests = requests,
				RoomData = roomData,
				NodeIds = nodeIds,
				BaseRandom = random
				};

			corridorJob.Execute();
			entities.Dispose();
			}

		[BurstCompile]
		private struct CorridorGenerationJob : IJob
			{
			public BufferLookup<RoomFeatureElement> FeatureBufferLookup;
			[ReadOnly] public ComponentLookup<SecretAreaConfig> SecretConfigLookup;
			[ReadOnly] public NativeArray<Entity> Entities;
			public ComponentLookup<RoomGenerationRequest> Requests;
			[ReadOnly] public ComponentLookup<RoomHierarchyData> RoomData;
			[ReadOnly] public ComponentLookup<NodeId> NodeIds;
			public Unity.Mathematics.Random BaseRandom;

			public void Execute ()
				{
				for (int i = 0; i < Entities.Length; i++)
					{
					Entity entity = Entities [ i ];
					RefRW<RoomGenerationRequest> request = Requests.GetRefRW(entity);

					if (request.ValueRO.GeneratorType != RoomGeneratorType.LinearBranchingCorridor || request.ValueRO.IsComplete)
						{
						continue;
						}

					if (!FeatureBufferLookup.HasBuffer(entity))
						{
						continue;
						}

					DynamicBuffer<RoomFeatureElement> features = FeatureBufferLookup [ entity ];
					RefRO<RoomHierarchyData> roomDataRO = RoomData.GetRefRO(entity);
					RefRO<NodeId> nodeId = NodeIds.GetRefRO(entity);
					RectInt bounds = roomDataRO.ValueRO.Bounds;
					features.Clear();

					var entityRandom = new Unity.Mathematics.Random(BaseRandom.state + (uint)entity.Index);

					int beatCount = math.max(4, bounds.width / 6);
					int beatWidth = bounds.width / beatCount;

					// Use nodeId coordinates to influence rhythm and beat complexity patterns
					float rhythmComplexity = CalculateRhythmComplexity(nodeId.ValueRO);

					for (int beat = 0; beat < beatCount; beat++)
						{
						int beatX = bounds.x + (beat * beatWidth);
						BeatType beatType = DetermineBeatType(beat, beatCount, rhythmComplexity);

						GenerateBeat(features, bounds, beatX, beatWidth, beatType, beat, request.ValueRO.GenerationSeed, ref entityRandom);
						}

					if (SecretConfigLookup.HasComponent(entity))
						{
						SecretAreaConfig secretConfig = SecretConfigLookup [ entity ];
						GenerateBranchingPaths(features, bounds, secretConfig, request.ValueRO.GenerationSeed, ref entityRandom, rhythmComplexity);
						}

					request.ValueRW.IsComplete = true;
					}
				}

			// Add meaningful rhythm complexity calculation based on coordinates
			private static float CalculateRhythmComplexity (NodeId nodeId)
				{
				int2 coords = nodeId.Coordinates;
				float distance = math.length(coords);
				// Distance influences rhythm complexity - farther rooms have more complex beats
				float baseComplexity = math.clamp(distance / 15f, 0.6f, 2.0f);
				// Coordinate sum creates rhythm variation pattern
				float rhythmVariation = ((coords.x + coords.y) % 5) * 0.1f + 0.8f; // 0.8 to 1.2 range
				return baseComplexity * rhythmVariation;
				}

			private static BeatType DetermineBeatType (int beatIndex, int totalBeats, float rhythmComplexity)
				{
				// 🧮 COORDINATE-AWARE - Uses rhythm complexity for spatial beat pattern intelligence
				// Burst-compatible enum-based corridor classification instead of managed strings
				BeatType basePattern = (beatIndex % 3) switch
					{
						0 => BeatType.Challenge,
						1 => BeatType.Rest,
						2 => BeatType.Secret,
						_ => BeatType.Rest
						};

				// 🔧 ENHANCEMENT READY - Transform string-based logic into efficient enum classification
				// Sacred Symbol Preservation: Preserves corridor length analysis while making Burst-compatible
				CorridorLength corridorType = totalBeats > 8 ? CorridorLength.Long :
											totalBeats > 5 ? CorridorLength.Medium :
											CorridorLength.Short;

				float progressionFactor = (float)beatIndex / totalBeats; // 0.0 to 1.0 progression through corridor

				// 🧮 COORDINATE-AWARE - Adaptive pacing based on corridor length and spatial progression
				switch (corridorType)
					{
					case CorridorLength.Long:
						// Long corridors: gentle intro, intense middle, easier ending
						if (progressionFactor is < 0.2f or > 0.8f)
							{
							// Early and late beats favor rest for pacing
							if (basePattern == BeatType.Challenge && rhythmComplexity < 1.2f)
								{
								return BeatType.Rest;
								}
							}
						else if (progressionFactor is >= 0.3f and <= 0.7f)
							{
							// Middle section gets more challenging
							if (basePattern == BeatType.Rest && rhythmComplexity > 1.0f)
								{
								return BeatType.Challenge;
								}
							}
						break;

					case CorridorLength.Medium:
						// Medium corridors: steady escalation with secret opportunities
						if (progressionFactor > 0.6f && basePattern == BeatType.Rest)
							{
							// Later beats in medium corridors favor secrets for exploration
							if (rhythmComplexity > 1.3f)
								{
								return BeatType.Secret;
								}
							}
						break;

					case CorridorLength.Short:
						// Short corridors: front-loaded intensity for quick challenges
						if (progressionFactor < 0.5f && rhythmComplexity > 1.5f)
							{
							// Early beats in short corridors get immediate challenge
							if (basePattern == BeatType.Rest)
								{
								return BeatType.Challenge;
								}
							}
						break;
					default:
						break;
					}

				// Higher rhythm complexity can shift patterns for more variety (original logic preserved)
				if (rhythmComplexity > 1.5f)
					{
					// Complex areas get more challenge beats
					if (basePattern == BeatType.Rest && (beatIndex % 4) == 0)
						{
						return BeatType.Challenge;
						}
					}
				else if (rhythmComplexity < 0.8f)
					{
					// Simple areas get more rest beats
					if (basePattern == BeatType.Challenge && (beatIndex % 2) == 1)
						{
						return BeatType.Rest;
						}
					}

				return basePattern;
				}

			private static void GenerateBeat (DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
								int beatX, int beatWidth, BeatType beatType, int beatIndex, uint seed, ref Unity.Mathematics.Random random)
				{
				switch (beatType)
					{
					case BeatType.Challenge:
						GenerateChallengeBeat(features, bounds, beatX, beatWidth, ref random, seed, beatIndex);
						break;
					case BeatType.Rest:
						GenerateRestBeat(features, bounds, beatX, beatWidth, ref random, seed, beatIndex);
						break;
					case BeatType.Secret:
						GenerateSecretBeat(features, bounds, beatX, beatWidth, ref random, seed, beatIndex);
						break;
					default:
						break;
					}
				}

			private static void GenerateChallengeBeat (DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
											 int beatX, int beatWidth, ref Unity.Mathematics.Random random, uint seed, int beatIndex)
				{
				// Use beat index to influence obstacle patterns and difficulty scaling
				int baseObstacleCount = random.NextInt(1, 3);
				int obstacleCount = beatIndex > 5 ? baseObstacleCount + 1 : baseObstacleCount; // Later beats get more obstacles

				for (int i = 0; i < obstacleCount; i++)
					{
					var obstaclePos = new int2(
						random.NextInt(beatX, beatX + beatWidth),
						random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
					);

					features.Add(new RoomFeatureElement
						{
						Type = RoomFeatureType.Obstacle,
						Position = obstaclePos,
						FeatureId = (uint)(seed + i + beatIndex * 1000) // Include beat index for unique IDs
						});
					}

				// Platform placement influenced by beat position for progression difficulty
				int platformY = bounds.y + bounds.height / 2;
				if (beatIndex % 3 == 0) // Every third beat gets elevated platform
					{
					platformY = bounds.y + (bounds.height * 2) / 3;
					}

				var platformPos = new int2(
					beatX + beatWidth / 2,
					platformY
				);

				features.Add(new RoomFeatureElement
					{
				 Type = RoomFeatureType.Platform,
					Position = platformPos,
					FeatureId = (uint)(seed + 10 + beatIndex * 1000)
					});
				}

			private static void GenerateRestBeat (DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
									int beatX, int beatWidth, ref Unity.Mathematics.Random random, uint seed, int beatIndex)
				{
				// Use beat index to influence rest quality - later beats get better recovery opportunities
				float healthChance = beatIndex > 3 ? 0.5f : 0.3f; // Better health chances as player progresses

				if (random.NextFloat() < healthChance)
					{
					var healthPos = new int2(
						beatX + beatWidth / 2,
						bounds.y + 1
					);

					features.Add(new RoomFeatureElement
						{
						Type = RoomFeatureType.HealthPickup,
						Position = healthPos,
						FeatureId = (uint)(seed + 100 + beatIndex * 1000)
						});
					}

				// Platform placement adjusted for rest beat progression
				int platformX = beatX + random.NextInt(1, beatWidth - 1);
				if (beatIndex % 4 == 0) // Every fourth beat gets centered safe platform
					{
					platformX = beatX + beatWidth / 2;
					}

				var platformPos = new int2(
					platformX,
					bounds.y + 1
				);

				features.Add(new RoomFeatureElement
					{
				 Type = RoomFeatureType.Platform,
					Position = platformPos,
					FeatureId = (uint)(seed + 110 + beatIndex * 1000)
					});
				}

			private static void GenerateSecretBeat (DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
									  int beatX, int beatWidth, ref Unity.Mathematics.Random random, uint seed, int beatIndex)
				{
				// Use beat index to influence secret complexity and placement strategy
				bool useAdvancedPlacement = beatIndex > 2; // Later beats get more sophisticated secret hiding

				int2 secretPos;
				if (useAdvancedPlacement)
					{
					// Advanced placement: alternate between high and low based on beat index
					bool useHighPlacement = (beatIndex % 2) == 0;
					secretPos = new int2(
						beatX + random.NextInt(0, beatWidth),
						useHighPlacement ? bounds.y + bounds.height - 1 : bounds.y + 1
					);
					}
				else
					{
					// Simple placement for early beats
					secretPos = new int2(
						beatX + random.NextInt(0, beatWidth),
						random.NextFloat() > 0.5f ? bounds.y + bounds.height - 1 : bounds.y + 1
					);
					}

				features.Add(new RoomFeatureElement
					{
					Type = RoomFeatureType.Secret,
					Position = secretPos,
					FeatureId = (uint)(seed + 200 + beatIndex * 1000)
					});

				// Concealment strategy influenced by beat progression
				int concealmentComplexity = beatIndex > 4 ? 2 : 1; // Later beats get multiple concealment layers
				for (int c = 0; c < concealmentComplexity; c++)
					{
					int wallOffset = c == 0 ? (secretPos.y == bounds.y + 1 ? 1 : -1) : (c % 2 == 0 ? 1 : -1);
					var wallPos = new int2(secretPos.x + c, secretPos.y + wallOffset);

					// Ensure wall stays within bounds
					if (wallPos.x >= bounds.x && wallPos.x < bounds.x + bounds.width &&
						wallPos.y >= bounds.y && wallPos.y < bounds.y + bounds.height)
						{
						features.Add(new RoomFeatureElement
							{
						 Type = RoomFeatureType.Obstacle,
							Position = wallPos,
							FeatureId = (uint)(seed + 210 + beatIndex * 1000 + c)
							});
						}
					}
				}

			private static void GenerateBranchingPaths (DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
										  SecretAreaConfig secretConfig, uint seed, ref Unity.Mathematics.Random random, float rhythmComplexity)
				{
				// Create upper and lower alternate routes - influenced by rhythm complexity
				if (bounds.height > 6)
					{
					// Use secret config percentage to influence branching density scaling
					float secretAreaInfluence = secretConfig.SecretAreaPercentage; // Now this parameter has meaning!
					int baseBranchingDensity = (int)(rhythmComplexity * 3f);
					int secretInfluencedDensity = (int)(baseBranchingDensity * (1.0f + secretAreaInfluence)); // Secret areas boost branching complexity

					// Upper route - spacing influenced by rhythm complexity and secret area density
					int upperY = bounds.y + bounds.height - 2;
					int baseUpperSpacing = rhythmComplexity > 1.2f ? 3 : 4;
					// Use secretInfluencedDensity to determine complexity tier
					int complexityTier = secretInfluencedDensity switch
						{
							<= 1 => 0,  // Simple
							<= 2 => 1,  // Moderate  
							<= 3 => 2,  // Complex
							_ => 3   // Extreme
							};

					int upperSpacing = complexityTier switch
						{
							0 => baseUpperSpacing,
							1 => math.max(2, baseUpperSpacing - 1),
							2 => math.max(2, baseUpperSpacing - 2),
							3 => 2 // Minimum spacing for extreme complexity
,
							_ => throw new System.NotImplementedException()
							};

					// Use tier for other features too
					float bonusSecretChance = complexityTier * 0.1f; // 0%, 10%, 20%, 30% bonus
					int additionalPlatformRows = complexityTier >= 2 ? 1 : 0; // Extra vertical layer

					for (int x = bounds.x + 2; x < bounds.x + bounds.width - 2; x += upperSpacing)
						{
						features.Add(new RoomFeatureElement
							{
						 Type = RoomFeatureType.Platform,
							Position = new int2(x, upperY),
							FeatureId = (uint)(seed + 1000 + x)
							});

						// Use bonusSecretChance to actually add bonus secrets based on complexity
						if (secretAreaInfluence > 0.3f && random.NextFloat() < (secretAreaInfluence + bonusSecretChance))
							{
							features.Add(new RoomFeatureElement
								{
								Type = RoomFeatureType.Secret,
								Position = new int2(x, upperY + 1),
								FeatureId = (uint)(seed + 1500 + x)
								});
							}

						// Use bonusSecretChance for rare treasure placement in extreme complexity tiers
						if (complexityTier >= 3 && random.NextFloat() < bonusSecretChance * 0.5f)
							{
							features.Add(new RoomFeatureElement
								{
							 Type = RoomFeatureType.PowerUp, // Rare treasures in extreme areas
								Position = new int2(x + 1, upperY + 2),
								FeatureId = (uint)(seed + 1800 + x)
								});
							}
						}

					// Use additionalPlatformRows to create extra vertical layers in complex areas
					if (additionalPlatformRows > 0)
						{
						int extraLayerY = upperY + 3; // Above the main upper route
						if (extraLayerY < bounds.y + bounds.height - 1)
							{
							int extraSpacing = upperSpacing + 1; // Slightly wider spacing for challenge
							for (int x = bounds.x + 4; x < bounds.x + bounds.width - 4; x += extraSpacing)
								{
								features.Add(new RoomFeatureElement
									{
									Type = RoomFeatureType.Platform,
									Position = new int2(x, extraLayerY),
									FeatureId = (uint)(seed + 3000 + x)
									});

								// Extra layers get skill-specific challenges based on bonusSecretChance
								if (complexityTier >= 2 && random.NextFloat() < (0.4f + bonusSecretChance))
									{
									features.Add(new RoomFeatureElement
										{
										Type = RoomFeatureType.Switch, // Switches for advanced routes
										Position = new int2(x, extraLayerY + 1),
										FeatureId = (uint)(seed + 3500 + x)
										});
									}

								// Use bonusSecretChance to create ultra-rare collectibles in extra layers
								if (complexityTier >= 3 && random.NextFloat() < bonusSecretChance * 0.3f)
									{
									features.Add(new RoomFeatureElement
										{
										Type = RoomFeatureType.Collectible, // Ultra-rare drops in highest layers
										Position = new int2(x - 1, extraLayerY),
										FeatureId = (uint)(seed + 3800 + x)
										});
									}
								}
							}
						}
					}
				}
			}
		}

	/// <summary>
	/// Beat types for rhythm-based corridor generation
	/// </summary>
	public enum BeatType : byte
		{
		Challenge = 0,
		Rest = 1,
		Secret = 2
		}

	/// <summary>
	/// Corridor length classification for Burst-compatible spatial pacing analysis
	/// Replaces managed string comparisons with efficient enum-based logic
	/// </summary>
	public enum CorridorLength : byte
		{
		Short = 0,   // Quick intensity corridors (≤5 beats)
		Medium = 1,  // Balanced pacing corridors (6-8 beats) 
		Long = 2     // Extended journey corridors (9+ beats)
		}

	/// <summary>
	/// Biome-Weighted Heightmap Generator
	/// For top-world terrain generation using noise + biome masks
	/// Integrates with ECS terrain chunks for DOTS performance
	/// </summary>
	[BurstCompile]
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	[UpdateAfter(typeof(LinearBranchingCorridorGenerator))]
	public partial struct BiomeWeightedHeightmapGenerator : ISystem
		{
		private ComponentLookup<Core.Biome> _biomeLookup;
		private BufferLookup<RoomFeatureElement> _featureBufferLookup;
		private EntityQuery _roomGenerationQuery; // 🔥 ADD PRE-CREATED QUERY

		[BurstCompile]
		public void OnCreate (ref SystemState state)
			{
			_biomeLookup = state.GetComponentLookup<Core.Biome>(true);
			_featureBufferLookup = state.GetBufferLookup<RoomFeatureElement>();
			
			// 🔥 FIX: Create query in OnCreate instead of OnUpdate
			_roomGenerationQuery = new EntityQueryBuilder(Allocator.Persistent)
				.WithAll<RoomGenerationRequest, RoomHierarchyData, NodeId>()
				.Build(ref state);
			}

		// NOTE: Cannot use [BurstCompile] on OnUpdate due to ref SystemState parameter
		public void OnUpdate (ref SystemState state)
			{
			_biomeLookup.Update(ref state);
			_featureBufferLookup.Update(ref state);

			// 🔥 FIX: Use pre-created query instead of creating new one
			NativeArray<Entity> entities = _roomGenerationQuery.ToEntityArray(Allocator.Temp);
			ComponentLookup<RoomGenerationRequest> requests = state.GetComponentLookup<RoomGenerationRequest>(false);
			ComponentLookup<RoomHierarchyData> roomData = state.GetComponentLookup<RoomHierarchyData>(true);
			ComponentLookup<NodeId> nodeIds = state.GetComponentLookup<NodeId>(true);

			var heightmapJob = new HeightmapGenerationJob
				{
				BiomeLookup = _biomeLookup,
				FeatureBufferLookup = _featureBufferLookup,
				Entities = entities,
				Requests = requests,
				RoomData = roomData,
				NodeIds = nodeIds
				};

			heightmapJob.Execute();
			entities.Dispose();
			}

		[BurstCompile]
		private struct HeightmapGenerationJob : IJob
			{
			[ReadOnly] public ComponentLookup<Core.Biome> BiomeLookup;
			public BufferLookup<RoomFeatureElement> FeatureBufferLookup;
			[ReadOnly] public NativeArray<Entity> Entities;
			public ComponentLookup<RoomGenerationRequest> Requests;
			[ReadOnly] public ComponentLookup<RoomHierarchyData> RoomData;
			[ReadOnly] public ComponentLookup<NodeId> NodeIds;

			public void Execute ()
				{
				for (int i = 0; i < Entities.Length; i++)
					{
					Entity entity = Entities [ i ];
					RefRW<RoomGenerationRequest> request = Requests.GetRefRW(entity);

					if (request.ValueRO.GeneratorType != RoomGeneratorType.BiomeWeightedHeightmap || request.ValueRO.IsComplete)
						{
						continue;
						}

					if (!FeatureBufferLookup.HasBuffer(entity))
						{
						continue;
						}

					DynamicBuffer<RoomFeatureElement> features = FeatureBufferLookup [ entity ];
					RefRO<RoomHierarchyData> roomDataRO = RoomData.GetRefRO(entity);
					RectInt bounds = roomDataRO.ValueRO.Bounds;
					features.Clear();

					Core.Biome biome = BiomeLookup.HasComponent(entity) ? BiomeLookup [ entity ] :
							   new Core.Biome(BiomeType.SolarPlains, Polarity.Sun);

					GenerateBiomeHeightmap(features, bounds, biome, request.ValueRO.GenerationSeed);
					request.ValueRW.IsComplete = true;
					}
				}

			private static void GenerateBiomeHeightmap (DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
											  Core.Biome biome, uint seed)
				{
				var random = new Unity.Mathematics.Random(seed);
				float noiseScale = GetBiomeNoiseScale(biome.Type);
				float heightVariation = GetBiomeHeightVariation(biome.Type);
				int baseHeight = bounds.y + bounds.height / 3;

				for (int x = bounds.x; x < bounds.x + bounds.width; x++)
					{
					float noise = math.sin(x * noiseScale + seed * 0.001f) * 0.5f + 0.5f;
					int height = baseHeight + (int)(noise * heightVariation);
					height = math.clamp(height, bounds.y, bounds.y + bounds.height - 1);

					features.Add(new RoomFeatureElement
						{
						Type = RoomFeatureType.Platform,
						Position = new int2(x, height),
						FeatureId = (uint)(seed + x)
						});

					if (ShouldAddBiomeFeature(x, biome, seed, ref random))
						{
						RoomFeatureType featureType = GetBiomeSpecificFeature(biome.Type);
						int featureHeight = height + 1;

						if (featureHeight < bounds.y + bounds.height)
							{
							features.Add(new RoomFeatureElement
								{
								Type = featureType,
								Position = new int2(x, featureHeight),
								FeatureId = (uint)(seed + x + 10000)
								});
							}
						}
					}
				}

			private static float GetBiomeNoiseScale (BiomeType biome)
				{
				return biome switch
					{
						BiomeType.SolarPlains => 0.1f,
						BiomeType.FrozenWastes => 0.05f,
						BiomeType.VolcanicCore => 0.2f,
						BiomeType.CrystalCaverns => 0.15f,
						_ => 0.1f
						};
				}

			private static float GetBiomeHeightVariation (BiomeType biome)
				{
				return biome switch
					{
						BiomeType.SolarPlains => 3.0f,
						BiomeType.FrozenWastes => 1.0f,
						BiomeType.VolcanicCore => 5.0f,
						BiomeType.CrystalCaverns => 4.0f,
						_ => 2.0f
						};
				}

			private static bool ShouldAddBiomeFeature (int x, Core.Biome biome, uint seed, ref Unity.Mathematics.Random random)
				{
				float featureChance = biome.Type switch
					{
						BiomeType.SolarPlains => 0.1f,
						BiomeType.FrozenWastes => 0.05f,
						BiomeType.VolcanicCore => 0.2f,
						BiomeType.CrystalCaverns => 0.15f,
						_ => 0.08f
						};

				// Use x position and seed to create deterministic spatial variation in feature placement
				float spatialVariation = math.sin(x * 0.1f + seed * 0.001f) * 0.5f + 0.5f; // 0 to 1 range
				float adjustedChance = featureChance * (0.5f + spatialVariation); // Varies feature density spatially

				return random.NextFloat() < adjustedChance;
				}

			private static RoomFeatureType GetBiomeSpecificFeature (BiomeType biome)
				{
				return biome switch
					{
						BiomeType.SolarPlains => RoomFeatureType.Obstacle,
						BiomeType.FrozenWastes => RoomFeatureType.Obstacle,
						BiomeType.VolcanicCore => RoomFeatureType.Obstacle,
						BiomeType.CrystalCaverns => RoomFeatureType.Collectible,
						_ => RoomFeatureType.Obstacle
						};
				}
			}
		}

	/// <summary>
	/// Layered Platform/Cloud Generator
	/// For sky biome generation with moving cloud platforms and floating islands
	/// Motion patterns are biome/sub-biome specific
	/// </summary>
	[BurstCompile]
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	[UpdateAfter(typeof(BiomeWeightedHeightmapGenerator))]
	public partial struct LayeredPlatformCloudGenerator : ISystem
		{
		private BufferLookup<RoomFeatureElement> _featureBufferLookup;
		private ComponentLookup<Core.Biome> _biomeLookup;
		private EntityQuery _roomGenerationQuery; // 🔥 ADD PRE-CREATED QUERY

		[BurstCompile]
		public void OnCreate (ref SystemState state)
			{
			_featureBufferLookup = state.GetBufferLookup<RoomFeatureElement>();
			_biomeLookup = state.GetComponentLookup<Core.Biome>(true);
			
			// 🔥 FIX: Create query in OnCreate instead of OnUpdate
			_roomGenerationQuery = new EntityQueryBuilder(Allocator.Persistent)
				.WithAll<RoomGenerationRequest, RoomHierarchyData, NodeId>()
				.Build(ref state);
			}

		// NOTE: Cannot use [BurstCompile] on OnUpdate due to ref SystemState parameter
		public void OnUpdate (ref SystemState state)
			{
			_featureBufferLookup.Update(ref state);
			_biomeLookup.Update(ref state);

			// 🔥 FIX: Use pre-created query instead of creating new one
			NativeArray<Entity> entities = _roomGenerationQuery.ToEntityArray(Allocator.Temp);
			ComponentLookup<RoomGenerationRequest> requests = state.GetComponentLookup<RoomGenerationRequest>(false);
			ComponentLookup<RoomHierarchyData> roomData = state.GetComponentLookup<RoomHierarchyData>(true);
			ComponentLookup<NodeId> nodeIds = state.GetComponentLookup<NodeId>(true);

			var cloudJob = new CloudGenerationJob
				{
				FeatureBufferLookup = _featureBufferLookup,
				BiomeLookup = _biomeLookup,
				Entities = entities,
				Requests = requests,
				RoomData = roomData,
				NodeIds = nodeIds
				};

			cloudJob.Execute();
			entities.Dispose();
			}

		[BurstCompile]
		private struct CloudGenerationJob : IJob
			{
			public BufferLookup<RoomFeatureElement> FeatureBufferLookup;
			[ReadOnly] public ComponentLookup<Core.Biome> BiomeLookup;
			[ReadOnly] public NativeArray<Entity> Entities;
			public ComponentLookup<RoomGenerationRequest> Requests;
			[ReadOnly] public ComponentLookup<RoomHierarchyData> RoomData;
			[ReadOnly] public ComponentLookup<NodeId> NodeIds;

			public void Execute ()
				{
				for (int i = 0; i < Entities.Length; i++)
					{
					Entity entity = Entities [ i ];
					RefRW<RoomGenerationRequest> request = Requests.GetRefRW(entity);

					if (request.ValueRO.GeneratorType != RoomGeneratorType.LayeredPlatformCloud || request.ValueRO.IsComplete)
						{
						continue;
						}

					if (!FeatureBufferLookup.HasBuffer(entity))
						{
						continue;
						}

					DynamicBuffer<RoomFeatureElement> features = FeatureBufferLookup [ entity ];
					RefRO<RoomHierarchyData> roomDataRO = RoomData.GetRefRO(entity);
					RefRO<NodeId> nodeId = NodeIds.GetRefRO(entity);
					RectInt bounds = roomDataRO.ValueRO.Bounds;
					features.Clear();

					Core.Biome biome = BiomeLookup.HasComponent(entity) ? BiomeLookup [ entity ] :
							   new Core.Biome(BiomeType.SkyGardens, Polarity.Wind);

					var random = new Unity.Mathematics.Random(request.ValueRO.GenerationSeed + (uint)entity.Index);

					// Use nodeId coordinates to influence cloud layer patterns and island placement
					float skyComplexity = CalculateSkyComplexity(nodeId.ValueRO);

					int layerCount = math.max(3, bounds.height / 4);

					for (int layer = 0; layer < layerCount; layer++)
						{
						int layerY = bounds.y + (layer * bounds.height / layerCount);
						GenerateCloudLayer(features, bounds, layerY, layer, biome, request.ValueRO.GenerationSeed, ref random, skyComplexity);
						}

					GenerateFloatingIslands(features, bounds, biome, request.ValueRO.GenerationSeed, ref random, skyComplexity);
					request.ValueRW.IsComplete = true;
					}
				}

			// Add meaningful sky complexity calculation based on coordinates
			private static float CalculateSkyComplexity (NodeId nodeId)
				{
				int2 coords = nodeId.Coordinates;
				int altitude = coords.y; // Y coordinate represents altitude in sky biomes
				float distance = math.length(coords);

				// Higher altitude areas are more complex (more challenging sky navigation)
				float altitudeComplexity = math.clamp(altitude / 10f + 1f, 0.8f, 2.5f);
				// Distance from origin adds variation
				float distanceVariation = math.clamp(distance / 25f, 0.7f, 1.6f);

				return altitudeComplexity * distanceVariation;
				}

			private static void GenerateCloudLayer (DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
										  int layerY, int layerIndex, Core.Biome biome, uint seed, ref Unity.Mathematics.Random random, float skyComplexity)
				{
				// Use sky complexity to influence cloud density and arrangement patterns
				int baseCloudCount = random.NextInt(2, 5);
				int cloudCount = (int)(baseCloudCount * skyComplexity); // More complex skies get more clouds
				cloudCount = math.clamp(cloudCount, 1, 8); // Reasonable bounds

				for (int cloud = 0; cloud < cloudCount; cloud++)
					{
					int cloudX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
					int cloudY = layerY + random.NextInt(-1, 2);

					features.Add(new RoomFeatureElement
						{
					 Type = RoomFeatureType.Platform,
						Position = new int2(cloudX, cloudY),
						FeatureId = (uint)(seed + layerIndex * 1000 + cloud * 100)
						});

					CloudMotionType motionType = GetCloudMotionType(biome.Type, biome.PrimaryPolarity);

					// Complex skies get more motion features
					if (skyComplexity > 1.3f)
						{
						AddCloudMotionFeature(features, cloudX, cloudY, motionType, seed, layerIndex, cloud);
						}
					}
				}

			private static void GenerateFloatingIslands (DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
											   Core.Biome biome, uint seed, ref Unity.Mathematics.Random random, float skyComplexity)
				{
				// Use sky complexity to determine island density and size
				int baseIslandCount = random.NextInt(1, 3);
				int islandCount = skyComplexity > 1.5f ? baseIslandCount + 1 : baseIslandCount; // Complex skies get extra islands

				for (int island = 0; island < islandCount; island++)
					{
					int islandCenterX = random.NextInt(bounds.x + 3, bounds.x + bounds.width - 3);
					int islandCenterY = random.NextInt(bounds.y + 2, bounds.y + bounds.height - 2);

					// Island size influenced by sky complexity
					int islandSize = skyComplexity > 1.2f ? 2 : 1; // Larger islands in complex areas

					for (int dx = -islandSize; dx <= islandSize; dx++)
						{
						for (int dy = 0; dy <= islandSize; dy++)
							{
							var pos = new int2(islandCenterX + dx, islandCenterY + dy);
							if (pos.x >= bounds.x && pos.x < bounds.x + bounds.width &&
								pos.y >= bounds.y && pos.y < bounds.y + bounds.height)
								{
								features.Add(new RoomFeatureElement
									{
									Type = RoomFeatureType.Platform,
									Position = pos,
									FeatureId = (uint)(seed + 5000 + island * 100 + dx * 10 + dy)
									});
								}
							}
						}

					AddIslandFeatures(features, islandCenterX, islandCenterY, biome, ref random, seed, island, skyComplexity);
					}
				}

			private static CloudMotionType GetCloudMotionType (BiomeType biome, Polarity polarity)
				{
				return biome switch
					{
						BiomeType.SkyGardens => CloudMotionType.Gentle,
						BiomeType.PlasmaFields => CloudMotionType.Electric,
						BiomeType.PowerPlant => CloudMotionType.Conveyor,
						_ => polarity switch
							{
								Polarity.Wind => CloudMotionType.Gusty,
								Polarity.Tech => CloudMotionType.Conveyor,
								_ => CloudMotionType.Gentle
								}
						};
				}

			private static void AddCloudMotionFeature (DynamicBuffer<RoomFeatureElement> features, int cloudX, int cloudY,
											 CloudMotionType motionType, uint seed, int layerIndex, int cloudIndex)
				{
				RoomFeatureType motionFeatureType = motionType switch
					{
						CloudMotionType.Conveyor => RoomFeatureType.Platform,
						CloudMotionType.Electric => RoomFeatureType.Obstacle,
						_ => RoomFeatureType.Platform
						};

				features.Add(new RoomFeatureElement
					{
					Type = motionFeatureType,
					Position = new int2(cloudX, cloudY + 1),
					FeatureId = (uint)(seed + layerIndex * 1000 + cloudIndex * 100 + 50)
					});
				}

			private static void AddIslandFeatures (DynamicBuffer<RoomFeatureElement> features, int centerX, int centerY,
										 Core.Biome biome, ref Unity.Mathematics.Random random, uint seed, int islandIndex, float skyComplexity)
				{
				RoomFeatureType featureType = biome.Type switch
					{
						BiomeType.SkyGardens => RoomFeatureType.PowerUp,
						BiomeType.PlasmaFields => RoomFeatureType.Collectible,
						BiomeType.PowerPlant => RoomFeatureType.SaveStation,
						_ => RoomFeatureType.Secret
						};

				// Sky complexity influences feature placement probability and variety
				float baseChance = 0.7f;
				float complexityBonus = (skyComplexity - 1.0f) * 0.3f; // More complex areas get higher chance
				float featureChance = math.clamp(baseChance + complexityBonus, 0.3f, 0.95f);

				if (random.NextFloat() < featureChance)
					{
					features.Add(new RoomFeatureElement
						{
						Type = featureType,
						Position = new int2(centerX, centerY + 2),
						FeatureId = (uint)(seed + 6000 + islandIndex * 100)
						});

					// Very complex sky areas get additional features
					if (skyComplexity > 2.0f && random.NextFloat() < 0.4f)
						{
						features.Add(new RoomFeatureElement
							{
						 Type = RoomFeatureType.Secret, // Bonus secret in complex areas
							Position = new int2(centerX + 1, centerY + 1),
							FeatureId = (uint)(seed + 6000 + islandIndex * 100 + 50)
							});
						}
					}
				}
			}
		}

	/// <summary>
	/// Cloud motion patterns for sky biome generation
	/// </summary>
	public enum CloudMotionType : byte
		{
		Gentle = 0,    // Slow, predictable drifting
		Gusty = 1,     // Irregular wind patterns
		Conveyor = 2,  // Mechanical conveyor-like movement
		Electric = 3   // Rapid, energetic movement
		}

	/// <summary>
	/// Utility class for type conversions
	/// </summary>
	public static class TypeConversionUtility
		{
		/// <summary>
		/// Convert RoomFeatureType to RoomFeatureObjectType (DEPRECATED - compatibility shim)
		/// </summary>
		[System.Obsolete("Use RoomFeatureType directly instead")]
		public static RoomFeatureObjectType ConvertToObjectType (RoomFeatureType featureType)
			{
			return featureType switch
				{
					RoomFeatureType.Platform => RoomFeatureObjectType.Platform,
					RoomFeatureType.Obstacle => RoomFeatureObjectType.Obstacle,
					RoomFeatureType.Secret => RoomFeatureObjectType.Secret,
					RoomFeatureType.PowerUp => RoomFeatureObjectType.PowerUp,
					RoomFeatureType.HealthPickup => RoomFeatureObjectType.HealthPickup,
					RoomFeatureType.SaveStation => RoomFeatureObjectType.SaveStation,
					RoomFeatureType.Switch => RoomFeatureObjectType.Switch,
					_ => RoomFeatureObjectType.Platform // Default fallback
					};
			}

		/// <summary>
		/// Compatibility shim - Use RoomFeatureType directly for new code
		/// </summary>
		public static RoomFeatureType NormalizeFeatureType (RoomFeatureType featureType)
			{
			return featureType; // Pass-through for compatibility
			}
		}
	}
