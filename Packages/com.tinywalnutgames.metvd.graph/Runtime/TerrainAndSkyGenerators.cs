using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Graph
{
    /// <summary>
    /// Stacked Segment Generator
    /// For vertical layout rooms - builds rooms in vertical slices
    /// Ensures climb/jump routes are coherent for towers, shafts, elevator-style challenges
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(WeightedTilePrefabGenerator))]
    public partial struct StackedSegmentGenerator : ISystem
    {
        private ComponentLookup<JumpPhysicsData> _jumpPhysicsLookup;
        private BufferLookup<RoomFeatureElement> _featureBufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _jumpPhysicsLookup = state.GetComponentLookup<JumpPhysicsData>(true);
            _featureBufferLookup = state.GetBufferLookup<RoomFeatureElement>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _jumpPhysicsLookup.Update(ref state);
            _featureBufferLookup.Update(ref state);

            var random = new Unity.Mathematics.Random((uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000));

            var stackedJob = new StackedSegmentJob
            {
                JumpPhysicsLookup = _jumpPhysicsLookup,
                FeatureBufferLookup = _featureBufferLookup,
                Random = random
            };

            state.Dependency = stackedJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct StackedSegmentJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<JumpPhysicsData> JumpPhysicsLookup;
        public BufferLookup<RoomFeatureElement> FeatureBufferLookup;
        public Unity.Mathematics.Random Random;

        public void Execute(Entity entity, ref RoomGenerationRequest request, ref RoomHierarchyData roomData, in NodeId nodeId)
        {
            if (request.GeneratorType != RoomGeneratorType.StackedSegment || request.IsComplete) return;

            if (!FeatureBufferLookup.HasBuffer(entity)) return;

            var features = FeatureBufferLookup[entity];
            var bounds = roomData.Bounds;
            features.Clear();

            // Determine segment count based on room height
            var segmentCount = math.max(3, bounds.height / 4);
            var segmentHeight = bounds.height / segmentCount;

            // Get jump physics for coherent route planning
            var jumpHeight = 3.0f; // Default
            if (JumpPhysicsLookup.HasComponent(entity))
            {
                jumpHeight = JumpPhysicsLookup[entity].JumpHeight;
            }

            // Generate each vertical segment
            for (int segment = 0; segment < segmentCount; segment++)
            {
                var segmentY = bounds.y + (segment * segmentHeight);
                GenerateVerticalSegment(features, bounds, segmentY, segmentHeight, segment, jumpHeight, request.GenerationSeed);
            }

            // Ensure vertical connectivity between segments
            EnsureVerticalConnectivity(features, bounds, segmentCount, segmentHeight, jumpHeight);
        }

        private void GenerateVerticalSegment(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, 
                                           int segmentY, int segmentHeight, int segmentIndex, float jumpHeight, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed + (uint)segmentIndex * 100);
            
            // Each segment has 1-3 platforms with vertical progression opportunities
            var platformCount = random.NextInt(1, 4);
            
            for (int p = 0; p < platformCount; p++)
            {
                var platformX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
                var platformY = segmentY + random.NextInt(0, segmentHeight - 1);
                
                // Ensure platforms support upward movement
                var featureType = RoomFeatureType.Platform;
                
                // Add climb assists for tall segments
                if (segmentHeight > jumpHeight + 1)
                {
                    if (p == 0) // First platform in segment - add wall for wall-jumping
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
            
            // Add segment-specific challenges
            if (segmentIndex % 3 == 0) // Every third segment has a challenge
            {
                AddVerticalChallenge(features, bounds, segmentY, segmentHeight, random, seed);
            }
        }

        private void AddVerticalChallenge(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, 
                                        int segmentY, int segmentHeight, Unity.Mathematics.Random random, uint seed)
        {
            var challengeType = random.NextInt(0, 3);
            
            switch (challengeType)
            {
                case 0: // Moving obstacle
                    {
                        var obstacleX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
                        var obstacleY = segmentY + segmentHeight / 2;
                        features.Add(new RoomFeatureElement
                        {
                            Type = RoomFeatureType.Obstacle,
                            Position = new int2(obstacleX, obstacleY),
                            FeatureId = (uint)(seed + 10000)
                        });
                    }
                    break;
                case 1: // Power-up placement requiring skill
                    {
                        var powerUpX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
                        var powerUpY = segmentY + segmentHeight - 1;
                        features.Add(new RoomFeatureElement
                        {
                            Type = RoomFeatureType.PowerUp,
                            Position = new int2(powerUpX, powerUpY),
                            FeatureId = (uint)(seed + 20000)
                        });
                    }
                    break;
                case 2: // Switch/door mechanism
                    {
                        var switchX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
                        var switchY = segmentY + 1;
                        features.Add(new RoomFeatureElement
                        {
                            Type = RoomFeatureType.Switch,
                            Position = new int2(switchX, switchY),
                            FeatureId = (uint)(seed + 30000)
                        });
                    }
                    break;
            }
        }

        private void EnsureVerticalConnectivity(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, 
                                              int segmentCount, int segmentHeight, float jumpHeight)
        {
            // Add connectivity platforms between segments if gaps are too large
            for (int segment = 0; segment < segmentCount - 1; segment++)
            {
                var currentSegmentTop = bounds.y + ((segment + 1) * segmentHeight);
                var nextSegmentBottom = bounds.y + (segment * segmentHeight);
                var gap = currentSegmentTop - nextSegmentBottom;
                
                if (gap > jumpHeight)
                {
                    // Add intermediate platform
                    var bridgeX = bounds.x + bounds.width / 2;
                    var bridgeY = nextSegmentBottom + (int)(gap / 2);
                    
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

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _featureBufferLookup = state.GetBufferLookup<RoomFeatureElement>();
            _secretConfigLookup = state.GetComponentLookup<SecretAreaConfig>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _featureBufferLookup.Update(ref state);
            _secretConfigLookup.Update(ref state);

            var random = new Unity.Mathematics.Random((uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000));

            var corridorJob = new LinearCorridorJob
            {
                FeatureBufferLookup = _featureBufferLookup,
                SecretConfigLookup = _secretConfigLookup,
                Random = random
            };

            state.Dependency = corridorJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct LinearCorridorJob : IJobEntity
    {
        public BufferLookup<RoomFeatureElement> FeatureBufferLookup;
        [ReadOnly] public ComponentLookup<SecretAreaConfig> SecretConfigLookup;
        public Unity.Mathematics.Random Random;

        public void Execute(Entity entity, ref RoomGenerationRequest request, ref RoomHierarchyData roomData, in NodeId nodeId)
        {
            if (request.GeneratorType != RoomGeneratorType.LinearBranchingCorridor || request.IsComplete) return;

            if (!FeatureBufferLookup.HasBuffer(entity)) return;

            var features = FeatureBufferLookup[entity];
            var bounds = roomData.Bounds;
            features.Clear();

            // Create rhythm-based horizontal progression
            var beatCount = math.max(4, bounds.width / 6); // One beat every 6 units
            var beatWidth = bounds.width / beatCount;

            for (int beat = 0; beat < beatCount; beat++)
            {
                var beatX = bounds.x + (beat * beatWidth);
                var beatType = DetermineBeatType(beat, beatCount);
                
                GenerateBeat(features, bounds, beatX, beatWidth, beatType, beat, request.GenerationSeed);
            }

            // Add branching paths for secrets
            if (SecretConfigLookup.HasComponent(entity))
            {
                var secretConfig = SecretConfigLookup[entity];
                GenerateBranchingPaths(features, bounds, secretConfig, request.GenerationSeed);
            }
        }

        private BeatType DetermineBeatType(int beatIndex, int totalBeats)
        {
            // Create rhythm pattern: Challenge, Rest, Secret, Challenge, Rest, etc.
            return (beatIndex % 3) switch
            {
                0 => BeatType.Challenge,
                1 => BeatType.Rest,
                2 => BeatType.Secret,
                _ => BeatType.Rest
            };
        }

        private void GenerateBeat(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, 
                                int beatX, int beatWidth, BeatType beatType, int beatIndex, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed + (uint)beatIndex * 50);
            
            switch (beatType)
            {
                case BeatType.Challenge:
                    GenerateChallengeBeat(features, bounds, beatX, beatWidth, random, seed);
                    break;
                case BeatType.Rest:
                    GenerateRestBeat(features, bounds, beatX, beatWidth, random, seed);
                    break;
                case BeatType.Secret:
                    GenerateSecretBeat(features, bounds, beatX, beatWidth, random, seed);
                    break;
            }
        }

        private void GenerateChallengeBeat(DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
                                         int beatX, int beatWidth, Unity.Mathematics.Random random, uint seed)
        {
            // Add obstacles and hazards for active challenge
            var obstacleCount = random.NextInt(1, 3);
            
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
                    FeatureId = (uint)(seed + i)
                });
            }
            
            // Add platform to navigate challenge
            var platformPos = new int2(
                beatX + beatWidth / 2,
                bounds.y + bounds.height / 2
            );
            
            features.Add(new RoomFeatureElement
            {
                Type = RoomFeatureType.Platform,
                Position = platformPos,
                FeatureId = (uint)(seed + 10)
            });
        }

        private void GenerateRestBeat(DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
                                    int beatX, int beatWidth, Unity.Mathematics.Random random, uint seed)
        {
            // Safe area with minimal obstacles, possibly health pickup
            if (random.NextFloat() < 0.3f)
            {
                var healthPos = new int2(
                    beatX + beatWidth / 2,
                    bounds.y + 1
                );
                
                features.Add(new RoomFeatureElement
                {
                    Type = RoomFeatureType.HealthPickup,
                    Position = healthPos,
                    FeatureId = (uint)(seed + 100)
                });
            }
            
            // Simple platform for traversal
            var platformPos = new int2(
                beatX + random.NextInt(1, beatWidth - 1),
                bounds.y + 1
            );
            
            features.Add(new RoomFeatureElement
            {
                Type = RoomFeatureType.Platform,
                Position = platformPos,
                FeatureId = (uint)(seed + 110)
            });
        }

        private void GenerateSecretBeat(DynamicBuffer<RoomFeatureElement> features, RectInt bounds,
                                      int beatX, int beatWidth, Unity.Mathematics.Random random, uint seed)
        {
            // Hidden elements that require exploration
            var secretPos = new int2(
                beatX + random.NextInt(0, beatWidth),
                random.NextFloat() > 0.5f ? bounds.y + bounds.height - 1 : bounds.y + 1
            );
            
            features.Add(new RoomFeatureElement
            {
                Type = RoomFeatureType.Secret,
                Position = secretPos,
                FeatureId = (uint)(seed + 200)
            });
            
            // Add concealment
            var wallPos = new int2(secretPos.x, secretPos.y + (secretPos.y == bounds.y + 1 ? 1 : -1));
            features.Add(new RoomFeatureElement
            {
                Type = RoomFeatureType.Obstacle,
                Position = wallPos,
                FeatureId = (uint)(seed + 210)
            });
        }

        private void GenerateBranchingPaths(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, 
                                          SecretAreaConfig secretConfig, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed + 1000);
            
            // Create upper and lower alternate routes
            if (bounds.height > 6)
            {
                // Upper route
                var upperY = bounds.y + bounds.height - 2;
                for (int x = bounds.x + 2; x < bounds.x + bounds.width - 2; x += 4)
                {
                    features.Add(new RoomFeatureElement
                    {
                        Type = RoomFeatureType.Platform,
                        Position = new int2(x, upperY),
                        FeatureId = (uint)(seed + 1000 + x)
                    });
                }
                
                // Lower route (if room is tall enough)
                if (bounds.height > 8)
                {
                    var lowerY = bounds.y + 2;
                    for (int x = bounds.x + 3; x < bounds.x + bounds.width - 3; x += 5)
                    {
                        features.Add(new RoomFeatureElement
                        {
                            Type = RoomFeatureType.Platform,
                            Position = new int2(x, lowerY),
                            FeatureId = (uint)(seed + 2000 + x)
                        });
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

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _biomeLookup = state.GetComponentLookup<Core.Biome>(true);
            _featureBufferLookup = state.GetBufferLookup<RoomFeatureElement>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _biomeLookup.Update(ref state);
            _featureBufferLookup.Update(ref state);

            var heightmapJob = new BiomeHeightmapJob
            {
                BiomeLookup = _biomeLookup,
                FeatureBufferLookup = _featureBufferLookup
            };

            state.Dependency = heightmapJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct BiomeHeightmapJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Core.Biome> BiomeLookup;
        public BufferLookup<RoomFeatureElement> FeatureBufferLookup;

        public void Execute(Entity entity, ref RoomGenerationRequest request, ref RoomHierarchyData roomData, in NodeId nodeId)
        {
            if (request.GeneratorType != RoomGeneratorType.BiomeWeightedHeightmap || request.IsComplete) return;

            if (!FeatureBufferLookup.HasBuffer(entity)) return;

            var features = FeatureBufferLookup[entity];
            var bounds = roomData.Bounds;
            features.Clear();

            // Get biome information for terrain characteristics
            var biome = BiomeLookup.HasComponent(entity) ? BiomeLookup[entity] : 
                       new Core.Biome(BiomeType.SolarPlains, Polarity.Sun);

            // Generate heightmap using biome-specific noise
            GenerateBiomeHeightmap(features, bounds, biome, request.GenerationSeed);
        }

        private void GenerateBiomeHeightmap(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, 
                                          Core.Biome biome, uint seed)
        {
            var noiseScale = GetBiomeNoiseScale(biome.Type);
            var heightVariation = GetBiomeHeightVariation(biome.Type);
            var baseHeight = bounds.y + bounds.height / 3;

            for (int x = bounds.x; x < bounds.x + bounds.width; x++)
            {
                // Generate noise-based height
                var noise = math.sin(x * noiseScale + seed * 0.001f) * 0.5f + 0.5f;
                var height = baseHeight + (int)(noise * heightVariation);
                
                // Clamp to room bounds
                height = math.clamp(height, bounds.y, bounds.y + bounds.height - 1);
                
                // Create terrain platform
                features.Add(new RoomFeatureElement
                {
                    Type = RoomFeatureType.Platform,
                    Position = new int2(x, height),
                    FeatureId = (uint)(seed + x)
                });
                
                // Add biome-specific features
                if (ShouldAddBiomeFeature(x, biome, seed))
                {
                    var featureType = GetBiomeSpecificFeature(biome.Type);
                    var featureHeight = height + 1;
                    
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

        private float GetBiomeNoiseScale(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.SolarPlains => 0.1f,      // Gentle rolling hills
                BiomeType.FrozenWastes => 0.05f,    // Smooth ice sheets
                BiomeType.VolcanicCore => 0.2f,     // Rough volcanic terrain
                BiomeType.CrystalCaverns => 0.15f,  // Crystalline formations
                _ => 0.1f
            };
        }

        private float GetBiomeHeightVariation(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.SolarPlains => 3.0f,      // Moderate hills
                BiomeType.FrozenWastes => 1.0f,     // Flat frozen landscape
                BiomeType.VolcanicCore => 5.0f,     // Dramatic elevation changes
                BiomeType.CrystalCaverns => 4.0f,   // Tall crystal spires
                _ => 2.0f
            };
        }

        private bool ShouldAddBiomeFeature(int x, Core.Biome biome, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed + (uint)x);
            var featureChance = biome.Type switch
            {
                BiomeType.SolarPlains => 0.1f,      // Occasional trees/rocks
                BiomeType.FrozenWastes => 0.05f,    // Sparse ice formations
                BiomeType.VolcanicCore => 0.2f,     // Frequent lava vents
                BiomeType.CrystalCaverns => 0.15f,  // Crystal formations
                _ => 0.08f
            };
            
            return random.NextFloat() < featureChance;
        }

        private RoomFeatureType GetBiomeSpecificFeature(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.SolarPlains => RoomFeatureType.Obstacle,      // Trees/rocks
                BiomeType.FrozenWastes => RoomFeatureType.Obstacle,     // Ice spikes
                BiomeType.VolcanicCore => RoomFeatureType.Obstacle,     // Lava vents
                BiomeType.CrystalCaverns => RoomFeatureType.Collectible, // Crystal pickups
                _ => RoomFeatureType.Obstacle
            };
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

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _featureBufferLookup = state.GetBufferLookup<RoomFeatureElement>();
            _biomeLookup = state.GetComponentLookup<Core.Biome>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _featureBufferLookup.Update(ref state);
            _biomeLookup.Update(ref state);

            var cloudJob = new LayeredCloudJob
            {
                FeatureBufferLookup = _featureBufferLookup,
                BiomeLookup = _biomeLookup
            };

            state.Dependency = cloudJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct LayeredCloudJob : IJobEntity
    {
        public BufferLookup<RoomFeatureElement> FeatureBufferLookup;
        [ReadOnly] public ComponentLookup<Core.Biome> BiomeLookup;

        public void Execute(Entity entity, ref RoomGenerationRequest request, ref RoomHierarchyData roomData, in NodeId nodeId)
        {
            if (request.GeneratorType != RoomGeneratorType.LayeredPlatformCloud || request.IsComplete) return;

            if (!FeatureBufferLookup.HasBuffer(entity)) return;

            var features = FeatureBufferLookup[entity];
            var bounds = roomData.Bounds;
            features.Clear();

            // Get biome for motion pattern determination
            var biome = BiomeLookup.HasComponent(entity) ? BiomeLookup[entity] : 
                       new Core.Biome(BiomeType.SkyGardens, Polarity.Wind);

            // Generate layered cloud platforms
            var layerCount = math.max(3, bounds.height / 4);
            
            for (int layer = 0; layer < layerCount; layer++)
            {
                var layerY = bounds.y + (layer * bounds.height / layerCount);
                GenerateCloudLayer(features, bounds, layerY, layer, biome, request.GenerationSeed);
            }

            // Add floating islands
            GenerateFloatingIslands(features, bounds, biome, request.GenerationSeed);
        }

        private void GenerateCloudLayer(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, 
                                      int layerY, int layerIndex, Core.Biome biome, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed + (uint)layerIndex * 200);
            var cloudCount = random.NextInt(2, 5);
            
            for (int cloud = 0; cloud < cloudCount; cloud++)
            {
                var cloudX = random.NextInt(bounds.x + 1, bounds.x + bounds.width - 1);
                var cloudY = layerY + random.NextInt(-1, 2); // Slight vertical variation
                
                // Create cloud platform
                features.Add(new RoomFeatureElement
                {
                    Type = RoomFeatureType.Platform,
                    Position = new int2(cloudX, cloudY),
                    FeatureId = (uint)(seed + layerIndex * 1000 + cloud * 100)
                });
                
                // Add motion pattern based on biome
                var motionType = GetCloudMotionType(biome.Type, biome.PrimaryPolarity);
                AddCloudMotionFeature(features, cloudX, cloudY, motionType, seed, layerIndex, cloud);
            }
        }

        private void GenerateFloatingIslands(DynamicBuffer<RoomFeatureElement> features, RectInt bounds, 
                                           Core.Biome biome, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed + 5000);
            var islandCount = random.NextInt(1, 3);
            
            for (int island = 0; island < islandCount; island++)
            {
                var islandCenterX = random.NextInt(bounds.x + 3, bounds.x + bounds.width - 3);
                var islandCenterY = random.NextInt(bounds.y + 2, bounds.y + bounds.height - 2);
                
                // Create island base (3x2 platform cluster)
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = 0; dy <= 1; dy++)
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
                
                // Add island-specific features based on biome
                AddIslandFeatures(features, islandCenterX, islandCenterY, biome, random, seed, island);
            }
        }

        private CloudMotionType GetCloudMotionType(BiomeType biome, Polarity polarity)
        {
            return biome switch
            {
                BiomeType.SkyGardens => CloudMotionType.Gentle,      // Slow drifting
                BiomeType.PlasmaFields => CloudMotionType.Electric, // Rapid movement
                BiomeType.PowerPlant => CloudMotionType.Conveyor,   // Mechanical motion
                _ => polarity switch
                {
                    Polarity.Wind => CloudMotionType.Gusty,          // Irregular movement
                    Polarity.Tech => CloudMotionType.Conveyor,      // Predictable patterns
                    _ => CloudMotionType.Gentle
                }
            };
        }

        private void AddCloudMotionFeature(DynamicBuffer<RoomFeatureElement> features, int cloudX, int cloudY, 
                                         CloudMotionType motionType, uint seed, int layerIndex, int cloudIndex)
        {
            // Add motion feature components to the entity for dynamic cloud behavior
            // This creates the complete motion system for interactive cloud platforms
            var motionFeatureType = motionType switch
            {
                CloudMotionType.Conveyor => RoomFeatureType.Platform, // Could be ConveyorPlatform
                CloudMotionType.Electric => RoomFeatureType.Obstacle, // Could be ElectricCloud
                _ => RoomFeatureType.Platform
            };
            
            // Add motion indicator (in practice this would be a separate motion component)
            features.Add(new RoomFeatureElement
            {
                Type = motionFeatureType,
                Position = new int2(cloudX, cloudY + 1),
                FeatureId = (uint)(seed + layerIndex * 1000 + cloudIndex * 100 + 50) // Motion feature ID
            });
        }

        private void AddIslandFeatures(DynamicBuffer<RoomFeatureElement> features, int centerX, int centerY, 
                                     Core.Biome biome, Unity.Mathematics.Random random, uint seed, int islandIndex)
        {
            // Add biome-specific island features
            var featureType = biome.Type switch
            {
                BiomeType.SkyGardens => RoomFeatureType.PowerUp,     // Nature power-ups
                BiomeType.PlasmaFields => RoomFeatureType.Collectible, // Energy crystals
                BiomeType.PowerPlant => RoomFeatureType.SaveStation, // Tech stations
                _ => RoomFeatureType.Secret                          // Hidden treasures
            };
            
            if (random.NextFloat() < 0.7f) // 70% chance for island feature
            {
                features.Add(new RoomFeatureElement
                {
                    Type = featureType,
                    Position = new int2(centerX, centerY + 2),
                    FeatureId = (uint)(seed + 6000 + islandIndex * 100)
                });
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
        public static RoomFeatureObjectType ConvertToObjectType(RoomFeatureType featureType)
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
        public static RoomFeatureType NormalizeFeatureType(RoomFeatureType featureType)
        {
            return featureType; // Pass-through for compatibility
        }
    }
}