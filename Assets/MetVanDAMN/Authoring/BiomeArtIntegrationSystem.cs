using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using TinyWalnutGames.MetVD.Core; // For NodeId, Biome component
using TinyWalnutGames.MetVD.Biome;
using TinyWalnutGames.GridLayerEditor;
using BiomeFieldSystem = TinyWalnutGames.MetVD.Biome.BiomeFieldSystem;
using System.Linq;
using System.Collections.Generic; // Needed for List<>

// Disambiguate Biome component from potential namespace collisions
using CoreBiome = TinyWalnutGames.MetVD.Core.Biome;

namespace TinyWalnutGames.MetVD.Authoring
{
    /// <summary>
    /// System responsible for applying biome art profiles to generated worlds
    /// Integrates with Grid Layer Editor for projection-aware tilemap creation
    /// NOTE: Current implementation performs all work on main thread via BiomeArtMainThreadSystem.
    /// This integration system is a placeholder for potential pre-pass / tagging logic.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(BiomeFieldSystem))]
    public partial struct BiomeArtIntegrationSystem : ISystem
    {
        private ComponentLookup<CoreBiome> biomeLookup;
        private ComponentLookup<BiomeArtProfileReference> artProfileLookup;
        private ComponentLookup<NodeId> nodeIdLookup;

        public void OnCreate(ref SystemState state)
        {
            biomeLookup = state.GetComponentLookup<CoreBiome>(true);
            artProfileLookup = state.GetComponentLookup<BiomeArtProfileReference>();
            nodeIdLookup = state.GetComponentLookup<NodeId>(true);

            // Require biome components to run
            state.RequireForUpdate<CoreBiome>();
            state.RequireForUpdate<BiomeArtProfileReference>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Currently no pre-pass job required; main-thread system handles creation.
            biomeLookup.Update(ref state);
            artProfileLookup.Update(ref state);
            nodeIdLookup.Update(ref state);
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
            var existing = Object.FindObjectsByType<Grid>((FindObjectsSortMode)FindObjectsInactive.Include);
            HashSet<Grid> before = new(existing);
            InvokeProjectionCreation(projectionType);
            Grid createdGrid = Object.FindObjectsByType<Grid>((FindObjectsSortMode)FindObjectsInactive.Include)
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
                            var instMat = Object.Instantiate(r.sharedMaterial);
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
            // Removed IsometricHexagonalLayers (not found). Use top-down layers for isometric/hex as fallback.
            return projectionType switch
            {
                ProjectionType.Platformer => System.Enum.GetNames(typeof(TwoDimensionalGridSetup.SideScrollingLayers)),
                ProjectionType.TopDown => System.Enum.GetNames(typeof(TwoDimensionalGridSetup.TopDownLayers)),
                ProjectionType.Isometric => System.Enum.GetNames(typeof(TwoDimensionalGridSetup.TopDownLayers)),
                ProjectionType.Hexagonal => System.Enum.GetNames(typeof(TwoDimensionalGridSetup.TopDownLayers)),
                _ => System.Enum.GetNames(typeof(TwoDimensionalGridSetup.TopDownLayers))
            };
        }

        private void InvokeProjectionCreation(ProjectionType projectionType)
        {
            switch (projectionType)
            {
                case ProjectionType.Platformer:
                    TwoDimensionalGridSetup.CreateSideScrollingGrid();
                    break;
                case ProjectionType.TopDown:
                    TwoDimensionalGridSetup.CreateDefaultTopDownGrid();
                    break;
                case ProjectionType.Isometric:
                    TwoDimensionalGridSetup.CreateIsometricTopDownGrid();
                    break;
                case ProjectionType.Hexagonal:
                    TwoDimensionalGridSetup.CreateHexTopDownGrid();
                    break;
                default:
                    TwoDimensionalGridSetup.CreateDefaultTopDownGrid();
                    break;
            }
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
                grid = GameObject.FindObjectsByType<Grid>((FindObjectsSortMode)FindObjectsInactive.Include)
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
                        if (placedPropPositions.Count >= settings.maxPropsPerBiome) return;
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
                    if (placedPropPositions.Count >= settings.maxPropsPerBiome) return;
                }
            }
        }

        private void PlaceSparseProps()
        {
            foreach (string layerName in settings.allowedPropLayers)
            {
                var layerObject = grid.transform.Find(layerName);
                if (layerObject == null) continue;

                int maxAttempts = Mathf.RoundToInt(settings.maxPropsPerBiome * 0.1f);
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
                        if (placedPropPositions.Count >= settings.maxPropsPerBiome) return;
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
                            if (placedPropPositions.Count >= settings.maxPropsPerBiome) return;
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
                            if (placedPropPositions.Count >= settings.maxPropsPerBiome) return;
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
                        if (placedPropPositions.Count >= settings.maxPropsPerBiome) return;
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
                    if (placedPropPositions.Count >= settings.maxPropsPerBiome) return;
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

            if (layerName.Contains("Ground") || layerName.Contains("Floor"))
            {
                suitability *= 1f - Mathf.Abs(sample.elevation - 0.5f);
            }
            else if (layerName.Contains("Water"))
            {
                suitability *= sample.moisture;
            }
            else if (layerName.Contains("Mountain") || layerName.Contains("Rock"))
            {
                suitability *= sample.elevation;
            }

            return Mathf.Clamp01(suitability);
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
