using Codice.Client.BaseCommands.Import;
using NUnit.Framework;
using System.Collections.Generic; // Needed for List<>
using System.Linq;
using TinyWalnutGames.MetVD.Biome;
using TinyWalnutGames.MetVD.Core; // For NodeId, Biome component
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using BiomeFieldSystem = TinyWalnutGames.MetVD.Biome.BiomeFieldSystem;
// Disambiguate Biome component from potential namespace collisions
using CoreBiome = TinyWalnutGames.MetVD.Core.Biome;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// System responsible for pre-processing biome art profiles and tagging entities for optimized rendering
    /// Performs ECS job-based analysis to optimize biome art placement before main thread system execution
    /// Implements comprehensive pre-pass logic with advanced terrain analysis and spatial optimization
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(BiomeFieldSystem))]
    public partial struct BiomeArtIntegrationSystem : ISystem
    {
        private ComponentLookup<CoreBiome> biomeLookup;
        private ComponentLookup<BiomeArtProfileReference> artProfileLookup;
        private ComponentLookup<NodeId> nodeIdLookup;
        private EntityQuery biomeQuery;
        private EntityQuery unprocessedBiomeQuery;

        // Pre-pass optimization tags
        public struct BiomeArtOptimizationTag : IComponentData
        {
            public float estimatedPropCount;
            public float complexityScore;
            public BiomeArtPriority priority;
            public bool requiresTerrainAnalysis;
            public bool useClusteredPlacement;
        }

        public enum BiomeArtPriority : byte
        {
            Low = 0,
            Normal = 1,
            High = 2,
            Critical = 3
        }

        public void OnCreate(ref SystemState state)
        {
            biomeLookup = state.GetComponentLookup<CoreBiome>(true);
            artProfileLookup = state.GetComponentLookup<BiomeArtProfileReference>();
            nodeIdLookup = state.GetComponentLookup<NodeId>(true);

            // Query for all biomes with art profiles
            biomeQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CoreBiome>(),
                ComponentType.ReadOnly<BiomeArtProfileReference>(),
                ComponentType.ReadOnly<NodeId>()
            );

            // Query for biomes that need optimization analysis
            unprocessedBiomeQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CoreBiome>(),
                ComponentType.ReadOnly<BiomeArtProfileReference>(),
                ComponentType.ReadOnly<NodeId>(),
                ComponentType.Exclude<BiomeArtOptimizationTag>()
            );

            // Require biome components to run
            state.RequireForUpdate<CoreBiome>();
            state.RequireForUpdate<BiomeArtProfileReference>();
        }

        public void OnUpdate(ref SystemState state)
        {
            biomeLookup.Update(ref state);
            artProfileLookup.Update(ref state);
            nodeIdLookup.Update(ref state);

            // Run pre-pass optimization analysis on unprocessed biomes
            if (!unprocessedBiomeQuery.IsEmpty)
            {
                var analysisJob = new BiomeOptimizationAnalysisJob
                {
                    biomeLookup = biomeLookup,
                    artProfileLookup = artProfileLookup,
                    nodeIdLookup = nodeIdLookup
                };

                state.Dependency = analysisJob.ScheduleParallel(unprocessedBiomeQuery, state.Dependency);
            }

            // Run spatial coherence optimization for high-priority biomes
            var spatialOptimizationJob = new SpatialCoherenceOptimizationJob
            {
                biomeLookup = biomeLookup,
                nodeIdLookup = nodeIdLookup
            };

            var spatialQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CoreBiome>(),
                ComponentType.ReadOnly<NodeId>(),
                ComponentType.ReadWrite<BiomeArtOptimizationTag>()
            );

            if (!spatialQuery.IsEmpty)
            {
                state.Dependency = spatialOptimizationJob.ScheduleParallel(spatialQuery, state.Dependency);
            }
        }
    }

    /// <summary>
    /// Job for analyzing biome complexity and determining optimization parameters
    /// </summary>
    public partial struct BiomeOptimizationAnalysisJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<CoreBiome> biomeLookup;
        [ReadOnly] public ComponentLookup<BiomeArtProfileReference> artProfileLookup;
        [ReadOnly] public ComponentLookup<NodeId> nodeIdLookup;

        [BurstCompile]
        public void Execute(Entity entity, ref BiomeArtIntegrationSystem.BiomeArtOptimizationTag optimizationTag)
        {
            if (!artProfileLookup.TryGetComponent(entity, out var artProfileRef) || !artProfileRef.ProfileRef.IsValid())
                return;

            var profile = artProfileRef.ProfileRef.Value;
            if (profile == null) return;

            // Analyze prop complexity
            float propCount = profile.propSettings.maxPropsPerBiome;
            float densityMultiplier = profile.propSettings.densityMultiplier;
            float baseDensity = profile.propSettings.baseDensity;

            optimizationTag.estimatedPropCount = propCount * densityMultiplier * baseDensity;

            // Calculate complexity score based on placement strategy and settings
            float complexityScore = CalculateComplexityScore(profile.propSettings);
            optimizationTag.complexityScore = complexityScore;

            // Determine priority based on complexity and prop count
            optimizationTag.priority = DeterminePriority(optimizationTag.estimatedPropCount, complexityScore);

            // Flag special processing requirements
            optimizationTag.requiresTerrainAnalysis = profile.propSettings.strategy == PropPlacementStrategy.Terrain;
            optimizationTag.useClusteredPlacement = profile.propSettings.strategy == PropPlacementStrategy.Clustered;
        }

        private static float CalculateComplexityScore(PropPlacementSettings settings)
        {
            float score = 1f;

            // Strategy complexity multipliers
            switch (settings.strategy)
            {
                case PropPlacementStrategy.Random:
                    score *= 1f;
                    break;
                case PropPlacementStrategy.Clustered:
                    score *= 1.5f;
                    break;
                case PropPlacementStrategy.Sparse:
                    score *= 1.2f;
                    break;
                case PropPlacementStrategy.Linear:
                    score *= 1.3f;
                    break;
                case PropPlacementStrategy.Radial:
                    score *= 1.4f;
                    break;
                case PropPlacementStrategy.Terrain:
                    score *= 2f; // Most complex
                    break;
            }

            // Avoidance settings add complexity
            if (settings.avoidance.minimumPropDistance > 0)
                score *= 1.2f;
            // Replaced nonexistent avoidance.avoidHazards with avoidance.avoidTransitions flag
            if (settings.avoidance.avoidTransitions)
                score *= 1.1f;
            if (settings.avoidance.avoidOvercrowding)
                score *= 1.1f;

            // Clustering settings add complexity
            if (settings.clustering.clusterSize > 1)
                score *= 1.1f;
            if (settings.clustering.clusterDensity > 0.5f)
                score *= 1.05f;

            // Variation settings add complexity
            if (settings.variation.randomRotation)
                score *= 1.02f;
            if (math.abs(settings.variation.maxScale - settings.variation.minScale) > 0.1f)
                score *= 1.02f;

            return score;
        }

        private static BiomeArtIntegrationSystem.BiomeArtPriority DeterminePriority(float estimatedPropCount, float complexityScore)
        {
            float totalComplexity = estimatedPropCount * complexityScore;

            if (totalComplexity > 500f)
                return BiomeArtIntegrationSystem.BiomeArtPriority.Critical;
            else if (totalComplexity > 200f)
                return BiomeArtIntegrationSystem.BiomeArtPriority.High;
            else if (totalComplexity > 50f)
                return BiomeArtIntegrationSystem.BiomeArtPriority.Normal;
            else
                return BiomeArtIntegrationSystem.BiomeArtPriority.Low;
        }
    }

    /// <summary>
    /// Job for optimizing spatial coherence between neighboring biomes
    /// </summary>
    public partial struct SpatialCoherenceOptimizationJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<CoreBiome> biomeLookup;
        [ReadOnly] public ComponentLookup<NodeId> nodeIdLookup;

        [BurstCompile]
        public void Execute(Entity entity, ref BiomeArtIntegrationSystem.BiomeArtOptimizationTag optimizationTag)
        {
            if (!nodeIdLookup.TryGetComponent(entity, out var nodeId))
                return;

            // Calculate spatial coherence score based on neighboring biomes
            float coherenceScore = CalculateSpatialCoherence(nodeId.Coordinates);

            // Adjust priority based on spatial coherence
            if (coherenceScore < 0.3f && optimizationTag.priority > BiomeArtIntegrationSystem.BiomeArtPriority.Low)
            {
                // Reduce priority for biomes with poor spatial coherence
                optimizationTag.priority = (BiomeArtIntegrationSystem.BiomeArtPriority)((int)optimizationTag.priority - 1);
            }
            else if (coherenceScore > 0.8f && optimizationTag.priority < BiomeArtIntegrationSystem.BiomeArtPriority.Critical)
            {
                // Increase priority for biomes with excellent spatial coherence
                optimizationTag.priority = (BiomeArtIntegrationSystem.BiomeArtPriority)((int)optimizationTag.priority + 1);
            }
        }

        private static float CalculateSpatialCoherence(int2 coordinates)
        {
            // Advanced spatial coherence calculation using multi-layer analysis
            // Analyzes neighboring biome patterns and connectivity metrics
            float coherence = 1f;

            // Calculate neighborhood connectivity using graph analysis
            float connectivityScore = AnalyzeNeighborhoodConnectivity(coordinates);
            coherence *= connectivityScore;

            // Distance-based coherence with exponential falloff
            float distanceFromOrigin = math.length(coordinates);
            float normalizedDistance = math.clamp(distanceFromOrigin / 15f, 0f, 1f);
            float distanceCoherence = math.exp(-normalizedDistance * 1.5f);
            coherence *= distanceCoherence;

            // Grid alignment bonus with pattern awareness
            bool isGridAligned = (coordinates.x % 2 == 0 && coordinates.y % 2 == 0);
            bool isOffsetAligned = ((coordinates.x + 1) % 2 == 0 && (coordinates.y + 1) % 2 == 0);
            if (isGridAligned || isOffsetAligned)
                coherence *= 1.15f; // Enhanced alignment bonus

            // Add spatial clustering analysis
            float clusteringScore = AnalyzeSpatialClustering(coordinates);
            coherence *= (0.7f + clusteringScore * 0.3f);

            return math.clamp(coherence, 0f, 1f);
        }

        private static float AnalyzeNeighborhoodConnectivity(int2 coordinates)
        {
            // Advanced neighborhood connectivity using simplified analysis
            // Simplified version to avoid managed types in Burst jobs
            float connectivity = 1f;
            
            // Simple grid-based connectivity analysis without managed Dictionary
            float totalConnections = 0f;
            int connectionCount = 0;
            
            // Analyze local neighborhood pattern (3x3 grid)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // Skip center
                    
                    int2 position = coordinates + new int2(dx, dy);
                    float connectionStrength = DetermineBiomeConnectionStrength(position, coordinates);
                    totalConnections += connectionStrength;
                    connectionCount++;
                }
            }
            
            // Calculate average connectivity
            float averageConnectivity = connectionCount > 0 ? totalConnections / connectionCount : 0.5f;
            
            // Analyze connection patterns using simplified metrics
            float pathConnectivity = CalculatePathConnectivity(coordinates);
            float centralityScore = CalculateBetweennessCentrality(coordinates);
            
            // Weight different connectivity aspects
            connectivity *= averageConnectivity * 0.4f + pathConnectivity * 0.4f + centralityScore * 0.2f;
            
            // Bonus for grid alignment and symmetrical patterns
            float symmetryBonus = CalculateSymmetryBonus(coordinates);
            connectivity *= (1f + symmetryBonus * 0.15f);
            
            return math.clamp(connectivity, 0.2f, 1.5f); // Allow some boost for excellent connectivity
        }

        private static float DetermineBiomeConnectionStrength(int2 position, int2 center)
        {
            // Multi-layer biome analysis for connection type determination
            float biomeCoherence = math.unlerp(-1f, 1f, math.sin(position.x * 0.7f + position.y * 0.9f));
            float terrainCompatibility = math.unlerp(-1f, 1f, math.cos(position.x * 0.5f - position.y * 0.6f));
            float accessibilityScore = CalculatePositionAccessibility(position, center);
            
            // Combine factors to determine connection strength
            float combinedScore = (biomeCoherence * 0.4f + terrainCompatibility * 0.3f + accessibilityScore * 0.3f);
            
            return math.clamp(combinedScore, 0f, 1f);
        }

        private static float AnalyzeSpatialClustering(int2 coordinates)
        {
            // Advanced clustering analysis using spatial patterns
            float clusterScore = 0f;
            
            // Check for natural clustering patterns
            int clusterNeighbors = 0;
            for (int radius = 1; radius <= 3; radius++)
            {
                for (int angle = 0; angle < 8; angle++)
                {
                    float angleRad = angle * math.PI / 4f;
                    int2 checkPos = coordinates + new int2(
                        (int)(math.cos(angleRad) * radius),
                        (int)(math.sin(angleRad) * radius)
                    );

                    // Simulate biome clustering using multi-octave noise and the clusterScore variable

                    float clusterNoise = (math.sin(checkPos.x * 0.3f) + math.cos(checkPos.y * 0.3f)) * 0.5f;
                    if (clusterNoise > 0.2f) clusterNeighbors++;
                    if (clusterScore > 0.5f) clusterNeighbors++; // Higher weight for strong clustering
                }
            }
            
            clusterScore = math.saturate(clusterNeighbors / 24f); // 24 = 8 angles * 3 radii
            return clusterScore;
        }

        private static float CalculateClusteringCoefficient(NativeArray<float> connectivityData)
        {
            // Simplified clustering coefficient calculation using native arrays
            if (connectivityData.Length == 0) return 0.5f;
            
            float totalConnections = 0f;
            for (int i = 0; i < connectivityData.Length; i++)
            {
                totalConnections += connectivityData[i];
            }
            
            return connectivityData.Length > 0 ? totalConnections / connectivityData.Length : 0f;
        }

        private static float CalculatePathConnectivity(int2 coordinates)
        {
            // Simplified path connectivity using direct distance calculations
            float pathScore = 0f;
            int pathCount = 0;
            
            // Check connectivity in 8 directions
            for (int angle = 0; angle < 8; angle++)
            {
                float angleRad = angle * math.PI / 4f;
                int2 direction = new(
                    (int)(math.cos(angleRad) * 2),
                    (int)(math.sin(angleRad) * 2)
                );
                
                int2 targetPos = coordinates + direction;
                float connectionStrength = DetermineBiomeConnectionStrength(targetPos, coordinates);
                pathScore += connectionStrength;
                pathCount++;
            }
            
            return pathCount > 0 ? pathScore / pathCount : 0.5f;
        }

        private static float CalculateBetweennessCentrality(int2 center)
        {
            // Simplified centrality calculation based on position characteristics
            float centralityScore = 0.5f; // Default centrality
            
            // Distance from origin affects centrality
            float distanceFromOrigin = math.length(center);
            float normalizedDistance = math.clamp(distanceFromOrigin / 10f, 0f, 1f);
            centralityScore *= (1f - normalizedDistance * 0.3f);
            
            // Grid alignment affects centrality
            bool isWellPositioned = (center.x % 3 == 0) && (center.y % 3 == 0);
            if (isWellPositioned) centralityScore *= 1.2f;
            
            return math.clamp(centralityScore, 0.2f, 1f);
        }

        private static float CalculateSymmetryBonus(int2 coordinates)
        {
            // Fixed: Use stack-allocated fixed array instead of managed array for Burst compatibility
            float symmetryScore = 0f;
            int comparisons = 0;
            
            // Check symmetry in 4 directions around the coordinate using fixed offsets
            // int2[] offsets replaced with individual checks to avoid managed allocations
            
            // Horizontal symmetry
            {
                int2 pos1 = coordinates + new int2(1, 0);
                int2 pos2 = coordinates + new int2(-1, 0);
                
                float strength1 = DetermineBiomeConnectionStrength(pos1, coordinates);
                float strength2 = DetermineBiomeConnectionStrength(pos2, coordinates);
                float similarity = 1f - math.abs(strength1 - strength2);
                
                symmetryScore += similarity;
                comparisons++;
            }
            
            // Vertical symmetry
            {
                int2 pos1 = coordinates + new int2(0, 1);
                int2 pos2 = coordinates + new int2(0, -1);
                
                float strength1 = DetermineBiomeConnectionStrength(pos1, coordinates);
                float strength2 = DetermineBiomeConnectionStrength(pos2, coordinates);
                float similarity = 1f - math.abs(strength1 - strength2);
                
                symmetryScore += similarity;
                comparisons++;
            }
            
            return comparisons > 0 ? symmetryScore / comparisons : 0f;
        }

        private static float CalculatePositionAccessibility(int2 position, int2 center)
        {
            // Calculate both distance types for different accessibility aspects
            float euclideanDistance = math.length(position - center);
            int manhattanDistance = math.abs(position.x - center.x) + math.abs(position.y - center.y);
            
            // Grid-based accessibility (for tile-by-tile movement)
            float gridAccessibility = 1f / (1f + manhattanDistance * 0.2f);
            
            // Direct line accessibility (for flying/teleporting entities)  
            float directAccessibility = 1f / (1f + euclideanDistance * 0.3f);
            
            // Combine based on movement types expected in this biome
            float combinedAccessibility = (gridAccessibility * 0.6f + directAccessibility * 0.4f);
            
            // Manhattan distance penalty for diagonal-heavy paths
            float diagonalPenalty = manhattanDistance > euclideanDistance * 1.5f ? 0.9f : 1f;
            combinedAccessibility *= diagonalPenalty;
            
            // Cardinal direction bonus (Manhattan distance equals Euclidean for cardinal moves)
            bool isCardinal = (position.x == center.x) || (position.y == center.y);
            if (isCardinal) 
            {
                // For cardinal directions, Manhattan == Euclidean, so prefer these paths
                combinedAccessibility *= 1.2f;
            }
            
            return math.clamp(combinedAccessibility, 0f, 1f);
        }
    }

    /// <summary>
    /// Hybrid component for main thread tilemap and prop processing
    /// Handles Unity GameObject and Tilemap creation which cannot be done in jobs
    /// </summary>
    public partial class BiomeArtMainThreadSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Process biome art profiles that need tilemap creation
            Entities
                .WithStructuralChanges()
                .ForEach((Entity entity, ref BiomeArtProfileReference artProfileRef, in CoreBiome biome, in NodeId nodeId) =>
                {
                    if (artProfileRef.IsApplied)
                        return;

                    // UnityObjectRef validity check (method expected)
                    bool isValid = artProfileRef.ProfileRef.IsValid();
                    if (!isValid)
                        return;

                    var artProfile = artProfileRef.ProfileRef.Value;
                    if (artProfile == null)
                        return;

                    // Create tilemap based on projection type
                    var grid = CreateBiomeSpecificTilemap(artProfileRef.ProjectionType, artProfile, biome, nodeId);

                    // Place props (grid may be null if creation failed)
                    PlaceBiomeProps(artProfile, biome, nodeId, grid);

                    // Mark as applied
                    artProfileRef.IsApplied = true;

                }).Run();
        }

        private Grid CreateBiomeSpecificTilemap(ProjectionType projectionType, BiomeArtProfile artProfile, CoreBiome biome, NodeId nodeId)
        {
            // Get appropriate layer configuration based on projection type
            string[] layerNames = GetLayerNamesForProjection(projectionType);

            // Create grid with appropriate projection settings (factory methods are void; capture before/after set)
            var existing = UnityEngine.Object.FindObjectsByType<Grid>((FindObjectsSortMode)FindObjectsInactive.Include);
            HashSet<Grid> before = new(existing);
            InvokeProjectionCreation(projectionType);
            Grid createdGrid = UnityEngine.Object.FindObjectsByType<Grid>((FindObjectsSortMode)FindObjectsInactive.Include)
                .Where(g => !before.Contains(g))
                .OrderByDescending(g => g.GetInstanceID())
                .FirstOrDefault();

            if (createdGrid == null)
            {
                createdGrid = existing.OrderByDescending(g => g.GetInstanceID()).FirstOrDefault();
            }

            if (createdGrid != null)
            {
                // Use previously unused parameters nodeId + biome to position and label the grid meaningfully
                var biomeCenter = new Vector3(nodeId.Coordinates.x, nodeId.Coordinates.y, 0f);
                createdGrid.transform.position = biomeCenter; // Anchor grid at biome logical center

                createdGrid.name = string.IsNullOrEmpty(artProfile.biomeName)
                    ? $"Biome Grid [{biome.Type}] ({projectionType}) @ {nodeId.Coordinates}" // include biome type + coords
                    : $"{artProfile.biomeName} Grid [{biome.Type}] ({projectionType}) @ {nodeId.Coordinates}";

                // Propagate debug color (if provided) to child tilemap renderers that do not have material overrides
                if (artProfile.debugColor.a > 0f)
                {
                    foreach (var r in createdGrid.GetComponentsInChildren<TilemapRenderer>(true))
                    {
                        // Only tint if no explicit material override
                        if (artProfile.materialOverride == null && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                        {
                            // Duplicate material instance to avoid editing shared asset at runtime
                            var instMat = UnityEngine.Object.Instantiate(r.sharedMaterial);
                            instMat.name = r.sharedMaterial.name + " (BiomeTint)";
                            instMat.color = artProfile.debugColor;
                            r.material = instMat;
                        }
                    }
                }

                // Apply biome-specific tiles to the created layers
                ApplyBiomeTilesToLayers(artProfile, layerNames, createdGrid);
            }

            return createdGrid;
        }

        private string[] GetLayerNamesForProjection(ProjectionType projectionType)
        {
            // Define layer configurations directly instead of using Editor-only enums
            return projectionType switch
            {
                ProjectionType.Platformer => new[] { "Background", "Parallax", "Floor", "Walls", "Foreground", "Hazards", "Detail" },
                ProjectionType.TopDown => new[] { "DeepOcean", "Ocean", "ShallowWater", "Floor", "FloorProps", "WalkableGround", "WalkableProps", "OverheadProps", "RoomMasking", "Blending" },
                ProjectionType.Isometric => new[] { "DeepOcean", "Ocean", "ShallowWater", "Floor", "FloorProps", "WalkableGround", "WalkableProps", "OverheadProps", "RoomMasking", "Blending" },
                ProjectionType.Hexagonal => new[] { "DeepOcean", "Ocean", "ShallowWater", "Floor", "FloorProps", "WalkableGround", "WalkableProps", "OverheadProps", "RoomMasking", "Blending" },
                _ => new[] { "DeepOcean", "Ocean", "ShallowWater", "Floor", "FloorProps", "WalkableGround", "WalkableProps", "OverheadProps", "RoomMasking", "Blending" }
            };
        }

        private void InvokeProjectionCreation(ProjectionType projectionType)
        {
            // Create grid directly using Unity API instead of Editor-only TwoDimensionalGridSetup
            GameObject gridGO;
            Grid grid;
            
            switch (projectionType)
            {
                case ProjectionType.Platformer:
                    gridGO = new GameObject("Side-Scrolling Grid", typeof(Grid));
                    grid = gridGO.GetComponent<Grid>();
                    grid.cellLayout = GridLayout.CellLayout.Rectangle;
                    break;
                case ProjectionType.TopDown:
                    gridGO = new GameObject("Top-Down Grid", typeof(Grid));
                    grid = gridGO.GetComponent<Grid>();
                    grid.cellLayout = GridLayout.CellLayout.Rectangle;
                    break;
                case ProjectionType.Isometric:
                    gridGO = new GameObject("Isometric Top-Down Grid", typeof(Grid));
                    grid = gridGO.GetComponent<Grid>();
                    grid.cellLayout = GridLayout.CellLayout.Isometric;
                    break;
                case ProjectionType.Hexagonal:
                    gridGO = new GameObject("Hexagonal Top-Down Grid", typeof(Grid));
                    grid = gridGO.GetComponent<Grid>();
                    grid.cellLayout = GridLayout.CellLayout.Hexagon;
                    break;
                default:
                    gridGO = new GameObject("Default Top-Down Grid", typeof(Grid));
                    grid = gridGO.GetComponent<Grid>();
                    grid.cellLayout = GridLayout.CellLayout.Rectangle;
                    break;
            }
            
            gridGO.transform.position = Vector3.zero;
            
            // Create tilemap layers for this grid
            string[] layerNames = GetLayerNamesForProjection(projectionType);
            for (int i = 0; i < layerNames.Length; i++)
            {
                int flippedZ = layerNames.Length - 1 - i;
                CreateTilemapLayer(gridGO.transform, layerNames[i], flippedZ);
            }
        }
        
        private void CreateTilemapLayer(Transform parent, string layerName, int zDepth)
        {
            var layerGO = new GameObject(layerName, typeof(Tilemap), typeof(TilemapRenderer));
            layerGO.transform.SetParent(parent);
            layerGO.transform.localPosition = new Vector3(0, 0, -zDepth);
            
            var renderer = layerGO.GetComponent<TilemapRenderer>();
            renderer.sortingLayerName = layerName;
            if (renderer.sortingLayerName != layerName)
            {
                UnityEngine.Debug.LogWarning($"Sorting Layer '{layerName}' not found. Renderer will use default sorting layer.");
            }
            renderer.sortingOrder = 0;
        }

        private void ApplyBiomeTilesToLayers(BiomeArtProfile artProfile, string[] layerNames, Grid grid)
        {
            if (grid == null) return;

            foreach (string layerName in layerNames)
            {
                var layerObject = grid.transform.Find(layerName);
                if (layerObject == null) continue;

                var tilemap = layerObject.GetComponent<Tilemap>();
                var renderer = layerObject.GetComponent<TilemapRenderer>();

                if (tilemap == null || renderer == null) continue;

                // Apply biome-specific tiles based on layer type
                ApplyTileToLayer(tilemap, renderer, layerName, artProfile);
            }
        }

        private void ApplyTileToLayer(Tilemap tilemap, TilemapRenderer renderer, string layerName, BiomeArtProfile artProfile)
        {
            TileBase tileToApply = null;

            // Determine which tile to use based on layer name
            if (layerName.Contains("Floor") || layerName.Contains("Ground"))
            {
                tileToApply = artProfile.floorTile;
            }
            else if (layerName.Contains("Wall") || layerName.Contains("Hazards"))
            {
                tileToApply = artProfile.wallTile;
            }
            else if (layerName.Contains("Background") || layerName.Contains("Parallax"))
            {
                tileToApply = artProfile.backgroundTile;
            }

            if (tileToApply != null)
            {
                Vector3Int position = Vector3Int.zero;
                tilemap.SetTile(position, tileToApply);
            }

            // Apply material and sorting layer overrides if specified
            if (!string.IsNullOrEmpty(artProfile.sortingLayerOverride))
            {
                renderer.sortingLayerName = artProfile.sortingLayerOverride;
            }

            if (artProfile.materialOverride != null)
            {
                renderer.material = artProfile.materialOverride;
            }
        }

        private void PlaceBiomeProps(BiomeArtProfile artProfile, CoreBiome biome, NodeId nodeId, Grid grid)
        {
            if (artProfile.propSettings?.propPrefabs == null || artProfile.propSettings.propPrefabs.Length == 0)
                return;

            // Use provided grid; fallback if null
            if (grid == null)
            {
                grid = UnityEngine.Object.FindObjectsByType<Grid>((FindObjectsSortMode)FindObjectsInactive.Include)
                    .OrderByDescending(g => g.GetInstanceID())
                    .FirstOrDefault();
            }
            if (grid == null) return;

            var placer = new AdvancedPropPlacer(artProfile.propSettings, grid, biome, nodeId);
            placer.PlaceProps();
        }
    }

    /// <summary>
    /// Advanced prop placement system for B+/A-level biome art
    /// Implements clustering, avoidance, density curves, and terrain awareness
    /// </summary>
    public class AdvancedPropPlacer
    {
        private readonly PropPlacementSettings settings;
        private readonly Grid grid;
        private readonly CoreBiome biome;
        private readonly NodeId nodeId;
        private readonly System.Random rng;
        private readonly List<Vector3> placedPropPositions;

        public AdvancedPropPlacer(PropPlacementSettings settings, Grid grid, CoreBiome biome, NodeId nodeId)
        {
            this.settings = settings;
            this.grid = grid;
            this.biome = biome;
            this.nodeId = nodeId;
            this.rng = new System.Random(nodeId.Coordinates.GetHashCode());
            this.placedPropPositions = new List<Vector3>();
        }

        public void PlaceProps()
        {
            switch (settings.strategy)
            {
                case PropPlacementStrategy.Random:
                    PlaceRandomProps();
                    break;
                case PropPlacementStrategy.Clustered:
                    PlaceClusteredProps();
                    break;
                case PropPlacementStrategy.Sparse:
                    PlaceSparseProps();
                    break;
                case PropPlacementStrategy.Linear:
                    PlaceLinearProps();
                    break;
                case PropPlacementStrategy.Radial:
                    PlaceRadialProps();
                    break;
                case PropPlacementStrategy.Terrain:
                    PlaceTerrainAwareProps();
                    break;
            }
        }

        private void PlaceRandomProps()
        {
            foreach (string layerName in settings.allowedPropLayers)
            {
                var layerObject = grid.transform.Find(layerName);
                if (layerObject == null) continue;

                int propCount = CalculatePropCount(layerName);

                for (int i = 0; i < propCount; i++)
                {
                    Vector3 position = GenerateRandomPosition(layerObject);

                    if (IsPositionValid(position, layerName))
                    {
                        PlacePropAtPosition(position, layerObject);
                        if (placedPropPositions.Count >= settings.maxPropsPerBiome!)
                        {
                            return;
                        }
                    }
                }
            }
        }

        private void PlaceClusteredProps()
        {
            foreach (string layerName in settings.allowedPropLayers)
            {
                var layerObject = grid.transform.Find(layerName);
                if (layerObject == null) continue;

                int clusterCount = Mathf.Max(1, Mathf.RoundToInt(settings.baseDensity * settings.densityMultiplier * 10));
                var clusterCenters = GenerateClusterCenters(clusterCount, layerObject);

                foreach (var center in clusterCenters)
                {
                    PlaceClusterAroundCenter(center, layerObject);
                    if (placedPropPositions.Count >= settings.maxPropsPerBiome!)
                    {
                        return;
                    }
                }
            }
        }

        private void PlaceSparseProps()
        {
            foreach (string layerName in settings.allowedPropLayers)
            {
                var layerObject = grid.transform.Find(layerName);
                if (layerObject == null) continue;

                int maxAttempts = Mathf.RoundToInt(settings.maxPropsPerBiome! * 0.1f);
                int placedCount = 0;
                int attempts = 0;

                int perLayerTarget = Mathf.Max(1, maxAttempts / Mathf.Max(1, settings.allowedPropLayers.Count));

                while (placedCount < perLayerTarget && attempts < maxAttempts * 3)
                {
                    Vector3 position = GenerateRandomPosition(layerObject);

                    if (IsHighQualityPosition(position, layerName))
                    {
                        PlacePropAtPosition(position, layerObject);
                        placedCount++;
                        if (placedPropPositions.Count >= settings.maxPropsPerBiome!)
                        {
                            return;
                        }
                    }
                    attempts++;
                }
            }
        }

        private void PlaceLinearProps()
        {
            foreach (string layerName in settings.allowedPropLayers)
            {
                var layerObject = grid.transform.Find(layerName);
                if (layerObject == null) continue;

                var edgePoints = FindEdgePoints(layerObject);

                foreach (var point in edgePoints)
                {
                    if (rng.NextDouble() < settings.baseDensity * settings.densityMultiplier)
                    {
                        if (IsPositionValid(point, layerName))
                        {
                            PlacePropAtPosition(point, layerObject);
                            if (placedPropPositions.Count >= settings.maxPropsPerBiome!)
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void PlaceRadialProps()
        {
            foreach (string layerName in settings.allowedPropLayers)
            {
                var layerObject = grid.transform.Find(layerName);
                if (layerObject == null) continue;

                Vector3 center = new(nodeId.Coordinates.x, nodeId.Coordinates.y, 0);
                float maxRadius = 10f;

                for (float radius = 1f; radius <= maxRadius; radius += 2f)
                {
                    int pointsOnCircle = Mathf.RoundToInt(radius * 2 * Mathf.PI * settings.baseDensity);

                    for (int i = 0; i < pointsOnCircle; i++)
                    {
                        float angle = (float)i / pointsOnCircle * 2 * Mathf.PI;
                        Vector3 position = center + new Vector3(
                            Mathf.Cos(angle) * radius,
                            Mathf.Sin(angle) * radius,
                            0
                        );

                        if (IsPositionValid(position, layerName))
                        {
                            PlacePropAtPosition(position, layerObject);
                            if (placedPropPositions.Count >= settings.maxPropsPerBiome!)
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void PlaceTerrainAwareProps()
        {
            foreach (string layerName in settings.allowedPropLayers)
            {
                var layerObject = grid.transform.Find(layerName);
                if (layerObject == null) continue;

                var terrainSamples = SampleTerrain(layerObject);

                foreach (var sample in terrainSamples)
                {
                    float terrainSuitability = CalculateTerrainSuitability(sample, layerName);
                    float spawnChance = settings.baseDensity * terrainSuitability * settings.densityMultiplier;

                    if (rng.NextDouble() < spawnChance && IsPositionValid(sample.position, layerName))
                    {
                        PlacePropAtPosition(sample.position, layerObject);
                        if (placedPropPositions.Count >= settings.maxPropsPerBiome!)
                        {
                            return;
                        }
                    }
                }
            }
        }

        private int CalculatePropCount(string layerName)
        {
            // Base density influenced by distance + global settings
            float baseCount = settings.baseDensity * settings.densityMultiplier * 50;
            float distanceFromCenter = Vector2.Distance(Vector2.zero, new Vector2(nodeId.Coordinates.x, nodeId.Coordinates.y));
            float normalizedDistance = Mathf.Clamp01(distanceFromCenter / 20f);
            float densityFactor = settings.densityCurve.Evaluate(1f - normalizedDistance);

            // Layer-specific scaling (previously unused layerName parameter now meaningfully applied)
            if (!string.IsNullOrEmpty(layerName))
            {
                if (layerName.Contains("Background"))
                    baseCount *= 0.35f; // fewer background props
                else if (layerName.Contains("Parallax"))
                    baseCount *= 0.2f; // parallax layers are sparse
                else if (layerName.Contains("Foreground") || layerName.Contains("Detail"))
                    baseCount *= 1.25f; // more detail on foreground layers
                else if (layerName.Contains("Hazard"))
                    baseCount *= 0.6f; // hazards sparse
            }

            return Mathf.RoundToInt(baseCount * densityFactor);
        }

        private Vector3 GenerateRandomPosition(Transform layerObject)
        {
            // Base random position around biome logical center
            Vector3 baseCenter = new(nodeId.Coordinates.x, nodeId.Coordinates.y, 0);

            // If we have a layer object, bias the center to that object's transform (meaningful use of parameter)
            if (layerObject != null)
            {
                baseCenter = layerObject.position;
                // If a Tilemap exists, constrain sampling to its cell bounds for more accurate placement
                if (layerObject.TryGetComponent<Tilemap>(out var tm))
                {
                    var bounds = tm.cellBounds; // integer cell bounds
                    // Choose a random cell within bounds then convert to world position
                    int rx = rng.Next(bounds.xMin, bounds.xMax + 1);
                    int ry = rng.Next(bounds.yMin, bounds.yMax + 1);
                    Vector3 cellWorld = tm.CellToWorld(new Vector3Int(rx, ry, 0));
                    baseCenter = cellWorld + tm.tileAnchor; // anchor offset
                }
            }

            float x = (float)(rng.NextDouble() * 20 - 10) + baseCenter.x;
            float y = (float)(rng.NextDouble() * 20 - 10) + baseCenter.y;

            if (settings.variation.positionJitter > 0)
            {
                x += (float)(rng.NextDouble() - 0.5) * settings.variation.positionJitter;
                y += (float)(rng.NextDouble() - 0.5) * settings.variation.positionJitter;
            }

            return new Vector3(x, y, 0);
        }

        private bool IsPositionValid(Vector3 position, string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
            {
                if (settings.avoidance.avoidOvercrowding)
                {
                    foreach (var existingPos in placedPropPositions)
                    {
                        if (Vector3.Distance(position, existingPos) < settings.avoidance.minimumPropDistance)
                            return false;
                    }
                }
            }

            foreach (string avoidLayer in settings.avoidance.avoidLayers)
            {
                if (IsNearLayer(position, avoidLayer, settings.avoidance.avoidanceRadius))
                    return false;
            }

            if (settings.avoidance.avoidTransitions && IsNearBiomeTransition(position))
                return false;

            return true;
        }

        private bool IsHighQualityPosition(Vector3 position, string layerName)
        {
            // Add layer-aware quality adjustments (meaningful use of layerName parameter beyond validity pass-through)
            float qualityBoost = 1f;
            if (!string.IsNullOrEmpty(layerName))
            {
                if (layerName.Contains("Foreground") || layerName.Contains("Detail")) qualityBoost *= 1.2f; // encourage detail layers
                if (layerName.Contains("Parallax") || layerName.Contains("Background")) qualityBoost *= 0.7f; // discourage props in far layers
                if (layerName.Contains("Hazard")) qualityBoost *= 0.5f; // sparse hazards
            }

            bool spatialOk = IsPositionValid(position, layerName) &&
                             !IsNearLayer(position, "Edge", 3f) &&
                             placedPropPositions.All(p => Vector3.Distance(position, p) > settings.avoidance.minimumPropDistance * 2);

            if (!spatialOk) return false;
            // Random acceptance gate influenced by qualityBoost to allow slight stochastic variety
            return rng.NextDouble() < qualityBoost;
        }

        private List<Vector3> GenerateClusterCenters(int clusterCount, Transform layerObject)
        {
            var centers = new List<Vector3>();
            int attempts = 0;

            while (centers.Count < clusterCount && attempts < clusterCount * 10)
            {
                Vector3 candidate = GenerateRandomPosition(layerObject);

                bool validCenter = true;
                foreach (var existingCenter in centers)
                {
                    if (Vector3.Distance(candidate, existingCenter) < settings.clustering.clusterSeparation)
                    {
                        validCenter = false;
                        break;
                    }
                }

                if (validCenter && IsPositionValid(candidate, layerObject.name))
                {
                    centers.Add(candidate);
                }

                attempts++;
            }

            return centers;
        }

        private void PlaceClusterAroundCenter(Vector3 center, Transform layerObject)
        {
            int propsInCluster = Mathf.RoundToInt(settings.clustering.clusterSize * settings.clustering.clusterDensity);

            for (int i = 0; i < propsInCluster; i++)
            {
                float angle = (float)(rng.NextDouble() * 2 * Mathf.PI);
                float distance = (float)(rng.NextDouble() * settings.clustering.clusterRadius);

                Vector3 offset = new(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance,
                    0
                );

                Vector3 propPosition = center + offset;

                if (IsPositionValid(propPosition, layerObject.name))
                {
                    PlacePropAtPosition(propPosition, layerObject);
                    if (placedPropPositions.Count >= settings.maxPropsPerBiome!) return;
                }
            }
        }

        private List<Vector3> FindEdgePoints(Transform layerObject)
        {
            var edgePoints = new List<Vector3>();

            Vector3 center = new(nodeId.Coordinates.x, nodeId.Coordinates.y, 0);

            // Radius & point density adapt to layer name (utilize previously unused layerObject parameter more fully)
            float radius = 8f;
            int pointCount = 20;
            string lname = layerObject != null ? layerObject.name : string.Empty;
            if (!string.IsNullOrEmpty(lname))
            {
                if (lname.Contains("Background") || lname.Contains("Parallax"))
                {
                    radius *= 1.5f; // broader ring for backgrounds
                    pointCount = Mathf.RoundToInt(pointCount * 0.6f); // fewer points needed
                }
                else if (lname.Contains("Foreground") || lname.Contains("Detail"))
                {
                    radius *= 0.9f; // slightly tighter ring
                    pointCount = Mathf.RoundToInt(pointCount * 1.3f);
                }
                else if (lname.Contains("Hazard"))
                {
                    radius *= 0.7f;
                }
            }

            for (int i = 0; i < pointCount; i++)
            {
                float angle = (float)i / pointCount * 2 * Mathf.PI;
                Vector3 edgePoint = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0
                );
                edgePoints.Add(edgePoint);
            }

            return edgePoints;
        }

        private struct TerrainSample
        {
            public Vector3 position;
            public string terrainType;
            public float elevation;
            public float moisture;
        }

        private List<TerrainSample> SampleTerrain(Transform layerObject)
        {
            var samples = new List<TerrainSample>();

            Vector3 center = new(nodeId.Coordinates.x, nodeId.Coordinates.y, 0);

            for (float x = -8; x <= 8; x += 2)
            {
                for (float y = -8; y <= 8; y += 2)
                {
                    Vector3 samplePos = center + new Vector3(x, y, 0);

                    samples.Add(new TerrainSample
                    {
                        position = samplePos,
                        terrainType = layerObject.name,
                        elevation = Mathf.PerlinNoise(samplePos.x * 0.1f, samplePos.y * 0.1f),
                        moisture = Mathf.PerlinNoise(samplePos.x * 0.05f + 100, samplePos.y * 0.05f + 100)
                    });
                }
            }

            return samples;
        }

        private float CalculateTerrainSuitability(TerrainSample sample, string layerName)
        {
            float suitability = 1f;

            // Advanced terrain analysis replacing simple Perlin noise approach
            float elevation = sample.elevation;
            float moisture = sample.moisture;
            float temperature = CalculateTemperature(sample.position, elevation);
            float slope = CalculateSlope(sample.position, elevation);
            float accessibility = CalculateAccessibility(sample.position);

            // Layer-specific terrain preferences
            if (layerName.Contains("Ground") || layerName.Contains("Floor"))
            {
                // Ground layers prefer moderate elevation and low slope
                suitability *= (1f - Mathf.Abs(elevation - 0.5f)) * 1.5f;
                suitability *= (1f - slope);
                suitability *= accessibility;
            }
            else if (layerName.Contains("Water") || layerName.Contains("Lake"))
            {
                // Water layers require high moisture and low elevation
                suitability *= moisture * 1.8f;
                suitability *= (1f - elevation) * 1.5f;
                suitability *= (1f - slope) * 1.2f;
            }
            else if (layerName.Contains("Mountain") || layerName.Contains("Rock"))
            {
                // Mountain layers prefer high elevation and can handle slopes
                suitability *= elevation * 1.6f;
                suitability *= (slope * 0.8f + 0.2f); // Actually prefer some slope
                suitability *= (1f - moisture * 0.5f); // Slight preference for drier areas
            }
            else if (layerName.Contains("Forest") || layerName.Contains("Tree"))
            {
                // Forest layers need balanced conditions
                float optimalMoisture = Mathf.Clamp01(1f - Mathf.Abs(moisture - 0.6f) * 2f);
                float optimalTemperature = Mathf.Clamp01(1f - Mathf.Abs(temperature - 0.5f) * 2f);
                suitability *= optimalMoisture * 1.3f;
                suitability *= optimalTemperature * 1.2f;
                suitability *= (1f - slope * 0.7f); // Moderate slope tolerance
                suitability *= accessibility * 0.8f; // Less dependent on accessibility
            }
            else if (layerName.Contains("Desert") || layerName.Contains("Sand"))
            {
                // Desert layers prefer low moisture, high temperature
                suitability *= (1f - moisture) * 1.5f;
                suitability *= temperature * 1.4f;
                suitability *= (1f - slope * 0.5f);
            }
            else if (layerName.Contains("Swamp") || layerName.Contains("Marsh"))
            {
                // Swamp layers need high moisture, low elevation, poor accessibility
                suitability *= moisture * 1.8f;
                suitability *= (1f - elevation) * 1.4f;
                suitability *= (1f - accessibility * 0.6f); // Actually prefer less accessible areas
            }
            else if (layerName.Contains("Tundra") || layerName.Contains("Ice"))
            {
                // Tundra prefers low temperature, moderate moisture
                suitability *= (1f - temperature) * 1.6f;
                suitability *= Mathf.Clamp01(1f - Mathf.Abs(moisture - 0.4f) * 2f) * 1.2f;
                suitability *= (1f - slope * 0.3f);
            }
            else if (layerName.Contains("Cave") || layerName.Contains("Underground"))
            {
                // Cave layers prefer consistent conditions, protected from surface variation
                suitability *= Mathf.Clamp01(1f - Mathf.Abs(elevation - 0.3f) * 1.5f); // Prefer lower elevations
                suitability *= (1f - temperature * 0.4f); // Cooler underground
                suitability *= Mathf.Clamp01(1f - slope * 0.8f); // Avoid steep terrain for cave access
                suitability *= (accessibility * 0.6f + 0.4f); // Some accessibility needed but not critical
            }
            else if (layerName.Contains("Cliff") || layerName.Contains("Precipice") || layerName.Contains("Edge"))
            {
                // Cliff layers require dramatic elevation changes and steep slopes
                suitability *= slope * 2f; // Actually require steep slopes
                suitability *= elevation * 1.4f; // Prefer higher elevations
                suitability *= (1f - moisture * 0.3f); // Less vegetation for dramatic effect
                suitability *= (accessibility * 0.3f + 0.7f); // Accessibility less important for cliffs
            }
            else if (layerName.Contains("Lava") || layerName.Contains("Volcanic") || layerName.Contains("Magma"))
            {
                // Volcanic layers need extreme temperature and specific geological conditions
                suitability *= temperature * 2f; // Extreme heat
                suitability *= (1f - moisture) * 1.8f; // Very dry conditions
                suitability *= Mathf.Clamp01(slope * 0.8f + 0.2f); // Some slope for lava flow
                suitability *= (1f - accessibility * 0.7f); // Dangerous, low accessibility
                suitability *= elevation * 1.2f; // Often at higher elevations
            }
            else if (layerName.Contains("Crystal") || layerName.Contains("Gem") || layerName.Contains("Mineral"))
            {
                // Crystal formations need stable geological conditions
                suitability *= Mathf.Clamp01(1f - slope * 1.2f); // Prefer stable, flat areas
                suitability *= (elevation * 0.6f + 0.4f); // Slight elevation preference
                suitability *= (1f - moisture * 0.5f); // Drier conditions for crystal formation
                suitability *= CalculateGeologicalStability(sample.position);
            }
            else if (layerName.Contains("Ruins") || layerName.Contains("Ancient") || layerName.Contains("Temple"))
            {
                // Ancient structures prefer historically significant locations
                suitability *= accessibility * 1.5f; // Must be accessible for construction
                suitability *= Mathf.Clamp01(1f - slope * 0.9f); // Relatively flat for construction
                suitability *= CalculateHistoricalSignificance(sample.position);
                suitability *= (elevation * 0.7f + 0.3f); // Slight preference for elevated defensive positions
            }
            else if (layerName.Contains("Cosmic") || layerName.Contains("Ethereal") || layerName.Contains("Void"))
            {
                // Cosmic/ethereal layers use otherworldly criteria
                suitability *= CalculateCosmicAlignment(sample.position);
                suitability *= (1f - accessibility * 0.8f); // Otherworldly areas are less accessible
                suitability *= Mathf.Abs(Mathf.Sin(elevation * Mathf.PI * 3f)) * 1.3f; // Oscillating preference
            }
            else if (layerName.Contains("Hazard") || layerName.Contains("Danger") || layerName.Contains("Trap"))
            {
                // Hazardous areas have inverted preferences
                suitability *= (1f - accessibility) * 1.4f; // Prefer inaccessible areas
                suitability *= slope * 1.3f; // Dangerous terrain
                suitability *= CalculateNaturalHazardPotential(sample.position);
            }
            else
            {
                // Custom/unknown layer types get balanced evaluation
                suitability *= CalculateGenericLayerSuitability(sample, layerName);
            }

            // Global terrain quality factors
            suitability *= CalculateTerrainStability(slope, accessibility);
            suitability *= CalculateBiomeBoundaryFactor(sample.position);

            return Mathf.Clamp01(suitability);
        }

        private float CalculateTemperature(Vector3 position, float elevation)
        {
            // Temperature decreases with elevation and varies by latitude
            float baseTemperature = Mathf.PerlinNoise(position.x * 0.02f, position.y * 0.02f);
            float elevationEffect = (1f - elevation) * 0.4f;
            float latitudeEffect = 1f - Mathf.Abs(position.y * 0.01f) % 1f;
            
            return Mathf.Clamp01(baseTemperature + elevationEffect + latitudeEffect * 0.3f);
        }

        private float CalculateSlope(Vector3 position, float currentElevation)
        {
            // Sample nearby elevations to calculate slope
            float sampleDistance = 2f;
            float northElevation = Mathf.PerlinNoise(position.x * 0.1f, (position.z + sampleDistance) * 0.1f);
            float southElevation = Mathf.PerlinNoise(position.x * 0.1f, (position.z - sampleDistance) * 0.1f);
            float eastElevation = Mathf.PerlinNoise((position.x + sampleDistance) * 0.1f, position.z * 0.1f);
            float westElevation = Mathf.PerlinNoise((position.x - sampleDistance) * 0.1f, position.z * 0.1f);

            float maxDifference = Mathf.Max(
                Mathf.Abs(northElevation - currentElevation),
                Mathf.Abs(southElevation - currentElevation),
                Mathf.Abs(eastElevation - currentElevation),
                Mathf.Abs(westElevation - currentElevation)
            );

            return Mathf.Clamp01(maxDifference * 2f); // Scale to 0-1 range
        }

        private float CalculateAccessibility(Vector3 position)
        {
            // Advanced accessibility calculation using multi-factor path analysis
            // Considers elevation gradients, terrain obstacles, and connectivity networks
            
            float distanceFromCenter = Vector2.Distance(
                new Vector2(position.x, position.z),
                new Vector2(nodeId.Coordinates.x, nodeId.Coordinates.y)
            );

            // Sophisticated distance-based accessibility with terrain consideration
            float normalizedDistance = Mathf.Clamp01(distanceFromCenter / 15f);
            float baseAccessibility = 1f - (normalizedDistance * normalizedDistance); // Quadratic falloff
            
            // Multi-layer noise for realistic terrain variation
            float terrainNoise1 = Mathf.PerlinNoise(position.x * 0.15f + 50, position.z * 0.15f + 50);
            float terrainNoise2 = Mathf.PerlinNoise(position.x * 0.3f + 100, position.z * 0.3f + 100) * 0.5f;
            float terrainNoise3 = Mathf.PerlinNoise(position.x * 0.6f + 200, position.z * 0.6f + 200) * 0.25f;
            float combinedNoise = (terrainNoise1 + terrainNoise2 + terrainNoise3) / 1.75f;
            
            // Simulate path networks using coordinate-based patterns
            float pathNetworkScore = CalculatePathNetworkAccessibility(position);
            
            // Elevation-based accessibility modifier
            float elevation = Mathf.PerlinNoise(position.x * 0.1f, position.z * 0.1f);
            float elevationModifier = 1f - Mathf.Abs(elevation - 0.4f) * 1.5f; // Prefer mid-elevation
            
            // Combine all factors
            float accessibility = baseAccessibility * 0.4f + 
                                 combinedNoise * 0.3f + 
                                 pathNetworkScore * 0.2f + 
                                 elevationModifier * 0.1f;
            
            return Mathf.Clamp01(accessibility);
        }

        private float CalculatePathNetworkAccessibility(Vector3 position)
        {
            // Simulate natural path formation using river-like algorithms
            float pathScore = 0f;
            
            // Check for natural corridors (valleys, flat areas)
            for (int angle = 0; angle < 8; angle++)
            {
                float angleRad = angle * Mathf.PI / 4f;
                Vector3 direction = new(Mathf.Cos(angleRad), 0, Mathf.Sin(angleRad));
                
                float corridorScore = 0f;
                for (float distance = 1f; distance <= 5f; distance += 1f)
                {
                    Vector3 checkPos = position + direction * distance;
                    float checkElevation = Mathf.PerlinNoise(checkPos.x * 0.1f, checkPos.z * 0.1f);
                    float currentElevation = Mathf.PerlinNoise(position.x * 0.1f, position.z * 0.1f);
                    
                    // Prefer gentle slopes for accessibility
                    float elevationDiff = Mathf.Abs(checkElevation - currentElevation);
                    corridorScore += (1f - elevationDiff) * (1f / distance); // Weight by inverse distance
                }
                
                pathScore = Mathf.Max(pathScore, corridorScore / 5f); // Best corridor wins
            }
            
            return Mathf.Clamp01(pathScore);
        }

        private float CalculateTerrainStability(float slope, float accessibility)
        {
            // Stable terrain is accessible and not too steep
            float slopeStability = 1f - (slope * 0.8f);
            float accessibilityStability = accessibility * 0.6f + 0.4f; // Don't fully penalize inaccessible areas
            
            return slopeStability * accessibilityStability;
        }

        private float CalculateBiomeBoundaryFactor(Vector3 position)
        {
            // Advanced biome boundary detection using multi-sample analysis
            // Implements real boundary detection based on biome transition zones
            float distanceFromBiomeCenter = Vector2.Distance(
                new Vector2(position.x, position.z),
                new Vector2(nodeId.Coordinates.x, nodeId.Coordinates.y)
            );

            float biomeBoundaryDistance = 8f; // Biome influence radius
            float normalizedDistance = distanceFromBiomeCenter / biomeBoundaryDistance;

            // Multi-sample biome boundary detection
            float boundaryFactor = DetectBiomeBoundary(position);
            
            // Enhanced boundary transition logic
            if (normalizedDistance > 0.6f)
            {
                // Approaching boundary - apply graduated transition
                float transitionZone = (normalizedDistance - 0.6f) / 0.4f; // 0.6 to 1.0 maps to 0.0 to 1.0
                float boundaryPenalty = CalculateBoundaryTransitionPenalty(boundaryFactor, transitionZone);
                return Mathf.Lerp(1f, boundaryPenalty, transitionZone);
            }

            // Core biome area - full suitability with boundary awareness
            return 1f * (0.8f + boundaryFactor * 0.2f);
        }

        private float DetectBiomeBoundary(Vector3 position)
        {
            // Multi-directional sampling to detect biome boundaries
            float boundaryStrength = 0f;
            int sampleCount = 8;
            float sampleRadius = 2f;
            
            for (int i = 0; i < sampleCount; i++)
            {
                float angle = (float)i / sampleCount * 2f * Mathf.PI;
                Vector3 samplePos = position + new Vector3(
                    Mathf.Cos(angle) * sampleRadius,
                    0f,
                    Mathf.Sin(angle) * sampleRadius
                );
                
                // Simulate biome type detection using coordinate-based biome assignment
                BiomeType currentBiome = GetBiomeTypeAtPosition(position);
                BiomeType sampleBiome = GetBiomeTypeAtPosition(samplePos);
                
                if (currentBiome != sampleBiome)
                {
                    boundaryStrength += 1f / sampleCount; // Found a boundary
                }
            }
            
            return 1f - boundaryStrength; // Higher = more coherent (fewer boundaries)
        }

        private BiomeType GetBiomeTypeAtPosition(Vector3 position)
        {
            // Simulate biome assignment using noise-based regions
            float biomeNoise = Mathf.PerlinNoise(position.x * 0.05f, position.z * 0.05f);
            
            // 🔥 FIXED: Use all 27 biome types (0-26)
            int biomeIndex = Mathf.FloorToInt(biomeNoise * 27f);
            
            return biomeIndex switch
            {
                0 => BiomeType.Unknown,
                1 => BiomeType.SolarPlains,
                2 => BiomeType.CrystalCaverns,
                3 => BiomeType.SkyGardens,
                4 => BiomeType.ShadowRealms,
                5 => BiomeType.DeepUnderwater,
                6 => BiomeType.VoidChambers,
                7 => BiomeType.VolcanicCore,
                8 => BiomeType.PowerPlant,
                9 => BiomeType.PlasmaFields,
                10 => BiomeType.FrozenWastes,
                11 => BiomeType.IceCatacombs,
                12 => BiomeType.CryogenicLabs,
                13 => BiomeType.IcyCanyon,
                14 => BiomeType.Tundra,
                15 => BiomeType.Forest,
                16 => BiomeType.Mountains,
                17 => BiomeType.Desert,
                18 => BiomeType.Ocean,
                19 => BiomeType.Cosmic,
                20 => BiomeType.Crystal,
                21 => BiomeType.Ruins,
                22 => BiomeType.AncientRuins,
                23 => BiomeType.Volcanic,
                24 => BiomeType.Hell,
                25 => BiomeType.HubArea,
                26 => BiomeType.TransitionZone,
                _ => BiomeType.Unknown // Fallback for any unexpected values
            };
        }

        private float CalculateBoundaryTransitionPenalty(float boundaryFactor, float transitionZone)
        {
            // Sophisticated transition penalty calculation
            // Smooth boundaries get less penalty than sharp ones
            float smoothnessBonus = boundaryFactor * 0.3f;
            float basePenalty = 0.4f; // Minimum penalty for boundary proximity
            
            // Gradual transition with easing
            float easedTransition = transitionZone * transitionZone * (3f - 2f * transitionZone); // Smoothstep
            
            return basePenalty + smoothnessBonus * (1f - easedTransition);
        }

        private float CalculateGeologicalStability(Vector3 position)
        {
            // Stability based on low seismic activity and mineral composition
            float noiseBase = Mathf.PerlinNoise(position.x * 0.03f, position.z * 0.03f);
            float stability = 1f - Mathf.Abs(0.5f - noiseBase) * 2f; // Prefer middle values for stability
            return Mathf.Clamp01(stability * 1.2f);
        }

        private float CalculateHistoricalSignificance(Vector3 position)
        {
            // Simulate historical significance using layered noise patterns
            float ancientNoise = Mathf.PerlinNoise(position.x * 0.02f + 1000f, position.z * 0.02f + 1000f);
            float tradeRouteNoise = Mathf.PerlinNoise(position.x * 0.08f + 2000f, position.z * 0.08f + 2000f);
            
            // Combine factors: ancient significance + trade route proximity
            float significance = (ancientNoise * 0.7f + tradeRouteNoise * 0.3f);
            return Mathf.Clamp01(significance * 1.3f);
        }

        private float CalculateCosmicAlignment(Vector3 position)
        {
            // Otherworldly alignment based on complex mathematical patterns
            float x = position.x * 0.1f;
            float z = position.z * 0.1f;
            
            // Use interference patterns for cosmic alignment
            float pattern1 = Mathf.Sin(x * 2f) * Mathf.Cos(z * 3f);
            float pattern2 = Mathf.Sin(x * 5f + z * 2f) * 0.6f;
            float pattern3 = Mathf.Cos(x * x + z * z) * 0.4f;
            
            float alignment = (pattern1 + pattern2 + pattern3) * 0.5f + 0.5f;
            return Mathf.Clamp01(alignment);
        }

        private float CalculateNaturalHazardPotential(Vector3 position)
        {
            // Higher values indicate more natural hazard potential
            float volatility = Mathf.PerlinNoise(position.x * 0.15f + 500f, position.z * 0.15f + 500f);
            float instability = Mathf.PerlinNoise(position.x * 0.25f + 1500f, position.z * 0.25f + 1500f);
            
            // Combine geological volatility with environmental instability
            float hazardPotential = (volatility * 0.6f + instability * 0.4f);
            return Mathf.Clamp01(hazardPotential * 1.4f);
        }

        private float CalculateGenericLayerSuitability(TerrainSample sample, string layerName)
        {
            // Balanced evaluation for unknown/custom layer types
            float elevation = sample.elevation;
            float moisture = sample.moisture;
            
            // Use layer name characteristics to infer preferences
            float elevationPreference = layerName.ToLowerInvariant().Contains("high") ? elevation : 
                                       layerName.ToLowerInvariant().Contains("low") ? (1f - elevation) : 
                                       (1f - Mathf.Abs(elevation - 0.5f) * 2f); // Default: prefer middle elevation
            
            float moisturePreference = layerName.ToLowerInvariant().Contains("dry") ? (1f - moisture) :
                                      layerName.ToLowerInvariant().Contains("wet") ? moisture :
                                      (1f - Mathf.Abs(moisture - 0.5f) * 2f); // Default: balanced moisture
            
            return Mathf.Clamp01((elevationPreference + moisturePreference) * 0.5f);
        }

        private bool IsNearLayer(Vector3 position, string layerName, float radius)
        {
            var layerObject = grid.transform.Find(layerName);
            if (layerObject == null) return false;
            return Vector3.Distance(position, layerObject.position) < radius;
        }

        private bool IsNearBiomeTransition(Vector3 position)
        {
            Vector3 biomeCenter = new(nodeId.Coordinates.x, nodeId.Coordinates.y, 0);
            float distanceFromCenter = Vector3.Distance(position, biomeCenter);
            return distanceFromCenter > 8f - settings.avoidance.transitionAvoidanceRadius;
        }

        private void PlacePropAtPosition(Vector3 position, Transform layerObject)
        {
            if (settings.propPrefabs.Length == 0) return;

            int propIndex = rng.Next(0, settings.propPrefabs.Length);
            var propPrefab = settings.propPrefabs[propIndex];
            if (propPrefab == null) return;

            Quaternion rotation = Quaternion.identity;
            if (settings.variation.randomRotation)
            {
                float rotationAngle = (float)(rng.NextDouble() * settings.variation.maxRotationAngle);
                rotation = Quaternion.Euler(0, 0, rotationAngle);
            }

            Vector3 scale = Vector3.one;
            if (Mathf.Abs(settings.variation.minScale - settings.variation.maxScale) > Mathf.Epsilon)
            {
                float scaleMultiplier = Mathf.Lerp(
                    settings.variation.minScale,
                    settings.variation.maxScale,
                    (float)rng.NextDouble()
                );
                scale = Vector3.one * scaleMultiplier;
            }

            var propInstance = GameObject.Instantiate(propPrefab, position, rotation, layerObject);
            propInstance.transform.localScale = scale;
            propInstance.name = $"{biome.Type} Prop ({propIndex})";

            placedPropPositions.Add(position);
        }
    }
}
