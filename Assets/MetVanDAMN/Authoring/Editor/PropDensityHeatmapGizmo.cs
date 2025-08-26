using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Authoring;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    /// <summary>
    /// Gizmo overlay system for visualizing prop density heatmaps in the scene view
    /// Addresses TODO: "Additional gizmo overlays (prop preview density heatmap)"
    /// </summary>
    public static class PropDensityHeatmapGizmo
    {
        private static bool s_showHeatmap = false;
        private static float s_heatmapResolution = 2f;
        private static int s_maxSamples = 100;

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawPropDensityHeatmap(BiomeArtProfileAuthoring biomeAuthoring, GizmoType gizmoType)
        {
            if (!s_showHeatmap || biomeAuthoring.artProfile == null) return;

            var profile = biomeAuthoring.artProfile;
            var bounds = CalculateBiomeBounds(biomeAuthoring.transform);
            
            // Sample density across the biome area
            var densityData = SampleDensityGrid(bounds, profile, s_heatmapResolution);
            
            // Draw heatmap visualization
            DrawHeatmapGrid(densityData, bounds);
        }

        private static Bounds CalculateBiomeBounds(Transform biomeTransform)
        {
            // Calculate bounds based on biome size or default to reasonable area
            Vector3 center = biomeTransform.position;
            Vector3 size = new Vector3(20f, 20f, 1f); // Default biome size
            
            // Try to get more accurate bounds from colliders or renderers
            var collider = biomeTransform.GetComponent<Collider>();
            if (collider != null)
            {
                size = collider.bounds.size;
            }
            else
            {
                var renderer = biomeTransform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    size = renderer.bounds.size;
                }
            }
            
            return new Bounds(center, size);
        }

        private static float[,] SampleDensityGrid(Bounds bounds, BiomeArtProfile profile, float resolution)
        {
            int gridWidth = Mathf.RoundToInt(bounds.size.x / resolution);
            int gridHeight = Mathf.RoundToInt(bounds.size.z / resolution);
            
            gridWidth = Mathf.Min(gridWidth, s_maxSamples);
            gridHeight = Mathf.Min(gridHeight, s_maxSamples);
            
            var densityGrid = new float[gridWidth, gridHeight];
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    Vector3 worldPos = new Vector3(
                        bounds.min.x + (x * resolution) + (resolution * 0.5f),
                        bounds.center.y,
                        bounds.min.z + (z * resolution) + (resolution * 0.5f)
                    );
                    
                    densityGrid[x, z] = CalculateExpectedDensity(worldPos, profile);
                }
            }
            
            return densityGrid;
        }

        private static float CalculateExpectedDensity(Vector3 position, BiomeArtProfile profile)
        {
            // Calculate expected prop density based on biome profile settings
            float baseDensity = profile.propSettings.baseDensity;
            float multiplier = profile.propSettings.densityMultiplier;
            
            // Factor in distance-based density curve
            float distanceFromCenter = Vector2.Distance(Vector2.zero, new Vector2(position.x, position.z));
            float normalizedDistance = Mathf.Clamp01(distanceFromCenter / 20f);
            float densityFactor = profile.propSettings.densityCurve.Evaluate(1f - normalizedDistance);
            
            // Terrain-based adjustments
            float elevation = Mathf.PerlinNoise(position.x * 0.1f, position.z * 0.1f);
            float moisture = Mathf.PerlinNoise(position.x * 0.05f + 100, position.z * 0.05f + 100);
            
            float terrainModifier = 1f;
            switch (profile.propSettings.strategy)
            {
                case PropPlacementStrategy.Terrain:
                    terrainModifier = (elevation + moisture) * 0.5f;
                    break;
                case PropPlacementStrategy.Clustered:
                    terrainModifier *= 1.2f; // Clusters tend to have higher local density
                    break;
                case PropPlacementStrategy.Sparse:
                    terrainModifier *= 0.3f; // Sparse placement reduces overall density
                    break;
            }
            
            return baseDensity * multiplier * densityFactor * terrainModifier;
        }

        private static void DrawHeatmapGrid(float[,] densityData, Bounds bounds)
        {
            int width = densityData.GetLength(0);
            int height = densityData.GetLength(1);
            
            float cellWidth = bounds.size.x / width;
            float cellHeight = bounds.size.z / height;
            
            // Find max density for normalization
            float maxDensity = 0f;
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    maxDensity = Mathf.Max(maxDensity, densityData[x, z]);
                }
            }
            
            if (maxDensity <= 0f) return;
            
            // Draw heat map cells
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    Vector3 cellCenter = new Vector3(
                        bounds.min.x + (x * cellWidth) + (cellWidth * 0.5f),
                        bounds.center.y,
                        bounds.min.z + (z * cellHeight) + (cellHeight * 0.5f)
                    );
                    
                    float normalizedDensity = densityData[x, z] / maxDensity;
                    Color heatColor = GetHeatMapColor(normalizedDensity);
                    
                    Gizmos.color = heatColor;
                    Vector3 cellSize = new Vector3(cellWidth, 0.1f, cellHeight);
                    Gizmos.DrawCube(cellCenter, cellSize);
                }
            }
        }

        private static Color GetHeatMapColor(float intensity)
        {
            // Create a heat map color from blue (low) to red (high)
            Color lowColor = new Color(0f, 0f, 1f, 0.3f);  // Blue, semi-transparent
            Color midColor = new Color(1f, 1f, 0f, 0.5f);  // Yellow
            Color highColor = new Color(1f, 0f, 0f, 0.7f); // Red
            
            if (intensity < 0.5f)
            {
                return Color.Lerp(lowColor, midColor, intensity * 2f);
            }
            else
            {
                return Color.Lerp(midColor, highColor, (intensity - 0.5f) * 2f);
            }
        }

        // Menu items for controlling heatmap display
        [MenuItem("Tools/MetVanDAMN/Toggle Prop Density Heatmap")]
        public static void TogglePropDensityHeatmap()
        {
            s_showHeatmap = !s_showHeatmap;
            SceneView.RepaintAll();
            
            Debug.Log($"Prop Density Heatmap: {(s_showHeatmap ? "Enabled" : "Disabled")}");
        }

        [MenuItem("Tools/MetVanDAMN/Heatmap Settings/Low Resolution")]
        public static void SetLowResolution()
        {
            s_heatmapResolution = 4f;
            s_maxSamples = 50;
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/MetVanDAMN/Heatmap Settings/Medium Resolution")]
        public static void SetMediumResolution()
        {
            s_heatmapResolution = 2f;
            s_maxSamples = 100;
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/MetVanDAMN/Heatmap Settings/High Resolution")]
        public static void SetHighResolution()
        {
            s_heatmapResolution = 1f;
            s_maxSamples = 200;
            SceneView.RepaintAll();
        }
    }
}