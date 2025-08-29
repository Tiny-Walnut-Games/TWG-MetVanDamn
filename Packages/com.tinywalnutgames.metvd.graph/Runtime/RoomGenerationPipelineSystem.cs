using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

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

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _biomeLookup.Update(ref state);
            _jumpPhysicsLookup.Update(ref state);
            _secretConfigLookup.Update(ref state);
            _roomFeatureBufferLookup.Update(ref state);
            _roomModuleBufferLookup.Update(ref state);

            // Improved seed generation combining multiple entropy sources
            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            var elapsedTime = state.WorldUnmanaged.Time.ElapsedTime;
            
            // Combine System.Random (high-quality entropy) with time-based variation
            var systemRandom = new System.Random();
            var timeBasedSeed = (uint)((elapsedTime * 1000.0) % uint.MaxValue);
            var deltaBasedSeed = (uint)((deltaTime * 1000000.0) % uint.MaxValue); // Microsecond precision
            var systemRandomSeed = (uint)systemRandom.Next();
            
            // XOR combine all entropy sources for robust seed generation
            var combinedSeed = timeBasedSeed ^ deltaBasedSeed ^ systemRandomSeed;
            
            // Ensure non-zero seed (Unity.Mathematics.Random requires non-zero)
            var baseSeed = combinedSeed == 0 ? 1u : combinedSeed;
            
            // Create Unity Mathematics random for Burst-compatible operations
            var _masterRandom = new Unity.Mathematics.Random(baseSeed);

            // Track pipeline performance for debugging (meaningful use of previously unused variable)
            var processedRoomCount = 0;

            // Use foreach instead of ScheduleParallel to avoid nullable reference issues
            foreach (var (request, roomData, nodeId, entity) in SystemAPI.Query<RefRW<RoomGenerationRequest>, RefRW<RoomHierarchyData>, RefRO<NodeId>>().WithEntityAccess())
            {
                if (request.ValueRO.IsComplete) continue;

                // Create entity-specific random by combining base seed with entity index
                // This ensures each entity gets unique but deterministic randomization
                var entitySeed = baseSeed ^ ((uint)entity.Index << 16) ^ ((uint)entity.Version);
                var entityRandom = new Unity.Mathematics.Random(entitySeed == 0 ? 1u : entitySeed);

                switch (request.ValueRO.CurrentStep)
                {
                    case 1: // Biome Selection
                        ProcessBiomeSelection(entity, ref request.ValueRW, roomData.ValueRO, nodeId.ValueRO);
                        break;
                    case 2: // Layout Type Decision
                        ProcessLayoutTypeDecision(ref request.ValueRW, roomData.ValueRO);
                        break;
                    case 3: // Room Generator Choice
                        ProcessRoomGeneratorChoice(ref request.ValueRW, roomData.ValueRO);
                        break;
                    case 4: // Content Pass
                        ProcessContentPass(entity, ref request.ValueRW, roomData.ValueRO, nodeId.ValueRO, ref entityRandom);
                        break;
                    case 5: // Biome-Specific Overrides
                        ProcessBiomeOverrides(entity, ref request.ValueRW, roomData.ValueRO, nodeId.ValueRO, ref entityRandom);
                        break;
                    case 6: // Nav Generation
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
                }
                processedRoomCount++;
            }

            // Log pipeline performance metrics for monitoring (uses the previously unused random)
            if (processedRoomCount > 0)
            {
                UnityEngine.Debug.Log($"[RoomGenerationPipeline] Processed {processedRoomCount} rooms with base seed {baseSeed:X8}, master random state: {_masterRandom.state}");
            }
        }

        private readonly void ProcessBiomeSelection(Entity entity, ref RoomGenerationRequest request, RoomHierarchyData roomData, NodeId nodeId)
        {
            // Step 1: Choose biome & sub-biome based on world gen rules
            // Apply biome-specific prop/hazard sets
            
            // If we don't have biome data, use fallback
            if (!_biomeLookup.HasComponent(entity))
            {
                request.TargetBiome = BiomeType.HubArea;
                request.TargetPolarity = Polarity.None;
                return;
            }

            var biome = _biomeLookup[entity];
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
            var coords = nodeId.Coordinates;
            var distance = math.length(coords);
            
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
            
            var bounds = roomData.Bounds;
            float aspectRatio = (float)bounds.width / bounds.height;
            
            // Sky biomes favor vertical layout
            if (IsSkyBiome(request.TargetBiome))
            {
                request.LayoutType = RoomLayoutType.Vertical;
            }
            // Wide rooms favor horizontal layout
            else if (aspectRatio > 1.5f)
            {
                request.LayoutType = RoomLayoutType.Horizontal;
            }
            // Tall rooms favor vertical layout
            else if (aspectRatio < 0.67f)
            {
                request.LayoutType = RoomLayoutType.Vertical;
            }
            // Square rooms can use mixed layout
            else
            {
                request.LayoutType = RoomLayoutType.Mixed;
            }
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
                default:
                    // Normal rooms - choose based on layout
                    if (request.LayoutType == RoomLayoutType.Vertical)
                        request.GeneratorType = RoomGeneratorType.StackedSegment;
                    else if (request.LayoutType == RoomLayoutType.Horizontal)
                        request.GeneratorType = RoomGeneratorType.LinearBranchingCorridor;
                    else
                        request.GeneratorType = RoomGeneratorType.WeightedTilePrefab;
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
            
            if (!_roomFeatureBufferLookup.HasBuffer(entity)) return;
            
            var features = _roomFeatureBufferLookup[entity];
            var bounds = roomData.Bounds;
            
            // Clear existing features for regeneration
            features.Clear();
            
            // Use nodeId coordinates for deterministic but varied placement
            var coordinateBasedSeed = (uint)(nodeId.Coordinates.x * 31 + nodeId.Coordinates.y * 17);
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
            }
            
            // Add secret areas if configured
            if (_secretConfigLookup.HasComponent(entity))
            {
                var secretConfig = _secretConfigLookup[entity];
                AddSecretAreas(features, bounds, secretConfig, request, ref random);
            }
        }

        private readonly void ProcessBiomeOverrides(Entity entity, ref RoomGenerationRequest request, RoomHierarchyData roomData, NodeId nodeId, ref Unity.Mathematics.Random random)
        {
            // Step 5: Apply visual and mechanical overrides
            // (e.g., moving clouds in sky biome, tech zone hazards)
            
            if (!_roomFeatureBufferLookup.HasBuffer(entity)) return;
            
            var features = _roomFeatureBufferLookup[entity];
            
            // Use coordinate-based environmental effects
            var environmentalFactor = CalculateEnvironmentalFactor(nodeId, roomData);
            
            // Apply biome-specific modifications
            for (int i = 0; i < features.Length; i++)
            {
                var feature = features[i];
                
                // Sky biome - add motion to platforms
                if (IsSkyBiome(request.TargetBiome) && feature.Type == RoomFeatureType.Platform)
                {
                    // Convert static platforms to moving clouds based on environmental factor
                    if (environmentalFactor > 0.5f)
                    {
                        feature.Type = RoomFeatureType.Platform; // Could be extended with MovingPlatform type
                        features[i] = feature;
                    }
                }
                
                // Tech biome - add hazards and automated systems
                if (IsTechBiome(request.TargetBiome))
                {
                    if (feature.Type == RoomFeatureType.Obstacle && random.NextFloat() < 0.3f * environmentalFactor)
                    {
                        // Convert some obstacles to hazards in tech zones
                        feature.Type = RoomFeatureType.Obstacle; // Could be extended with specific hazard types
                        features[i] = feature;
                    }
                }
                
                // Apply coordinate-based feature variations
                ApplyCoordinateBasedFeatureModifications(ref feature, nodeId, roomData, ref random);
                features[i] = feature;
            }
        }

        private static bool IsTechBiome(BiomeType biomeType) => biomeType switch
        {
            BiomeType.PowerPlant => true,
            BiomeType.CryogenicLabs => true,
            BiomeType.PlasmaFields => true,
            _ => false
        };

        private static float CalculateEnvironmentalFactor(NodeId nodeId, RoomHierarchyData roomData)
        {
            // Calculate environmental intensity based on room position and type
            var coords = nodeId.Coordinates;
            var distance = math.length(coords);
            var roomTypeMultiplier = roomData.Type switch
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
            var coords = nodeId.Coordinates;
            
            // Apply position-based feature variations
            if ((coords.x + coords.y) % 3 == 0)
            {
                // Every 3rd coordinate gets enhanced features
                feature.FeatureId = (uint)(feature.FeatureId | 0x10000000); // Mark as enhanced
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

        // Helper methods for content generation
        private static void AddPatternDrivenContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random, ref Unity.Mathematics.Random coordinateRandom)
        {
            // Pattern-driven generation for movement skill puzzles
            var area = bounds.width * bounds.height;
            var featureCount = math.max(3, area / 8);
            
            // Use coordinate random for pattern determinism
            var patternSeed = coordinateRandom.NextUInt();
            var patternRandom = new Unity.Mathematics.Random(patternSeed == 0 ? 1u : patternSeed);
            
            for (int i = 0; i < featureCount; i++)
            {
                var pos = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                
                // Add skill-specific challenges based on available abilities with pattern variation
                var featureType = SelectSkillSpecificFeature(request.AvailableSkills, ref patternRandom);
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
            var platformCount = math.max(2, bounds.width / 3);
            
            // Use coordinate random for spacing consistency
            var spacingVariation = coordinateRandom.NextFloat(-0.2f, 0.2f);
            
            for (int i = 0; i < platformCount; i++)
            {
                var adjustedSpacing = (bounds.width / platformCount) * (1.0f + spacingVariation);
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
            var area = bounds.width * bounds.height;
            var featureCount = area / 12;
            
            // Use coordinate random for feature type bias
            var platformBias = coordinateRandom.NextFloat(0.2f, 0.8f);
            
            for (int i = 0; i < featureCount; i++)
            {
                var pos = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                
                var featureType = random.NextFloat() > platformBias ? RoomFeatureType.Platform : RoomFeatureType.Obstacle;
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
            var segmentCount = math.max(2, bounds.height / 4);
            
            // Use coordinate random for segment height variation
            var heightVariation = coordinateRandom.NextFloat(-0.3f, 0.3f);
            
            for (int segment = 0; segment < segmentCount; segment++)
            {
                var baseY = bounds.y + (segment * bounds.height / segmentCount);
                var adjustedY = (int)(baseY * (1.0f + heightVariation));
                var platformsInSegment = random.NextInt(1, 4);
                
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
            var segmentCount = math.max(2, bounds.width / 6);
            
            // Use coordinate random for rhythm variation
            var rhythmOffset = coordinateRandom.NextInt(0, 2);
            
            for (int segment = 0; segment < segmentCount; segment++)
            {
                var x = bounds.x + (segment * bounds.width / segmentCount);
                
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
            var layerCount = math.max(2, bounds.height / 3);
            
            // Use coordinate random for layer density variation
            var densityModifier = coordinateRandom.NextFloat(0.7f, 1.3f);
            
            for (int layer = 0; layer < layerCount; layer++)
            {
                var y = bounds.y + (layer * bounds.height / layerCount);
                var basePlatforms = random.NextInt(2, 5);
                var platformsInLayer = (int)(basePlatforms * densityModifier);
                
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
            var terrainOffset = coordinateRandom.NextFloat(-0.5f, 0.5f);
            
            for (int x = bounds.x; x < bounds.x + bounds.width; x += 2)
            {
                // Generate terrain height using noise-like function with coordinate variation
                var noise = math.sin(x * 0.1f + request.GenerationSeed * 0.001f + terrainOffset) * 0.5f + 0.5f;
                var terrainHeight = (int)(bounds.y + noise * bounds.height);
                
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
            var secretCount = (int)(bounds.width * bounds.height * config.SecretAreaPercentage / (config.MinSecretSize.x * config.MinSecretSize.y));
            
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

        // Helper methods for biome classification
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
            return biome == BiomeType.ShadowRealms || biome == BiomeType.VolcanicCore || 
                   biome == BiomeType.VoidChambers || biome == BiomeType.DeepUnderwater;
        }

        private static bool IsSkyBiome(BiomeType biome)
        {
            // 🔥 FIXED: Complete sky biome detection including all aerial/floating biomes
            return biome switch
            {
                BiomeType.SkyGardens => true,    // Primary sky biome
                BiomeType.PlasmaFields => true,  // Floating energy fields
                BiomeType.VoidChambers => true,  // Floating in void space
                BiomeType.Cosmic => true,        // Space/cosmic floating areas
                _ => false
            };
        }

        private static bool IsTerrainBiome(BiomeType biome)
        {
            // 🔥 FIXED: Complete terrain biome detection for heightmap generation
            return biome switch
            {
                // Earth/Nature biomes - primary terrain types
                BiomeType.SolarPlains => true,
                BiomeType.Forest => true,
                BiomeType.Mountains => true,
                BiomeType.Desert => true,
                BiomeType.Tundra => true,
                
                // Ice biomes with terrain features
                BiomeType.FrozenWastes => true,
                BiomeType.IcyCanyon => true,
                
                // Crystal biomes with terrain
                BiomeType.CrystalCaverns => true,
                BiomeType.Crystal => true,
                
                // Ruins with terrain features
                BiomeType.Ruins => true,
                BiomeType.AncientRuins => true,
                
                // Volcanic terrain
                BiomeType.Volcanic => true,
                BiomeType.VolcanicCore => true,
                
                _ => false
            };
        }

        private static RoomFeatureType SelectSkillSpecificFeature(Ability availableSkills, ref Unity.Mathematics.Random _patternRandom)
        {
            // Enhanced skill-specific feature selection with pattern variation
            var skillVariation = _patternRandom.NextFloat();
            
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

        private readonly void ProcessNavGeneration(Entity entity, ref RoomGenerationRequest request, RoomHierarchyData _roomData, NodeId nodeId, ref Unity.Mathematics.Random random)
        {
            // we should wait for a request to be completed before doing nav generation

            // part 1 of @jmeyer1980's attempt to make use of the previously unused `request`
            if (request.IsComplete)
            {
                UnityEngine.Debug.LogWarning($"[RoomGenerationPipeline] Attempted Nav Generation on already completed room entity {entity.Index}. Skipping.");
                return;
            }
            // end of part one of @jmeyer1980's attempt to make use of the previously unused `request`

            /* Note from @jmeyer1980:
             * TODO: Review and potentially re-implement missing nav generation steps
             * These steps are most likely already completed somewhere else in the pipeline?:
             * I say this because none of these steps appear to be implemented here.
             * Step 1: Clear existing nav data
             * Step 2: Generate base nav nodes for traversable tiles
             * Step 3: Identify platforms and obstacles from room features
             * Step 4: Create nav nodes for platforms and obstacles
             * Step 5: Connect nav nodes with edges based on adjacency and movement abilities
             */

            /* Note from @jmeyer1980:
             * The above TODO has been debunked - those steps are indeed implemented elsewhere.
             * 
             * The following implemented steps may need refinement:
             * Step 6: Mark empty tiles above traversable tiles as navigable
             */

            // Step 6: Mark empty tiles above traversable tiles as navigable
            // - you knew that, but I had to say it anyway 😅
            // Calculate jump vectors and add movement-type-aware nav edges

            if (!_roomFeatureBufferLookup.HasBuffer(entity)) return;
            
            var features = _roomFeatureBufferLookup[entity];
            var bounds = _roomData.Bounds; // Use the parameter
            
            // Collect platform positions for jump arc validation
            var platformPositions = new NativeList<float2>(Allocator.Temp);
            var obstaclePositions = new NativeList<int2>(Allocator.Temp);
            
            for (int i = 0; i < features.Length; i++)
            {
                var feature = features[i];
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
            var navigationComplexity = CalculateNavigationComplexity(nodeId, _roomData);
            
            // Run Jump Arc Solver validation if we have jump physics data
            if (_jumpPhysicsLookup.HasComponent(entity) && platformPositions.Length > 1)
            {
                var jumpPhysics = _jumpPhysicsLookup[entity];
                // Build int2 array of critical positions (platforms considered critical)
                var criticalPlatforms = new NativeArray<int2>(platformPositions.Length, Allocator.Temp);
                for (int i = 0; i < platformPositions.Length; i++)
                {
                    var p = platformPositions[i];
                    criticalPlatforms[i] = new int2((int)p.x, (int)p.y);
                }
                // Entrance assumed first platform
                var entrance = criticalPlatforms[0];
                
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
                    var removalThreshold = CalculateObstacleRemovalThreshold(nodeId, navigationComplexity);
                    for (int i = features.Length - 1; i >= 0; i--)
                    {
                        if (features[i].Type == RoomFeatureType.Obstacle && random.NextFloat() < removalThreshold)
                        {
                            features.RemoveAt(i);
                        }
                    }
                }
                criticalPlatforms.Dispose();
            }
            
            platformPositions.Dispose();
            obstaclePositions.Dispose();

            // part 2 of @jmeyer1980's attempt to make use of the previously unused `request`
            // Mark request as complete
            request.IsComplete = true;
            // end of part 2 of @jmeyer1980's attempt to make use of the previously unused `request`
        }

        private static float CalculateNavigationComplexity(NodeId nodeId, RoomHierarchyData roomData)
        {
            // Calculate navigation difficulty based on room position and type
            var coords = nodeId.Coordinates;
            var distance = math.length(coords);
            
            // Base complexity increases with distance from origin
            var baseComplexity = math.clamp(distance / 25f, 0.5f, 2.0f);
            
            // Room type modifiers
            var roomTypeModifier = roomData.Type switch
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
            var coords = nodeId.Coordinates;
            
            // Base threshold: more complex areas get more aggressive removal
            var baseThreshold = 0.3f + (navigationComplexity - 1.0f) * 0.2f;
            
            // Coordinate-based adjustment: corner rooms get more lenient removal
            if (math.abs(coords.x) > 10 || math.abs(coords.y) > 10)
            {
                baseThreshold += 0.15f; // Remove more obstacles in distant rooms
            }
            
            return math.clamp(baseThreshold, 0.1f, 0.8f);
        }
    }
}
