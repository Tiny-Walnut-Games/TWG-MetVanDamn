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
    /// Enhanced with legend overlay and adjustable color gradient
    /// </summary>
    public static class PropDensityHeatmapGizmo
    {
        private static bool s_showHeatmap = false;
        private static bool s_showLegend = true;
        private static float s_heatmapResolution = 2f;
        private static int s_maxSamples = 100;
        private static HeatmapColorScheme s_colorScheme = HeatmapColorScheme.BlueToRed;
        private static float s_intensityMultiplier = 1f;

        public enum HeatmapColorScheme
        {
            BlueToRed,
            Grayscale,
            Rainbow,
            Green,
            Custom
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawPropDensityHeatmap(BiomeArtProfileAuthoring biomeAuthoring, GizmoType gizmoType)
        {
            if (!s_showHeatmap || biomeAuthoring.artProfile == null)
            {
                return;
            }

            BiomeArtProfile profile = biomeAuthoring.artProfile;
            Bounds bounds = CalculateBiomeBounds(biomeAuthoring.transform);

            // Sample density across the biome area
            float[,] densityData = SampleDensityGrid(bounds, profile, s_heatmapResolution);
            
            // Draw heatmap visualization
            DrawHeatmapGrid(densityData, bounds);
            
            // Draw legend overlay if enabled
            if (s_showLegend)
            {
                DrawHeatmapLegend(densityData, bounds);
            }
        }

        private static Bounds CalculateBiomeBounds(Transform biomeTransform)
        {
            // Calculate bounds based on biome size or default to reasonable area
            Vector3 center = biomeTransform.position;
            Vector3 size = new Vector3(20f, 20f, 1f); // Default biome size

            // Try to get more accurate bounds from colliders or renderers
            Collider collider = biomeTransform.GetComponent<Collider>();
            if (collider != null)
            {
                size = collider.bounds.size;
            }
            else
            {
                Renderer renderer = biomeTransform.GetComponent<Renderer>();
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

            float[,] densityGrid = new float[gridWidth, gridHeight];
            
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
            
            if (maxDensity <= 0f)
            {
                return;
            }

            // Draw heat map cells with enhanced numeric labels
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
                    float actualDensity = densityData[x, z];
                    Color heatColor = GetHeatMapColor(normalizedDensity);
                    
                    // Draw heat map cell
                    Gizmos.color = heatColor;
                    Vector3 cellSize = new Vector3(cellWidth, 0.1f, cellHeight);
                    Gizmos.DrawCube(cellCenter, cellSize);
                    
                    // Draw numeric label using Handles.Label instead of sphere markers
                    if (actualDensity > 0.1f) // Only show labels for significant density values
                    {
                        string densityLabel = actualDensity.ToString("F1");
                        Vector3 labelPosition = cellCenter + Vector3.up * 0.5f;
                        
                        // Use contrasting color for label based on background intensity
                        var labelStyle = new GUIStyle();
                        labelStyle.normal.textColor = normalizedDensity > 0.5f ? Color.white : Color.black;
                        labelStyle.fontSize = 10;
                        labelStyle.alignment = TextAnchor.MiddleCenter;
                        
                        UnityEditor.Handles.Label(labelPosition, densityLabel, labelStyle);
                    }
                }
            }
        }

        private static Color GetHeatMapColor(float intensity)
        {
            // Apply intensity multiplier for user adjustment
            intensity = Mathf.Clamp01(intensity * s_intensityMultiplier);
            
            return s_colorScheme switch
            {
                HeatmapColorScheme.BlueToRed => GetBlueToRedGradient(intensity),
                HeatmapColorScheme.Grayscale => GetGrayscaleGradient(intensity),
                HeatmapColorScheme.Rainbow => GetRainbowGradient(intensity),
                HeatmapColorScheme.Green => GetGreenGradient(intensity),
                HeatmapColorScheme.Custom => GetCustomGradient(intensity),
                _ => GetBlueToRedGradient(intensity)
            };
        }

        private static Color GetBlueToRedGradient(float intensity)
        {
            // Classic heat map: Blue (low) -> Yellow -> Red (high)
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

        private static Color GetGrayscaleGradient(float intensity)
        {
            float alpha = Mathf.Lerp(0.2f, 0.8f, intensity);
            return new Color(intensity, intensity, intensity, alpha);
        }

        private static Color GetRainbowGradient(float intensity)
        {
            // HSV rainbow from purple to red
            Color color = Color.HSVToRGB(Mathf.Lerp(0.8f, 0f, intensity), 1f, 1f);
            color.a = Mathf.Lerp(0.3f, 0.8f, intensity);
            return color;
        }

        private static Color GetGreenGradient(float intensity)
        {
            // Dark green to bright green
            Color lowColor = new Color(0f, 0.2f, 0f, 0.3f);
            Color highColor = new Color(0f, 1f, 0f, 0.8f);
            return Color.Lerp(lowColor, highColor, intensity);
        }

        private static Color GetCustomGradient(float intensity)
        {
            // Customizable gradient - could be exposed to user preferences
            Color lowColor = new Color(0.2f, 0f, 0.8f, 0.3f);  // Purple
            Color midColor = new Color(1f, 0.5f, 0f, 0.5f);    // Orange
            Color highColor = new Color(1f, 1f, 1f, 0.8f);     // White
            
            if (intensity < 0.5f)
            {
                return Color.Lerp(lowColor, midColor, intensity * 2f);
            }
            else
            {
                return Color.Lerp(midColor, highColor, (intensity - 0.5f) * 2f);
            }
        }

        /// <summary>
        /// Draws a legend overlay showing the heatmap color scale and density values
        /// </summary>
        private static void DrawHeatmapLegend(float[,] densityData, Bounds bounds)
        {
            // Calculate legend position (offset from the heatmap area)
            Vector3 legendPosition = bounds.max + new Vector3(2f, 0f, 0f);
            
            // Find min/max density for the legend scale
            float minDensity = float.MaxValue;
            float maxDensity = float.MinValue;
            
            int width = densityData.GetLength(0);
            int height = densityData.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    float density = densityData[x, z];
                    minDensity = Mathf.Min(minDensity, density);
                    maxDensity = Mathf.Max(maxDensity, density);
                }
            }
            
            if (maxDensity <= minDensity)
            {
                return;
            }

            // Draw legend gradient bar
            int legendSteps = 10;
            float legendHeight = 5f;
            float legendWidth = 0.5f;
            
            for (int i = 0; i < legendSteps; i++)
            {
                float t = (float)i / (legendSteps - 1);
                Color legendColor = GetHeatMapColor(t);
                
                Vector3 stepPosition = legendPosition + new Vector3(0f, 0f, t * legendHeight);
                Vector3 stepSize = new Vector3(legendWidth, 0.1f, legendHeight / legendSteps * 1.1f);
                
                Gizmos.color = legendColor;
                Gizmos.DrawCube(stepPosition, stepSize);
            }
            
            // Draw legend outline
            Gizmos.color = Color.white;
            Vector3 outlineCenter = legendPosition + new Vector3(0f, 0f, legendHeight * 0.5f);
            Vector3 outlineSize = new Vector3(legendWidth * 1.2f, 0.12f, legendHeight * 1.1f);
            Gizmos.DrawWireCube(outlineCenter, outlineSize);
            
            // Draw density value labels (using GL for text rendering in scene view)
            DrawLegendLabels(legendPosition, legendHeight, minDensity, maxDensity);
        }

        private static void DrawLegendLabels(Vector3 legendPosition, float legendHeight, float minDensity, float maxDensity)
        {
            // Use Handles.Label for proper text rendering in scene view
            float[] labelPositions = { 0f, 0.25f, 0.5f, 0.75f, 1f };
            string[] labelTexts = { "Min", "Low", "Med", "High", "Max" };
            
            for (int i = 0; i < labelPositions.Length; i++)
            {
                float t = labelPositions[i];
                Vector3 labelPos = legendPosition + new Vector3(1.2f, 0f, t * legendHeight);
                float densityValue = Mathf.Lerp(minDensity, maxDensity, t);
                
                // Create comprehensive label with value and description
                string labelText = $"{labelTexts[i]}\n{densityValue:F1}";
                
                // Set up label style for better visibility
                var labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = Color.white },
                    fontSize = 10,
                    alignment = TextAnchor.MiddleLeft
                };
                
                // Add black outline for better readability
                var outlineStyle = new GUIStyle(labelStyle)
                {
                    normal = { textColor = Color.black }
                };
                
                // Draw outline (shadow effect)
                Vector3 offset = new Vector3(0.02f, 0f, 0.02f);
                Handles.Label(labelPos + offset, labelText, outlineStyle);
                Handles.Label(labelPos - offset, labelText, outlineStyle);
                Handles.Label(labelPos + new Vector3(0.02f, 0f, -0.02f), labelText, outlineStyle);
                Handles.Label(labelPos + new Vector3(-0.02f, 0f, 0.02f), labelText, outlineStyle);
                
                // Draw main label
                Handles.Label(labelPos, labelText, labelStyle);
                
                // Draw small sphere marker for visual reference
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(labelPos + new Vector3(-0.3f, 0f, 0f), 0.05f);
            }
            
            // Add legend title
            Vector3 titlePos = legendPosition + new Vector3(1.2f, 0.5f, legendHeight + 0.5f);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.yellow },
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
            
            Handles.Label(titlePos, "Prop Density", titleStyle);
        }

        // Menu items for controlling heatmap display
        [MenuItem("Tools/MetVanDAMN/Toggle Prop Density Heatmap")]
        public static void TogglePropDensityHeatmap()
        {
            s_showHeatmap = !s_showHeatmap;
            SceneView.RepaintAll();
            
            Debug.Log($"Prop Density Heatmap: {(s_showHeatmap ? "Enabled" : "Disabled")}");
        }

        [MenuItem("Tools/MetVanDAMN/Toggle Heatmap Legend")]
        public static void ToggleHeatmapLegend()
        {
            s_showLegend = !s_showLegend;
            SceneView.RepaintAll();
            
            Debug.Log($"Heatmap Legend: {(s_showLegend ? "Enabled" : "Disabled")}");
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

        // Color scheme menu items
        [MenuItem("Tools/MetVanDAMN/Heatmap Colors/Blue to Red")]
        public static void SetBlueToRedColors()
        {
            s_colorScheme = HeatmapColorScheme.BlueToRed;
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/MetVanDAMN/Heatmap Colors/Grayscale")]
        public static void SetGrayscaleColors()
        {
            s_colorScheme = HeatmapColorScheme.Grayscale;
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/MetVanDAMN/Heatmap Colors/Rainbow")]
        public static void SetRainbowColors()
        {
            s_colorScheme = HeatmapColorScheme.Rainbow;
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/MetVanDAMN/Heatmap Colors/Green Gradient")]
        public static void SetGreenColors()
        {
            s_colorScheme = HeatmapColorScheme.Green;
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/MetVanDAMN/Heatmap Colors/Custom")]
        public static void SetCustomColors()
        {
            s_colorScheme = HeatmapColorScheme.Custom;
            SceneView.RepaintAll();
        }

        // Intensity adjustment menu items
        [MenuItem("Tools/MetVanDAMN/Heatmap Intensity/Low (0.5x)")]
        public static void SetLowIntensity()
        {
            s_intensityMultiplier = 0.5f;
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/MetVanDAMN/Heatmap Intensity/Normal (1.0x)")]
        public static void SetNormalIntensity()
        {
            s_intensityMultiplier = 1f;
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/MetVanDAMN/Heatmap Intensity/High (2.0x)")]
        public static void SetHighIntensity()
        {
            s_intensityMultiplier = 2f;
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/MetVanDAMN/Heatmap Intensity/Very High (3.0x)")]
        public static void SetVeryHighIntensity()
        {
            s_intensityMultiplier = 3f;
            SceneView.RepaintAll();
        }

        // Legend visibility toggle
        [MenuItem("Tools/MetVanDAMN/Heatmap/Toggle Legend Visibility")]
        public static void ToggleLegendVisibility()
        {
            s_showLegend = !s_showLegend;
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/MetVanDAMN/Heatmap/Toggle Legend Visibility", true)]
        public static bool ToggleLegendVisibility_Validate()
        {
            Menu.SetChecked("Tools/MetVanDAMN/Heatmap/Toggle Legend Visibility", s_showLegend);
            return true;
        }
    }
}