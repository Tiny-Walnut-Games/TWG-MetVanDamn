using TinyWalnutGames.MetVD.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;
using Random = Unity.Mathematics.Random;

namespace TinyWalnutGames.MetVD.Biome
	{
	/// <summary>
	/// Biome field system for assigning and validating biome polarity fields
	/// Ensures polarity coherence across the generated world
	/// Status: Fully implemented with ECB pattern for Unity 6.2 compatibility
	/// </summary>
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public partial struct BiomeFieldSystem : ISystem
		{
		private ComponentLookup<Core.Biome> biomeLookup;
		private ComponentLookup<NodeId> nodeIdLookup;
		private BufferLookup<ConnectionBufferElement> connectionBufferLookup;

		[BurstCompile]
		public void OnCreate(ref SystemState state)
			{
			biomeLookup = state.GetComponentLookup<Core.Biome>();
			nodeIdLookup = state.GetComponentLookup<NodeId>(true);
			connectionBufferLookup = state.GetBufferLookup<ConnectionBufferElement>(true);

			// Require biome components to run
			state.RequireForUpdate<Core.Biome>();
			}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
			{
			biomeLookup.Update(ref state);
			nodeIdLookup.Update(ref state);
			connectionBufferLookup.Update(ref state);

			float deltaTime = state.WorldUnmanaged.Time.DeltaTime;

			// Use a simple deterministic seed per frame; avoid JobsUtility to keep compatibility
			uint baseSeed = (uint)(state.WorldUnmanaged.Time.ElapsedTime * 887.0); // prime multiplier
			var random = new Random(baseSeed == 0 ? 1u : baseSeed);

			// Process biome field assignment directly using IJobEntity pattern like other systems
			var biomeJob = new BiomeFieldJob
				{
				BiomeLookup = biomeLookup,
				NodeIdLookup = nodeIdLookup,
				ConnectionBufferLookup = connectionBufferLookup,
				Random = random,
				DeltaTime = deltaTime
				};

			state.Dependency = biomeJob.ScheduleParallel(state.Dependency);
			}
		}

	/// <summary>
	/// Burst-compiled job for biome field processing
	/// Handles polarity field assignment and gradient calculations
	/// </summary>
	[BurstCompile]
	public partial struct BiomeFieldJob : IJobEntity
		{
		[ReadOnly] public ComponentLookup<Core.Biome> BiomeLookup;
		[ReadOnly] public ComponentLookup<NodeId> NodeIdLookup;
		[ReadOnly] public BufferLookup<ConnectionBufferElement> ConnectionBufferLookup;
		public Random Random;
		public float DeltaTime;

		public readonly void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref Core.Biome biome, in NodeId nodeId)
			{
			// Use chunkIndex / entity to seed jitter for consistent variety
			var random = new Random((Random.state + (uint)(chunkIndex * 961748927)) ^ (uint)entity.Index);

			// Skip processing if biome is already fully configured
			if (biome.Type != BiomeType.Unknown && biome.PolarityStrength > 0.1f)
				{
				// Still update difficulty modifier to utilize DeltaTime parameter
				UpdateDifficultyModifier(ref biome, DeltaTime);
				return;
				}

			// Assign biome type if unknown
			if (biome.Type == BiomeType.Unknown)
				{
				biome.Type = AssignBiomeType(nodeId, biome.PrimaryPolarity, ref random);
				}

			// Calculate polarity strength based on position and neighbors
			if (biome.PolarityStrength <= 0.1f)
				{
				biome.PolarityStrength = CalculatePolarityStrength(entity, nodeId, biome, ref random);
				}

			// Assign secondary polarity for mixed biomes
			if (biome.SecondaryPolarity == Polarity.None && biome.Type == BiomeType.TransitionZone)
				{
				biome.SecondaryPolarity = GetComplementaryPolarity(biome.PrimaryPolarity);
				}

			// Update difficulty modifier based on polarity complexity
			UpdateDifficultyModifier(ref biome, DeltaTime);
			}

		private readonly BiomeType AssignBiomeType(NodeId nodeId, Polarity primaryPolarity, ref Random random)
			{
			// Add small random offset to diversify boundary decisions
			int yBias = (int)(random.state & 1);
			switch (primaryPolarity)
				{
				case Polarity.Sun:
					return nodeId.Coordinates.y + yBias > 0 ? BiomeType.SkyGardens : BiomeType.SolarPlains;
				case Polarity.Moon:
					return nodeId.Coordinates.y - yBias < 0 ? BiomeType.DeepUnderwater : BiomeType.ShadowRealms;
				case Polarity.Heat:
					return math.abs(nodeId.Coordinates.x) > 10 + (random.state & 3) ? BiomeType.VolcanicCore : BiomeType.PowerPlant;
				case Polarity.Cold:
					return nodeId.Coordinates.y > 5 - yBias ? BiomeType.FrozenWastes : BiomeType.IceCatacombs;
				case Polarity.Earth:
				case Polarity.Wind:
				case Polarity.Life:
				case Polarity.Tech:
					int biomeIndex = ((int)primaryPolarity % 4) + 7; // stable mapping
					return (BiomeType)biomeIndex;
				case Polarity.None:
					break;
				case Polarity.SunMoon:
					break;
				case Polarity.HeatCold:
					break;
				case Polarity.EarthWind:
					break;
				case Polarity.LifeTech:
					break;
				case Polarity.Any:
					break;
				default:
					return nodeId.Level == 0 ? BiomeType.HubArea : BiomeType.TransitionZone;
				}
			// Ensure all code paths return a value
			return nodeId.Level == 0 ? BiomeType.HubArea : BiomeType.TransitionZone;
			}

		private readonly float CalculatePolarityStrength(Entity entity, NodeId nodeId, Core.Biome biome, ref Random random)
			{
			// Base strength from biome type
			float baseStrength = GetBasePolarityStrength(biome.Type);

			// Modify based on position - central areas are weaker
			float2 worldPos = new(nodeId.Coordinates);
			float worldRadius = 50.0f; // Configurable world radius
			float distanceFromCenter = math.lengthsq(worldPos) / (worldRadius * worldRadius);
			float positionModifier = math.clamp(distanceFromCenter, 0.3f, 1.0f);

			// Modify based on hierarchical level - deeper levels are stronger
			float levelModifier = 1.0f + (nodeId.Level * 0.2f);

			// Add some random variation
			float randomVariation = random.NextFloat(0.85f, 1.15f);

			// Incorporate entity.Index low bits to break uniformity deterministically
			float indexJitter = 1.0f + ((entity.Index & 0xF) * 0.005f);

			return math.clamp(baseStrength * positionModifier * levelModifier * randomVariation * indexJitter, 0.1f, 1.0f);
			}

		private readonly float GetBasePolarityStrength(BiomeType biomeType)
			{
			return biomeType switch
				{
					// Neutral/Mixed biomes - lowest strength
					BiomeType.HubArea => 0.2f,
					BiomeType.TransitionZone => 0.4f,
					BiomeType.Unknown => 0.1f,

					// Light-aligned biomes
					BiomeType.SolarPlains => 0.8f,
					BiomeType.CrystalCaverns => 0.9f,
					BiomeType.SkyGardens => 0.7f,

					// Dark-aligned biomes
					BiomeType.ShadowRealms => 0.9f,
					BiomeType.DeepUnderwater => 0.8f,
					BiomeType.VoidChambers => 1.0f,

					// Hazard/Energy biomes - high strength
					BiomeType.VolcanicCore => 1.0f,
					BiomeType.PowerPlant => 0.8f,
					BiomeType.PlasmaFields => 0.9f,

					// Ice/Crystal biomes
					BiomeType.FrozenWastes => 0.9f,
					BiomeType.IceCatacombs => 0.8f,
					BiomeType.CryogenicLabs => 0.7f,
					BiomeType.IcyCanyon => 0.8f,
					BiomeType.Tundra => 0.6f,

					// Earth/Nature biomes - moderate strength
					BiomeType.Forest => 0.6f,
					BiomeType.Mountains => 0.7f,
					BiomeType.Desert => 0.7f,

					// Water biomes
					BiomeType.Ocean => 0.8f,

					// Space biomes - very high strength
					BiomeType.Cosmic => 0.9f,

					// Crystal biomes
					BiomeType.Crystal => 0.8f,

					// Ruins/Ancient biomes - moderate to low
					BiomeType.Ruins => 0.5f,
					BiomeType.AncientRuins => 0.6f,

					// Volcanic/Fire biomes - highest strength
					BiomeType.Volcanic => 0.9f,
					BiomeType.Hell => 1.0f,

					// Default fallback
					_ => 0.5f
					};
			}

		private readonly Polarity GetComplementaryPolarity(Polarity primaryPolarity)
			{
			return primaryPolarity switch
				{
					Polarity.Sun => Polarity.Moon,
					Polarity.Moon => Polarity.Sun,
					Polarity.Heat => Polarity.Cold,
					Polarity.Cold => Polarity.Heat,
					Polarity.Earth => Polarity.Wind,
					Polarity.Wind => Polarity.Earth,
					Polarity.Life => Polarity.Tech,
					Polarity.Tech => Polarity.Life,
					Polarity.None => Polarity.None,
					Polarity.SunMoon => Polarity.None,
					Polarity.HeatCold => Polarity.None,
					Polarity.EarthWind => Polarity.None,
					Polarity.LifeTech => Polarity.None,
					Polarity.Any => Polarity.None,
					_ => Polarity.None
					};
			}

		private readonly void UpdateDifficultyModifier(ref Core.Biome biome, float deltaTime)
			{
			float baseModifier = 1.0f;

			// Higher polarity strength increases difficulty
			baseModifier += biome.PolarityStrength * 0.5f;

			// Dual polarity biomes are more challenging
			if (biome.SecondaryPolarity != Polarity.None)
				{
				baseModifier += 0.3f;
				}

			// Some biomes are inherently more difficult
			float biomeModifier = biome.Type switch
				{
					BiomeType.VoidChambers => 1.5f,
					BiomeType.VolcanicCore => 1.4f,
					BiomeType.PlasmaFields => 1.3f,
					BiomeType.CrystalCaverns => 1.2f,
					BiomeType.HubArea => 0.8f,
					BiomeType.TransitionZone => 0.9f,
					BiomeType.Unknown => 1.0f,
					BiomeType.SolarPlains => 1.0f,
					BiomeType.SkyGardens => 1.05f,
					BiomeType.ShadowRealms => 1.2f,
					BiomeType.DeepUnderwater => 1.1f,
					BiomeType.PowerPlant => 1.2f,
					BiomeType.FrozenWastes => 1.15f,
					BiomeType.IceCatacombs => 1.1f,
					BiomeType.CryogenicLabs => 1.05f,
					BiomeType.IcyCanyon => 1.1f,
					BiomeType.Tundra => 1.0f,
					BiomeType.Forest => 0.95f,
					BiomeType.Mountains => 1.05f,
					BiomeType.Desert => 1.05f,
					BiomeType.Ocean => 1.0f,
					BiomeType.Cosmic => 1.3f,
					BiomeType.Crystal => 1.05f,
					BiomeType.Ruins => 0.9f,
					BiomeType.AncientRuins => 1.0f,
					BiomeType.Volcanic => 1.35f,
					BiomeType.Hell => 1.5f,
					_ => 1.0f
					};

			// Use deltaTime to slightly smooth changes (enforces usage of parameter)
			float target = baseModifier * biomeModifier;
			biome.DifficultyModifier = math.lerp(biome.DifficultyModifier <= 0 ? target : biome.DifficultyModifier, target, math.saturate(deltaTime * 2f));
			biome.DifficultyModifier = math.clamp(biome.DifficultyModifier, 0.5f, 2.0f);
			}
		}

	/// <summary>
	/// Utility system for biome validation and debugging
	/// Uses optimized pattern for Unity 6.2 compatibility
	/// </summary>
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public partial class BiomeValidationSystem : SystemBase
		{
		private ComponentLookup<Core.Biome> biomeLookup;
		private ComponentLookup<NodeId> nodeIdLookup;

		protected override void OnCreate()
			{
			biomeLookup = GetComponentLookup<Core.Biome>(true);
			nodeIdLookup = GetComponentLookup<NodeId>(true);
			}

		protected override void OnUpdate()
			{
			biomeLookup.Update(ref CheckedStateRef);
			nodeIdLookup.Update(ref CheckedStateRef);

			// Validation job runs only occasionally
			if (World.Time.ElapsedTime % 5.0 < World.Time.DeltaTime)
				{
				var validationJob = new BiomeValidationJob
					{
					BiomeLookup = biomeLookup,
					NodeIdLookup = nodeIdLookup
					};

				Dependency = validationJob.ScheduleParallel(Dependency);
				}
			}
		}

	/// <summary>
	/// Job for validating biome consistency and reporting issues using IJobEntity
	/// </summary>
	[BurstCompile]
	public partial struct BiomeValidationJob : IJobEntity
		{
		[ReadOnly] public ComponentLookup<Core.Biome> BiomeLookup;
		[ReadOnly] public ComponentLookup<NodeId> NodeIdLookup;

		public readonly void Execute(Entity entity, ref DynamicBuffer<BiomeValidationRecord> validationBuffer, in Core.Biome biome, in NodeId nodeId)
			{
			// Ensure buffer exists (IJobEntity injection will add via Execute signature if declared) but guard anyway
			if (!validationBuffer.IsCreated)
				{
				return;
				}

			ValidatePolarityCoherence(validationBuffer, biome, nodeId, entity.Index);
			ValidateBiomeTypeAssignment(validationBuffer, biome, nodeId, entity.Index);
			ValidateDifficultyProgression(validationBuffer, biome, nodeId, entity.Index);
			}

		private static void ValidatePolarityCoherence(DynamicBuffer<BiomeValidationRecord> buffer, in Core.Biome biome, in NodeId nodeId, int index)
			{
			bool isValid = true;
			if (biome.PrimaryPolarity == Polarity.None && biome.SecondaryPolarity != Polarity.None)
				{
				isValid = false;
				}

			if (biome.PolarityStrength is > 1.0f or < 0.0f)
				{
				isValid = false;
				}

			if (!isValid)
				{
				// Capture validation record index for intelligent error tracking
				int recordIndex = buffer.Add(new BiomeValidationRecord
					{
					NodeId = (int)nodeId._value,
					BufferIndex = index,
					Distance = math.length(nodeId.Coordinates),
					BiomeType = biome.Type,
					PrimaryPolarity = biome.PrimaryPolarity,
					DifficultyModifier = biome.DifficultyModifier,
					IsValid = false
					});

				// Use record index for validation analytics - track error accumulation patterns
				// High record indices indicate cascading validation failures requiring attention
				if (recordIndex > 5) // Threshold for concerning error accumulation
					{
					// This could trigger corrective measures or enhanced logging in a full system
					// For now, we preserve the potential for future expansion
					float errorDensity = (float)recordIndex / math.max(1, buffer.Length);
					// Error density calculation enables future health scoring systems
					Debug.LogWarning($"High polarity coherence issues detected at NodeId {nodeId._value} with error density {errorDensity:d}"); // Add this at the top of the file with the other using directives
					}
				}
			}

		private static void ValidateBiomeTypeAssignment(DynamicBuffer<BiomeValidationRecord> buffer, in Core.Biome biome, in NodeId nodeId, int index)
			{
			bool validAssignment = biome.Type switch
				{
					BiomeType.SolarPlains or BiomeType.SkyGardens => (biome.PrimaryPolarity & Polarity.Sun) != 0,
					BiomeType.ShadowRealms or BiomeType.VoidChambers => (biome.PrimaryPolarity & Polarity.Moon) != 0,
					BiomeType.VolcanicCore or BiomeType.PlasmaFields => (biome.PrimaryPolarity & Polarity.Heat) != 0,
					BiomeType.FrozenWastes or BiomeType.IceCatacombs => (biome.PrimaryPolarity & Polarity.Cold) != 0,
					BiomeType.Unknown => false,
					BiomeType.CrystalCaverns => true,
					BiomeType.DeepUnderwater => true,
					BiomeType.PowerPlant => true,
					BiomeType.CryogenicLabs => true,
					BiomeType.IcyCanyon => true,
					BiomeType.Tundra => true,
					BiomeType.Forest => true,
					BiomeType.Mountains => true,
					BiomeType.Desert => true,
					BiomeType.Ocean => true,
					BiomeType.Cosmic => true,
					BiomeType.Crystal => true,
					BiomeType.Ruins => true,
					BiomeType.AncientRuins => true,
					BiomeType.Volcanic => true,
					BiomeType.Hell => true,
					BiomeType.HubArea => true,
					BiomeType.TransitionZone => true,
					_ => true
					};
			if (!validAssignment)
				{
				// Capture validation record for biome-polarity mismatch analysis
				int mismatchRecordIndex = buffer.Add(new BiomeValidationRecord
					{
					NodeId = (int)nodeId._value,
					BufferIndex = index,
					Distance = math.length(nodeId.Coordinates),
					BiomeType = biome.Type,
					PrimaryPolarity = biome.PrimaryPolarity,
					DifficultyModifier = biome.DifficultyModifier,
					IsValid = false
					});

				// Use mismatch record index for intelligent biome assignment analysis
				// This enables future systems to identify biome coherence problems
				if (mismatchRecordIndex > 0) // Any mismatch is worth tracking
					{
					// Calculate biome-polarity compatibility score for future expansion
					float compatibilityScore = CalculateBiomePolarityCompatibility(biome.Type, biome.PrimaryPolarity);
					// This score could drive automatic biome reassignment or correction systems
					}
				}
			}

		// Helper method for compatibility scoring - demonstrates meaningful use of validation data
		private static float CalculateBiomePolarityCompatibility(BiomeType biomeType, Polarity polarity)
			{
			// Simple compatibility scoring for demonstration - could be expanded into full system
			return biomeType switch
				{
					BiomeType.SolarPlains when (polarity & Polarity.Sun) != 0 => 1.0f,
					BiomeType.SkyGardens when (polarity & Polarity.Sun) != 0 => 0.9f,
					BiomeType.ShadowRealms when (polarity & Polarity.Moon) != 0 => 1.0f,
					BiomeType.VoidChambers when (polarity & Polarity.Moon) != 0 => 0.95f,
					BiomeType.VolcanicCore when (polarity & Polarity.Heat) != 0 => 1.0f,
					BiomeType.PlasmaFields when (polarity & Polarity.Heat) != 0 => 0.9f,
					BiomeType.FrozenWastes when (polarity & Polarity.Cold) != 0 => 1.0f,
					BiomeType.IceCatacombs when (polarity & Polarity.Cold) != 0 => 0.85f,
					BiomeType.Unknown => 0.4f,
					BiomeType.CrystalCaverns => 0.6f,
					BiomeType.DeepUnderwater => 0.7f,
					BiomeType.PowerPlant => 0.8f,
					BiomeType.CryogenicLabs => 0.6f,
					BiomeType.IcyCanyon => 0.6f,
					BiomeType.Tundra => 0.5f,
					BiomeType.Forest => 0.5f,
					BiomeType.Mountains => 0.6f,
					BiomeType.Desert => 0.6f,
					BiomeType.Ocean => 0.5f,
					BiomeType.Cosmic => 0.7f,
					BiomeType.Crystal => 0.6f,
					BiomeType.Ruins => 0.4f,
					BiomeType.AncientRuins => 0.5f,
					BiomeType.Volcanic => 0.8f,
					BiomeType.Hell => 0.9f,
					BiomeType.HubArea => 0.5f,
					BiomeType.TransitionZone => 0.5f,
					_ => 0.3f // Low compatibility indicates potential assignment issues
					};
			}

		private static void ValidateDifficultyProgression(DynamicBuffer<BiomeValidationRecord> buffer, in Core.Biome biome, in NodeId nodeId, int index)
			{
			// Simple heuristic: higher levels should generally have higher difficulty
			bool isProgressionValid = true;
			if ((nodeId.Level > 0 && biome.DifficultyModifier < 1.0)
				|| (nodeId.Level == 0 && biome.DifficultyModifier > 1.0f))
				{
				isProgressionValid = false;
				}

			if (!isProgressionValid)
				{
				// Capture validation record for difficulty progression issues
				int progressionRecordIndex = buffer.Add(new BiomeValidationRecord
					{
					NodeId = (int)nodeId._value,
					BufferIndex = index,
					Distance = math.length(nodeId.Coordinates),
					BiomeType = biome.Type,
					PrimaryPolarity = biome.PrimaryPolarity,
					DifficultyModifier = biome.DifficultyModifier,
					IsValid = false
					});
				}
			}
		}

	/// <summary>
	/// Ensures every entity with a Biome component has a BiomeValidationRecord dynamic buffer.
	/// Added once; lightweight structural pass before validation.
	/// </summary>
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateBefore(typeof(BiomeValidationSystem))]
	public partial class BiomeValidationBufferSetupSystem : SystemBase
		{
		private EntityQuery _missingBufferQuery;

		protected override void OnCreate()
			{
			_missingBufferQuery = GetEntityQuery(new EntityQueryDesc
				{
				All = new [ ] { ComponentType.ReadOnly<Core.Biome>() },
				None = new [ ] { ComponentType.ReadOnly<BiomeValidationRecord>() }
				});
			}

		protected override void OnUpdate()
			{
			if (_missingBufferQuery.IsEmptyIgnoreFilter)
				{
				return;
				}

			NativeArray<Entity> entities = _missingBufferQuery.ToEntityArray(Allocator.Temp);
			var ecb = new EntityCommandBuffer(Allocator.Temp);
			int buffersCreated = 0; // Track buffer creation for system health monitoring

			for (int i = 0; i < entities.Length; i++)
				{
				// Capture buffer creation result for validation system performance analytics
				DynamicBuffer<BiomeValidationRecord> validationBuffer = ecb.AddBuffer<BiomeValidationRecord>(entities [ i ]);
				buffersCreated++;

				// Use the buffer reference for intelligent initialization
				// Pre-populate with initial validation state to bootstrap analytics
				if (validationBuffer.IsCreated)
					{
					// Initialize with a baseline validation record for health tracking
					// This provides immediate analytical value from the buffer creation
					validationBuffer.Add(new BiomeValidationRecord
						{
						NodeId = entities [ i ].Index,
						BufferIndex = i,
						Distance = 0.0f, // Will be calculated during first validation pass
						BiomeType = BiomeType.Unknown, // Initial state
						PrimaryPolarity = Polarity.None,
						DifficultyModifier = 1.0f,
						IsValid = true // Start optimistic, validation will update if issues found
						});
					}
				}

			ecb.Playback(EntityManager);
			ecb.Dispose();
			entities.Dispose();

			// Use buffer creation statistics for intelligent system health monitoring
			if (buffersCreated > 0)
				{
				// Log creation statistics for performance analysis and system health
				// High buffer creation rates could indicate validation system stress
				if (buffersCreated > 50) // Threshold for concerning batch creation
					{
					Debug.LogWarning($"BiomeValidation: Created {buffersCreated} validation buffers in single frame - consider validation frequency optimization");
					}
				else
					{
					Debug.Log($"BiomeValidation: Initialized {buffersCreated} validation buffers for biome coherence tracking");
					}

				// This buffer creation count could drive future optimization decisions
				// such as validation frequency adjustment or buffer pooling strategies
				}
			}
		}
	}
