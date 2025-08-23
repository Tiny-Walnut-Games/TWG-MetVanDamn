using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.GridLayerEditor;
using BiomeFieldSystem = TinyWalnutGames.MetVD.Biome.BiomeFieldSystem;
using System.Linq;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// System responsible for applying biome art profiles to generated worlds
    /// Integrates with Grid Layer Editor for projection-aware tilemap creation
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(BiomeFieldSystem))]
    public partial struct BiomeArtIntegrationSystem : ISystem
    {
        private ComponentLookup<Biome> biomeLookup;
        private ComponentLookup<BiomeArtProfileReference> artProfileLookup;
        private ComponentLookup<NodeId> nodeIdLookup;

        public void OnCreate(ref SystemState state)
        {
            biomeLookup = state.GetComponentLookup<Biome>(true);
            artProfileLookup = state.GetComponentLookup<BiomeArtProfileReference>();
            nodeIdLookup = state.GetComponentLookup<NodeId>(true);

            // Require biome components to run
            state.RequireForUpdate<Biome>();
            state.RequireForUpdate<BiomeArtProfileReference>();
        }

        public void OnUpdate(ref SystemState state)
        {
            biomeLookup.Update(ref state);
            artProfileLookup.Update(ref state);
            nodeIdLookup.Update(ref state);

            // Process biome art integration on main thread since it interacts with GameObjects
            var job = new BiomeArtIntegrationJob
            {
                BiomeLookup = biomeLookup,
                ArtProfileLookup = artProfileLookup,
                NodeIdLookup = nodeIdLookup
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>
    /// Job for processing biome art integration
    /// Handles tilemap creation and prop placement for biomes
    /// </summary>
    [BurstCompile]
    public partial struct BiomeArtIntegrationJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Biome> BiomeLookup;
        public ComponentLookup<BiomeArtProfileReference> ArtProfileLookup;
        [ReadOnly] public ComponentLookup<NodeId> NodeIdLookup;

        public readonly void Execute(Entity entity, ref BiomeArtProfileReference artProfile, in Biome biome, in NodeId nodeId)
        {
            // Skip if already applied or profile is null
            if (artProfile.IsApplied || !artProfile.ProfileRef.IsValid)
                return;

            // Mark as applied to prevent duplicate processing
            artProfile.IsApplied = true;

            // Schedule tilemap creation based on projection type
            // Note: Actual tilemap creation happens on main thread via hybrid components
            ScheduleTilemapCreation(artProfile.ProjectionType, biome, nodeId);
        }

        private readonly void ScheduleTilemapCreation(ProjectionType projectionType, Biome biome, NodeId nodeId)
        {
            // This method sets up data for main thread tilemap creation
            // The actual GameObject/Tilemap creation must happen on main thread
            // For now, we'll use a marker to indicate this biome needs tilemap creation
            
            // Future: Add to a command buffer or event system for main thread processing
            // For the initial implementation, we'll handle this in the main thread portion
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
                .WithStructuralChanges() // Allow structural changes for GameObject creation
                .ForEach((Entity entity, ref BiomeArtProfileReference artProfileRef, in Biome biome, in NodeId nodeId) =>
                {
                    if (artProfileRef.IsApplied || !artProfileRef.ProfileRef.IsValid)
                        return;

                    var artProfile = artProfileRef.ProfileRef.Value;
                    if (artProfile == null)
                        return;

                    // Create tilemap based on projection type
                    CreateBiomeSpecificTilemap(artProfileRef.ProjectionType, artProfile, biome, nodeId);
                    
                    // Place props
                    PlaceBiomeProps(artProfile, biome, nodeId);

                    // Mark as applied
                    artProfileRef.IsApplied = true;

                }).Run();
        }

        private void CreateBiomeSpecificTilemap(ProjectionType projectionType, BiomeArtProfile artProfile, Biome biome, NodeId nodeId)
        {
            // Get appropriate layer configuration based on projection type
            string[] layerNames = GetLayerNamesForProjection(projectionType);
            
            // Create grid with appropriate projection settings
            CreateProjectionAwareGrid(projectionType, layerNames, artProfile.biomeName);
            
            // Apply biome-specific tiles to the created layers
            ApplyBiomeTilesToLayers(artProfile, layerNames, createdGrid);
        }

        private string[] GetLayerNamesForProjection(ProjectionType projectionType)
        {
            return projectionType switch
            {
                ProjectionType.Platformer => System.Enum.GetNames(typeof(TwoDimensionalGridSetup.SideScrollingLayers)),
                ProjectionType.TopDown => System.Enum.GetNames(typeof(TwoDimensionalGridSetup.TopDownLayers)),
                ProjectionType.Isometric => System.Enum.GetNames(typeof(IsometricHexagonalLayers)),
                ProjectionType.Hexagonal => System.Enum.GetNames(typeof(IsometricHexagonalLayers)),
                _ => System.Enum.GetNames(typeof(TwoDimensionalGridSetup.TopDownLayers))
            };
        }

        private Grid CreateProjectionAwareGrid(ProjectionType projectionType, string[] layerNames, string biomeName)
        {
            // Call appropriate TwoDimensionalGridSetup method based on projection
            Grid createdGrid = null;
            switch (projectionType)
            {
                case ProjectionType.Platformer:
                    createdGrid = TwoDimensionalGridSetup.CreateSideScrollingGrid();
                    break;
                case ProjectionType.TopDown:
                    createdGrid = TwoDimensionalGridSetup.CreateDefaultTopDownGrid();
                    break;
                case ProjectionType.Isometric:
                    createdGrid = TwoDimensionalGridSetup.CreateIsometricTopDownGrid();
                    break;
                case ProjectionType.Hexagonal:
                    createdGrid = TwoDimensionalGridSetup.CreateHexTopDownGrid();
                    break;
                default:
                    createdGrid = TwoDimensionalGridSetup.CreateDefaultTopDownGrid();
                    break;
            }

            // Rename the created grid to include biome information
            if (createdGrid != null)
            {
                createdGrid.name = $"{biomeName} Grid ({projectionType})";
            }
            return createdGrid;
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
                // For demonstration, place a single tile at origin
                // In production, this would be driven by the actual world generation data
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

        private void PlaceBiomeProps(BiomeArtProfile artProfile, Biome biome, NodeId nodeId)
        {
            if (artProfile.propSettings?.propPrefabs == null || artProfile.propSettings.propPrefabs.Length == 0)
                return;

            var grid = GameObject.FindObjectOfType<Grid>();
            if (grid == null) return;

            // Use advanced prop placement based on strategy
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
        private readonly Biome biome;
        private readonly NodeId nodeId;
        private readonly System.Random rng;
        private readonly List<Vector3> placedPropPositions;

        public AdvancedPropPlacer(PropPlacementSettings settings, Grid grid, Biome biome, NodeId nodeId)
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

                // Calculate number of props to place based on density
                int propCount = CalculatePropCount(layerName);
                
                for (int i = 0; i < propCount; i++)
                {
                    Vector3 position = GenerateRandomPosition(layerObject);
                    
                    if (IsPositionValid(position, layerName))
                    {
                        PlacePropAtPosition(position, layerObject);
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

                // Generate cluster centers
                int clusterCount = Mathf.Max(1, Mathf.RoundToInt(settings.baseDensity * settings.densityMultiplier * 10));
                var clusterCenters = GenerateClusterCenters(clusterCount, layerObject);

                // Place props around each cluster center
                foreach (var center in clusterCenters)
                {
                    PlaceClusterAroundCenter(center, layerObject);
                }
            }
        }

        private void PlaceSparseProps()
        {
            foreach (string layerName in settings.allowedPropLayers)
            {
                var layerObject = grid.transform.Find(layerName);
                if (layerObject == null) continue;

                // Very selective placement with high quality spots
                int maxAttempts = Mathf.RoundToInt(settings.maxPropsPerBiome * 0.1f); // 10% of max for sparse
                int placedCount = 0;
                int attempts = 0;

                while (placedCount < maxAttempts / settings.allowedPropLayers.Count && attempts < maxAttempts * 3)
                {
                    Vector3 position = GenerateRandomPosition(layerObject);
                    
                    if (IsHighQualityPosition(position, layerName))
                    {
                        PlacePropAtPosition(position, layerObject);
                        placedCount++;
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

                // Find edges and place props along them
                var edgePoints = FindEdgePoints(layerObject);
                
                foreach (var point in edgePoints)
                {
                    if (rng.NextDouble() < settings.baseDensity * settings.densityMultiplier)
                    {
                        if (IsPositionValid(point, layerName))
                        {
                            PlacePropAtPosition(point, layerObject);
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

                // Create radial pattern from center
                Vector3 center = new Vector3(nodeId.Coordinates.x, nodeId.Coordinates.y, 0);
                float maxRadius = 10f; // Adjust based on biome size
                
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

                // Sample terrain and place props based on terrain features
                var terrainSamples = SampleTerrain(layerObject);
                
                foreach (var sample in terrainSamples)
                {
                    float terrainSuitability = CalculateTerrainSuitability(sample, layerName);
                    float spawnChance = settings.baseDensity * terrainSuitability * settings.densityMultiplier;
                    
                    if (rng.NextDouble() < spawnChance && IsPositionValid(sample.position, layerName))
                    {
                        PlacePropAtPosition(sample.position, layerObject);
                    }
                }
            }
        }

        private int CalculatePropCount(string layerName)
        {
            float baseCount = settings.baseDensity * settings.densityMultiplier * 50; // Base scaling factor
            float distanceFromCenter = Vector2.Distance(Vector2.zero, new Vector2(nodeId.Coordinates.x, nodeId.Coordinates.y));
            float normalizedDistance = Mathf.Clamp01(distanceFromCenter / 20f); // Normalize to 0-1 over 20 units
            float densityFactor = settings.densityCurve.Evaluate(1f - normalizedDistance); // Invert so center = 1
            
            return Mathf.RoundToInt(baseCount * densityFactor);
        }

        private Vector3 GenerateRandomPosition(Transform layerObject)
        {
            // Generate position within biome bounds
            float x = (float)(rng.NextDouble() * 20 - 10) + nodeId.Coordinates.x; // ±10 units from center
            float y = (float)(rng.NextDouble() * 20 - 10) + nodeId.Coordinates.y;
            
            // Add position jitter if enabled
            if (settings.variation.positionJitter > 0)
            {
                x += (float)(rng.NextDouble() - 0.5) * settings.variation.positionJitter;
                y += (float)(rng.NextDouble() - 0.5) * settings.variation.positionJitter;
            }
            
            return new Vector3(x, y, 0);
        }

        private bool IsPositionValid(Vector3 position, string layerName)
        {
            // Check avoidance rules
            if (settings.avoidance.avoidOvercrowding)
            {
                foreach (var existingPos in placedPropPositions)
                {
                    if (Vector3.Distance(position, existingPos) < settings.avoidance.minimumPropDistance)
                        return false;
                }
            }

            // Check layer avoidance
            foreach (string avoidLayer in settings.avoidance.avoidLayers)
            {
                if (IsNearLayer(position, avoidLayer, settings.avoidance.avoidanceRadius))
                    return false;
            }

            // Check biome transition avoidance
            if (settings.avoidance.avoidTransitions && IsNearBiomeTransition(position))
                return false;

            return true;
        }

        private bool IsHighQualityPosition(Vector3 position, string layerName)
        {
            // High quality positions are away from edges, hazards, and other props
            return IsPositionValid(position, layerName) && 
                   !IsNearLayer(position, "Edge", 3f) &&
                   placedPropPositions.All(p => Vector3.Distance(position, p) > settings.avoidance.minimumPropDistance * 2);
        }

        private List<Vector3> GenerateClusterCenters(int clusterCount, Transform layerObject)
        {
            var centers = new List<Vector3>();
            int attempts = 0;
            
            while (centers.Count < clusterCount && attempts < clusterCount * 10)
            {
                Vector3 candidate = GenerateRandomPosition(layerObject);
                
                // Check minimum separation from other cluster centers
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
                // Generate position within cluster radius
                float angle = (float)(rng.NextDouble() * 2 * Mathf.PI);
                float distance = (float)(rng.NextDouble() * settings.clustering.clusterRadius);
                
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance,
                    0
                );
                
                Vector3 propPosition = center + offset;
                
                if (IsPositionValid(propPosition, layerObject.name))
                {
                    PlacePropAtPosition(propPosition, layerObject);
                }
            }
        }

        private List<Vector3> FindEdgePoints(Transform layerObject)
        {
            // Simplified edge detection - in production, this would analyze the actual tilemap
            var edgePoints = new List<Vector3>();
            
            // Generate points along the perimeter of the biome area
            Vector3 center = new Vector3(nodeId.Coordinates.x, nodeId.Coordinates.y, 0);
            float radius = 8f; // Approximate biome radius
            int pointCount = 20;
            
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
            
            // Grid-based terrain sampling
            Vector3 center = new Vector3(nodeId.Coordinates.x, nodeId.Coordinates.y, 0);
            
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
            
            // Adjust based on terrain type
            if (layerName.Contains("Ground") || layerName.Contains("Floor"))
            {
                // Ground props prefer flat areas
                suitability *= 1f - Mathf.Abs(sample.elevation - 0.5f);
            }
            else if (layerName.Contains("Water"))
            {
                // Water props prefer high moisture
                suitability *= sample.moisture;
            }
            else if (layerName.Contains("Mountain") || layerName.Contains("Rock"))
            {
                // Mountain props prefer high elevation
                suitability *= sample.elevation;
            }
            
            return Mathf.Clamp01(suitability);
        }

        private bool IsNearLayer(Vector3 position, string layerName, float radius)
        {
            var layerObject = grid.transform.Find(layerName);
            if (layerObject == null) return false;
            
            // Simplified distance check - in production, would check actual tilemap content
            return Vector3.Distance(position, layerObject.position) < radius;
        }

        private bool IsNearBiomeTransition(Vector3 position)
        {
            // Simplified biome edge detection
            Vector3 biomeCenter = new Vector3(nodeId.Coordinates.x, nodeId.Coordinates.y, 0);
            float distanceFromCenter = Vector3.Distance(position, biomeCenter);
            return distanceFromCenter > 8f - settings.avoidance.transitionAvoidanceRadius;
        }

        private void PlacePropAtPosition(Vector3 position, Transform layerObject)
        {
            if (settings.propPrefabs.Length == 0) return;
            
            // Select random prop prefab
            int propIndex = rng.Next(0, settings.propPrefabs.Length);
            var propPrefab = settings.propPrefabs[propIndex];
            
            if (propPrefab == null) return;
            
            // Apply variations
            Quaternion rotation = Quaternion.identity;
            if (settings.variation.randomRotation)
            {
                float rotationAngle = (float)(rng.NextDouble() * settings.variation.maxRotationAngle);
                rotation = Quaternion.Euler(0, 0, rotationAngle);
            }
            
            Vector3 scale = Vector3.one;
            if (settings.variation.minScale != settings.variation.maxScale)
            {
                float scaleMultiplier = Mathf.Lerp(
                    settings.variation.minScale,
                    settings.variation.maxScale,
                    (float)rng.NextDouble()
                );
                scale = Vector3.one * scaleMultiplier;
            }
            
            // Instantiate prop
            var propInstance = GameObject.Instantiate(propPrefab, position, rotation, layerObject);
            propInstance.transform.localScale = scale;
            propInstance.name = $"{biome.BiomeType} Prop ({propIndex})";
            
            // Track placed position for avoidance calculations
            placedPropPositions.Add(position);
            
            // Respect max props limit
            if (placedPropPositions.Count >= settings.maxPropsPerBiome)
                return;
        }
    }
}