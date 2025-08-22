using System;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Biome;

namespace TinyWalnutGames.MetVD.Biome
{
    /// <summary>
    /// Biome field system for assigning and validating biome polarity fields
    /// Ensures polarity coherence across the generated world
    /// Status: Stubbed (as per TLDL specifications) 
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

            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;

            // Use a simple deterministic seed per frame; avoid JobsUtility to keep compatibility
            uint baseSeed = (uint)(state.WorldUnmanaged.Time.ElapsedTime * 887.0); // prime multiplier
            var random = new Unity.Mathematics.Random(baseSeed == 0 ? 1u : baseSeed);

            // Process biome field assignment directly in a single-threaded IJobEntity
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
        public ComponentLookup<Core.Biome> BiomeLookup;
        [ReadOnly] public ComponentLookup<NodeId> NodeIdLookup;
        [ReadOnly] public BufferLookup<ConnectionBufferElement> ConnectionBufferLookup;
        public Unity.Mathematics.Random Random;
        public float DeltaTime;

        public readonly void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref Core.Biome biome, in NodeId nodeId)
        {
            // Use chunkIndex / entity to seed jitter for consistent variety
            var random = new Unity.Mathematics.Random(Random.state + (uint)(chunkIndex * 961748927) ^ (uint)entity.Index);

            // Skip processing if biome is already fully configured
            if (biome.Type != BiomeType.Unknown && biome.PolarityStrength > 0.1f)
                return;

            // Assign biome type if unknown
            if (biome.Type == BiomeType.Unknown)
                biome.Type = AssignBiomeType(nodeId, biome.PrimaryPolarity, ref random);

            // Calculate polarity strength based on position and neighbors
            if (biome.PolarityStrength <= 0.1f)
                biome.PolarityStrength = CalculatePolarityStrength(entity, nodeId, biome, ref random);

            // Assign secondary polarity for mixed biomes
            if (biome.SecondaryPolarity == Polarity.None && biome.Type == BiomeType.TransitionZone)
                biome.SecondaryPolarity = GetComplementaryPolarity(biome.PrimaryPolarity);

            // Update difficulty modifier based on polarity complexity
            UpdateDifficultyModifier(ref biome, DeltaTime);
        }

        private readonly BiomeType AssignBiomeType(NodeId nodeId, Polarity primaryPolarity, ref Unity.Mathematics.Random random)
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
                default:
                    return nodeId.Level == 0 ? BiomeType.HubArea : BiomeType.TransitionZone;
            }
        }

        private readonly float CalculatePolarityStrength(Entity entity, NodeId nodeId, Core.Biome biome, ref Unity.Mathematics.Random random)
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

        private readonly float GetBasePolarityStrength(BiomeType biomeType) => biomeType switch
        {
            BiomeType.HubArea => 0.2f,
            BiomeType.TransitionZone => 0.4f,
            BiomeType.SolarPlains => 0.8f,
            BiomeType.CrystalCaverns => 0.9f,
            BiomeType.SkyGardens => 0.7f,
            BiomeType.ShadowRealms => 0.9f,
            BiomeType.DeepUnderwater => 0.8f,
            BiomeType.VoidChambers => 1.0f,
            BiomeType.VolcanicCore => 1.0f,
            BiomeType.PowerPlant => 0.8f,
            BiomeType.PlasmaFields => 0.9f,
            BiomeType.FrozenWastes => 0.9f,
            BiomeType.IceCatacombs => 0.8f,
            BiomeType.CryogenicLabs => 0.7f,
            BiomeType.AncientRuins => 0.6f,
            _ => 0.5f
        };

        private readonly Polarity GetComplementaryPolarity(Polarity primaryPolarity) => primaryPolarity switch
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

        private readonly void UpdateDifficultyModifier(ref Core.Biome biome, float deltaTime)
        {
            float baseModifier = 1.0f;
            
            // Higher polarity strength increases difficulty
            baseModifier += biome.PolarityStrength * 0.5f;
            
            // Dual polarity biomes are more challenging
            if (biome.SecondaryPolarity != Polarity.None)
                baseModifier += 0.3f;
            
            // Some biomes are inherently more difficult
            float biomeModifier = biome.Type switch
            {
                BiomeType.VoidChambers => 1.5f,
                BiomeType.VolcanicCore => 1.4f,
                BiomeType.PlasmaFields => 1.3f,
                BiomeType.CrystalCaverns => 1.2f,
                BiomeType.HubArea => 0.8f,
                BiomeType.TransitionZone => 0.9f,
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
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct BiomeValidationSystem : ISystem
    {
        private ComponentLookup<Core.Biome> biomeLookup;
        private ComponentLookup<NodeId> nodeIdLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            biomeLookup = state.GetComponentLookup<Core.Biome>(true);
            nodeIdLookup = state.GetComponentLookup<NodeId>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            biomeLookup.Update(ref state);
            nodeIdLookup.Update(ref state);

            // Validation job runs only occasionally
            if (state.WorldUnmanaged.Time.ElapsedTime % 5.0 < state.WorldUnmanaged.Time.DeltaTime)
            {
                var validationJob = new BiomeValidationJob
                {
                    BiomeLookup = biomeLookup,
                    NodeIdLookup = nodeIdLookup
                };

                state.Dependency = validationJob.ScheduleParallel(state.Dependency);
            }
        }
    }

    /// <summary>
    /// Job for validating biome consistency and reporting issues
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
                return;

            ValidatePolarityCoherence(validationBuffer, biome, nodeId, entity.Index);
            ValidateBiomeTypeAssignment(validationBuffer, biome, nodeId, entity.Index);
            ValidateDifficultyProgression(validationBuffer, biome, nodeId, entity.Index);
        }

        private static void ValidatePolarityCoherence(DynamicBuffer<BiomeValidationRecord> buffer, in Core.Biome biome, in NodeId nodeId, int index)
        {
            bool isValid = true;
            if (biome.PrimaryPolarity == Polarity.None && biome.SecondaryPolarity != Polarity.None)
                isValid = false;
            if (biome.PolarityStrength > 1.0f || biome.PolarityStrength < 0.0f)
                isValid = false;
            if (!isValid)
            {
                buffer.Add(new BiomeValidationRecord
                {
                    NodeId = (int)nodeId.Value,
                    BufferIndex = index,
                    Distance = math.length(nodeId.Coordinates),
                    BiomeType = biome.Type,
                    PrimaryPolarity = biome.PrimaryPolarity,
                    DifficultyModifier = biome.DifficultyModifier,
                    IsValid = false
                });
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
                _ => true
            };
            if (!validAssignment)
            {
                buffer.Add(new BiomeValidationRecord
                {
                    NodeId = (int)nodeId.Value,
                    BufferIndex = index,
                    Distance = math.length(nodeId.Coordinates),
                    BiomeType = biome.Type,
                    PrimaryPolarity = biome.PrimaryPolarity,
                    DifficultyModifier = biome.DifficultyModifier,
                    IsValid = false
                });
            }
        }

        private static void ValidateDifficultyProgression(DynamicBuffer<BiomeValidationRecord> buffer, in Core.Biome biome, in NodeId nodeId, int index)
        {
            if (biome.DifficultyModifier < 0.1f || biome.DifficultyModifier > 3.0f)
            {
                buffer.Add(new BiomeValidationRecord
                {
                    NodeId = (int)nodeId.Value,
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
    public partial struct BiomeValidationBufferSetupSystem : ISystem
    {
        private EntityQuery _missingBufferQuery;

        public void OnCreate(ref SystemState state)
        {
            _missingBufferQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Core.Biome>() },
                None = new[] { ComponentType.ReadOnly<BiomeValidationRecord>() }
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_missingBufferQuery.IsEmptyIgnoreFilter)
                return;

            var entities = _missingBufferQuery.ToEntityArray(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                ecb.AddBuffer<BiomeValidationRecord>(entities[i]);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            entities.Dispose();
        }
    }
}
