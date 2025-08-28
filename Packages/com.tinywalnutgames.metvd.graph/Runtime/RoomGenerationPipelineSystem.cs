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

            var deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            uint baseSeed = (uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000.0);
            var random = new Unity.Mathematics.Random(baseSeed == 0 ? 1u : baseSeed);

            // Use foreach instead of ScheduleParallel to avoid nullable reference issues
            foreach (var (request, roomData, nodeId, entity) in SystemAPI.Query<RefRW<RoomGenerationRequest>, RefRW<RoomHierarchyData>, RefRO<NodeId>>().WithEntityAccess())
            {
                if (request.ValueRO.IsComplete) continue;

                var entityRandom = new Unity.Mathematics.Random(random.state + (uint)entity.Index);

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
            var area = bounds.width * bounds.height;
            
            // Clear existing features for regeneration
            features.Clear();
            
            // Add content based on generator type
            switch (request.GeneratorType)
            {
                case RoomGeneratorType.PatternDrivenModular:
                    AddPatternDrivenContent(features, bounds, request, ref random);
                    break;
                case RoomGeneratorType.ParametricChallenge:
                    AddParametricChallengeContent(features, bounds, request, ref random);
                    break;
                case RoomGeneratorType.WeightedTilePrefab:
                    AddWeightedContent(features, bounds, request, ref random);
                    break;
                case RoomGeneratorType.StackedSegment:
                    AddStackedSegmentContent(features, bounds, request, ref random);
                    break;
                case RoomGeneratorType.LinearBranchingCorridor:
                    AddLinearCorridorContent(features, bounds, request, ref random);
                    break;
                case RoomGeneratorType.LayeredPlatformCloud:
                    AddLayeredPlatformContent(features, bounds, request, ref random);
                    break;
                case RoomGeneratorType.BiomeWeightedHeightmap:
                    AddHeightmapContent(features, bounds, request, ref random);
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
            
            // Apply biome-specific modifications
            for (int i = 0; i < features.Length; i++)
            {
                var feature = features[i];
                
                // Sky biome - add motion to platforms
                if (IsSkyBiome(request.TargetBiome) && feature.Type == RoomFeatureType.Platform)
                {
                    // Convert static platforms to moving clouds
                    feature.Type = RoomFeatureType.Platform; // Could be extended with MovingPlatform type
                    features[i] = feature;
                }
                
                // Tech biome - add hazards and automated systems
                if (request.TargetBiome == BiomeType.PowerPlant || request.TargetBiome == BiomeType.CryogenicLabs)
                {
                    if (feature.Type == RoomFeatureType.Obstacle && random.NextFloat() < 0.3f)
                    {
                        // Convert some obstacles to hazards in tech zones
                        feature.Type = RoomFeatureType.Obstacle; // Could be extended with specific hazard types
                        features[i] = feature;
                    }
                }
            }
        }

        private readonly void ProcessNavGeneration(Entity entity, ref RoomGenerationRequest request, RoomHierarchyData roomData, NodeId nodeId, ref Unity.Mathematics.Random random)
        {
            // Step 6: Mark empty tiles above traversable tiles as navigable
            // Calculate jump vectors and add movement-type-aware nav edges
            
            if (!_roomFeatureBufferLookup.HasBuffer(entity)) return;
            
            var features = _roomFeatureBufferLookup[entity];
            var bounds = roomData.Bounds;
            
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
            
            // Run Jump Arc Solver validation if we have jump physics data
            if (_jumpPhysicsLookup.HasComponent(entity) && platformPositions.Length > 1)
            {
                var jumpPhysics = _jumpPhysicsLookup[entity];
                // Build int2 array of critical positions (platforms considered critical)
                var critical = new NativeArray<int2>(platformPositions.Length, Allocator.Temp);
                for (int i = 0; i < platformPositions.Length; i++)
                {
                    var p = platformPositions[i];
                    critical[i] = new int2((int)p.x, (int)p.y);
                }
                // Entrance assumed first platform
                var entrance = critical[0];
                
                // Convert JumpPhysicsData to JumpArcPhysics for the solver
                var arcPhysics = new JumpArcPhysics(
                    jumpPhysics.JumpHeight,
                    jumpPhysics.JumpDistance,
                    jumpPhysics.HasDoubleJump ? 1.5f : 1.0f,
                    jumpPhysics.GravityScale,
                    jumpPhysics.HasWallJump ? jumpPhysics.JumpHeight * 0.8f : 0.0f,
                    jumpPhysics.HasGlide ? 6.0f : 4.0f
                );
                
                bool allReachable = JumpArcSolver.ValidateRoomReachability(entrance, critical, Ability.Jump | Ability.DoubleJump, arcPhysics, new RectInt(0,0,bounds.width,bounds.height), Allocator.Temp);
                if (!allReachable && features.Length > 2)
                {
                    for (int i = features.Length - 1; i >= 0; i--)
                    {
                        if (features[i].Type == RoomFeatureType.Obstacle && random.NextFloat() < 0.3f)
                        {
                            features.RemoveAt(i);
                        }
                    }
                }
                critical.Dispose();
            }
            
            platformPositions.Dispose();
            obstaclePositions.Dispose();
        }

        // Helper methods for content generation
        private static void AddPatternDrivenContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random)
        {
            // Pattern-driven generation for movement skill puzzles
            var area = bounds.width * bounds.height;
            var featureCount = math.max(3, area / 8);
            
            for (int i = 0; i < featureCount; i++)
            {
                var pos = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                
                // Add skill-specific challenges based on available abilities
                var featureType = SelectSkillSpecificFeature(request.AvailableSkills);
                features.Add(new RoomFeatureElement
                {
                    Type = featureType,
                    Position = pos,
                    FeatureId = (uint)(request.GenerationSeed + i)
                });
            }
        }

        private static void AddParametricChallengeContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random)
        {
            // Parametric generation tuned for testing specific skills
            var platformCount = math.max(2, bounds.width / 3);
            
            for (int i = 0; i < platformCount; i++)
            {
                var pos = new int2(
                    bounds.x + (i * bounds.width / platformCount),
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

        private static void AddWeightedContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random)
        {
            // Standard weighted generation with optional secrets
            var area = bounds.width * bounds.height;
            var featureCount = area / 12;
            
            for (int i = 0; i < featureCount; i++)
            {
                var pos = new int2(
                    random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                    random.NextInt(bounds.y + 1, bounds.y + bounds.height - 1)
                );
                
                var featureType = random.NextFloat() > 0.7f ? RoomFeatureType.Platform : RoomFeatureType.Obstacle;
                features.Add(new RoomFeatureElement
                {
                    Type = featureType,
                    Position = pos,
                    FeatureId = (uint)(request.GenerationSeed + i)
                });
            }
        }

        private static void AddStackedSegmentContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random)
        {
            // Vertical stacked segments for climb/jump routes
            var segmentCount = math.max(2, bounds.height / 4);
            
            for (int segment = 0; segment < segmentCount; segment++)
            {
                var y = bounds.y + (segment * bounds.height / segmentCount);
                var platformsInSegment = random.NextInt(1, 4);
                
                for (int p = 0; p < platformsInSegment; p++)
                {
                    var pos = new int2(
                        random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1),
                        y + random.NextInt(0, bounds.height / segmentCount)
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

        private static void AddLinearCorridorContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random)
        {
            // Horizontal corridor with rhythm pacing
            var segmentCount = math.max(2, bounds.width / 6);
            
            for (int segment = 0; segment < segmentCount; segment++)
            {
                var x = bounds.x + (segment * bounds.width / segmentCount);
                
                // Alternate between challenge and rest beats
                if (segment % 2 == 0) // Challenge beat
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

        private static void AddLayeredPlatformContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random)
        {
            // Layered platforms for sky biomes with motion patterns
            var layerCount = math.max(2, bounds.height / 3);
            
            for (int layer = 0; layer < layerCount; layer++)
            {
                var y = bounds.y + (layer * bounds.height / layerCount);
                var platformsInLayer = random.NextInt(2, 5);
                
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

        private static void AddHeightmapContent(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, RoomGenerationRequest request, ref Unity.Mathematics.Random random)
        {
            // Heightmap-based terrain generation for overworld biomes
            for (int x = bounds.x; x < bounds.x + bounds.width; x += 2)
            {
                // Generate terrain height using noise-like function
                var noise = math.sin(x * 0.1f + request.GenerationSeed * 0.001f) * 0.5f + 0.5f;
                var terrainHeight = (int)(bounds.y + noise * bounds.height);
                
                features.Add(new RoomFeatureElement
                {
                    Type = RoomFeatureType.Platform,
                    Position = new int2(x, terrainHeight),
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
            return biome == BiomeType.SkyGardens || biome == BiomeType.PlasmaFields;
        }

        private static bool IsTerrainBiome(BiomeType biome)
        {
            return biome == BiomeType.SolarPlains || biome == BiomeType.FrozenWastes || 
                   biome == BiomeType.CrystalCaverns;
        }

        private static RoomFeatureType SelectSkillSpecificFeature(Ability availableSkills)
        {
            if ((availableSkills & Ability.Dash) != 0) return RoomFeatureType.Obstacle;
            if ((availableSkills & Ability.WallJump) != 0) return RoomFeatureType.Platform;
            if ((availableSkills & Ability.Grapple) != 0) return RoomFeatureType.Platform;
            return RoomFeatureType.Platform;
        }
    }
}
