using UnityEngine;
using UnityEditor;
using TinyWalnutGames.MetVD.Authoring;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    /// <summary>
    /// Editor menu items for creating sample BiomeArtProfile assets
    /// Provides quick access to B+/A-level prop placement configurations
    /// </summary>
    public static class BiomeArtProfileEditorMenus
    {
        private const string MENU_BASE = "Assets/Create/MetVanDAMN/Sample Biome Profiles/";
        private const string SAVE_PATH = "Assets/MetVanDAMN/SampleProfiles/";

        [MenuItem(MENU_BASE + "Forest Biome (Clustered)", false, 1)]
        public static void CreateForestBiomeSample()
        {
            BiomeArtProfile profile = BiomeArtProfileSamples.CreateForestBiomeSample();
            SaveProfileAsset(profile, "ForestBiome_ClusteredSample");
        }

        [MenuItem(MENU_BASE + "Desert Biome (Radial)", false, 2)]
        public static void CreateDesertBiomeSample()
        {
            BiomeArtProfile profile = BiomeArtProfileSamples.CreateDesertBiomeSample();
            SaveProfileAsset(profile, "DesertBiome_RadialSample");
        }

        [MenuItem(MENU_BASE + "Mountain Biome (Terrain-Aware)", false, 3)]
        public static void CreateMountainBiomeSample()
        {
            BiomeArtProfile profile = BiomeArtProfileSamples.CreateMountainBiomeSample();
            SaveProfileAsset(profile, "MountainBiome_TerrainSample");
        }

        [MenuItem(MENU_BASE + "Coastal Biome (Linear)", false, 4)]
        public static void CreateCoastalBiomeSample()
        {
            BiomeArtProfile profile = BiomeArtProfileSamples.CreateCoastalBiomeSample();
            SaveProfileAsset(profile, "CoastalBiome_LinearSample");
        }

        [MenuItem(MENU_BASE + "Urban Biome (Sparse)", false, 5)]
        public static void CreateUrbanBiomeSample()
        {
            BiomeArtProfile profile = BiomeArtProfileSamples.CreateUrbanBiomeSample();
            SaveProfileAsset(profile, "UrbanBiome_SparseSample");
        }

        [MenuItem(MENU_BASE + "Create All Sample Profiles", false, 20)]
        public static void CreateAllSampleProfiles()
        {
            // Ensure the sample profiles directory exists
            if (!AssetDatabase.IsValidFolder(SAVE_PATH.TrimEnd('/')))
            {
                string parentPath = System.IO.Path.GetDirectoryName(SAVE_PATH.TrimEnd('/'));
                string folderName = System.IO.Path.GetFileName(SAVE_PATH.TrimEnd('/'));
                AssetDatabase.CreateFolder(parentPath, folderName);
            }

            CreateForestBiomeSample();
            CreateDesertBiomeSample();
            CreateMountainBiomeSample();
            CreateCoastalBiomeSample();
            CreateUrbanBiomeSample();

            EditorUtility.DisplayDialog("Sample Profiles Created", 
                "All sample BiomeArtProfile assets have been created in " + SAVE_PATH + 
                "\n\nThese demonstrate B+/A-level prop placement strategies:\n" +
                "• Forest: Clustered vegetation\n" +
                "• Desert: Radial oasis distribution\n" +
                "• Mountain: Terrain-aware placement\n" +
                "• Coastal: Linear shoreline features\n" +
                "• Urban: Sparse landmark placement", 
                "OK");
        }

        private static void SaveProfileAsset(BiomeArtProfile profile, string fileName)
        {
            // Ensure the directory exists
            if (!AssetDatabase.IsValidFolder(SAVE_PATH.TrimEnd('/')))
            {
                string parentPath = System.IO.Path.GetDirectoryName(SAVE_PATH.TrimEnd('/'));
                string folderName = System.IO.Path.GetFileName(SAVE_PATH.TrimEnd('/'));
                AssetDatabase.CreateFolder(parentPath, folderName);
            }

            string assetPath = SAVE_PATH + fileName + ".asset";
            
            // Check if asset already exists
            if (AssetDatabase.LoadAssetAtPath<BiomeArtProfile>(assetPath) != null)
            {
                if (!EditorUtility.DisplayDialog("Asset Exists", 
                    $"A BiomeArtProfile already exists at {assetPath}. Overwrite it?", 
                    "Overwrite", "Cancel"))
                {
                    return;
                }
            }

            AssetDatabase.CreateAsset(profile, assetPath);
            AssetDatabase.SaveAssets();
            
            // Select and ping the created asset
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
            
            Debug.Log($"Created sample BiomeArtProfile: {assetPath}");
        }
    }

    /// <summary>
    /// Custom inspector for BiomeArtProfile with enhanced prop placement visualization
    /// Provides real-time feedback on advanced placement settings
    /// </summary>
    [CustomEditor(typeof(BiomeArtProfile))]
    public class BiomeArtProfileInspector : UnityEditor.Editor
    {
        private BiomeArtProfile profile;
        private bool showAdvancedSettings = true;
        private bool showPreviewStats = true;

        public override void OnInspectorGUI()
        {
            profile = (BiomeArtProfile)target;
            
            // Header with biome information
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Biome: {profile.biomeName}", EditorStyles.boldLabel);
            
            if (!string.IsNullOrEmpty(profile.biomeName))
            {
                EditorGUILayout.LabelField($"Debug Color:", EditorStyles.miniLabel);
                EditorGUI.DrawRect(GUILayoutUtility.GetRect(100, 20), profile.debugColor);
            }

            EditorGUILayout.Space();

            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space();

            // Advanced settings section
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Prop Placement Analysis", true);
            if (showAdvancedSettings && profile.propSettings != null)
            {
                DrawAdvancedSettings();
            }

            // Preview statistics
            showPreviewStats = EditorGUILayout.Foldout(showPreviewStats, "Placement Preview Statistics", true);
            if (showPreviewStats && profile.propSettings != null)
            {
                DrawPreviewStats();
            }

            // Quick actions
            EditorGUILayout.Space();
            DrawQuickActions();
        }

        private void DrawAdvancedSettings()
        {
            EditorGUI.indentLevel++;

            PropPlacementSettings settings = profile.propSettings;
            
            // Strategy information
            EditorGUILayout.LabelField("Strategy Analysis", EditorStyles.boldLabel);
            string strategyDescription = GetStrategyDescription(settings.strategy);
            EditorGUILayout.HelpBox(strategyDescription, MessageType.Info);
            
            // Density analysis
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Density Analysis", EditorStyles.boldLabel);
            float effectiveDensity = settings.baseDensity * settings.densityMultiplier;
            EditorGUILayout.LabelField($"Effective Density: {effectiveDensity:F3}");
            
            if (effectiveDensity < 0.05f)
            {
                EditorGUILayout.HelpBox("Very sparse placement - good for landmarks or special items", MessageType.Info);
            }
            else if (effectiveDensity < 0.15f)
            {
                EditorGUILayout.HelpBox("Moderate density - balanced placement", MessageType.Info);
            }
            else if (effectiveDensity < 0.3f)
            {
                EditorGUILayout.HelpBox("High density - lush environments", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Very high density - may cause performance issues", MessageType.Warning);
            }

            // Performance warnings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Performance Analysis", EditorStyles.boldLabel);
            
            if (settings.maxPropsPerBiome > 200)
            {
                EditorGUILayout.HelpBox("High prop count may impact performance on lower-end devices", MessageType.Warning);
            }

            if (!settings.useSpatialOptimization && settings.maxPropsPerBiome > 100)
            {
                EditorGUILayout.HelpBox("Consider enabling spatial optimization for better performance", MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawPreviewStats()
        {
            EditorGUI.indentLevel++;

            PropPlacementSettings settings = profile.propSettings;
            
            // Estimated prop counts
            EditorGUILayout.LabelField("Estimated Props per Biome", EditorStyles.boldLabel);
            
            float baseEstimate = settings.baseDensity * settings.densityMultiplier * 100; // Rough estimate
            int minProps = Mathf.RoundToInt(baseEstimate * 0.7f);
            int maxProps = Mathf.Min(Mathf.RoundToInt(baseEstimate * 1.3f), settings.maxPropsPerBiome);
            
            EditorGUILayout.LabelField($"Estimated Range: {minProps} - {maxProps} props");
            EditorGUILayout.LabelField($"Hard Limit: {settings.maxPropsPerBiome} props");
            
            // Clustering information
            if (settings.strategy == PropPlacementStrategy.Clustered)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Clustering Statistics", EditorStyles.boldLabel);
                
                int estimatedClusters = Mathf.RoundToInt(settings.baseDensity * settings.densityMultiplier * 10);
                int propsPerCluster = Mathf.RoundToInt(settings.clustering.clusterSize * settings.clustering.clusterDensity);
                
                EditorGUILayout.LabelField($"Estimated Clusters: {estimatedClusters}");
                EditorGUILayout.LabelField($"Props per Cluster: {propsPerCluster}");
                EditorGUILayout.LabelField($"Cluster Spread: {settings.clustering.clusterRadius:F1} units");
            }
            
            // Quality rating
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quality Rating", EditorStyles.boldLabel);
            string qualityRating = CalculateQualityRating(settings);
            EditorGUILayout.LabelField($"Current Rating: {qualityRating}");

            EditorGUI.indentLevel--;
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Validate Setup"))
            {
                ValidateProfile();
            }
            
            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset Profile", 
                    "This will reset the prop placement settings to default values. Continue?", 
                    "Reset", "Cancel"))
                {
                    ResetToDefaults();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private string GetStrategyDescription(PropPlacementStrategy strategy)
        {
            return strategy switch
            {
                PropPlacementStrategy.Random => "Random scatter distribution. Good for general decoration and ambient props.",
                PropPlacementStrategy.Clustered => "Natural clustering behavior. Excellent for vegetation, rocks, and organic features.",
                PropPlacementStrategy.Sparse => "High-quality selective placement. Perfect for landmarks, special items, and focal points.",
                PropPlacementStrategy.Linear => "Edge-following placement. Ideal for fences, paths, shorelines, and boundaries.",
                PropPlacementStrategy.Radial => "Center-outward distribution. Great for settlements, clearings, and oasis effects.",
                PropPlacementStrategy.Terrain => "Terrain-aware intelligent placement. Best for realistic environmental distribution.",
                _ => "Unknown strategy"
            };
        }

        private string CalculateQualityRating(PropPlacementSettings settings)
        {
            int score = 0;
            
            // Strategy sophistication (0-2 points)
            if (settings.strategy == PropPlacementStrategy.Terrain)
            {
                score += 2;
            }
            else if (settings.strategy == PropPlacementStrategy.Clustered || settings.strategy == PropPlacementStrategy.Radial)
            {
                score += 1;
            }

            // Density curve usage (0-1 points)
            if (settings.densityCurve != null && settings.densityCurve.keys.Length > 2)
            {
                score += 1;
            }

            // Avoidance sophistication (0-2 points)
            if (settings.avoidance.avoidOvercrowding && settings.avoidance.avoidTransitions)
            {
                score += 2;
            }
            else if (settings.avoidance.avoidOvercrowding || settings.avoidance.avoidTransitions)
            {
                score += 1;
            }

            // Variation features (0-2 points)
            if (settings.variation.randomRotation && settings.variation.positionJitter > 0)
            {
                score += 2;
            }
            else if (settings.variation.randomRotation || settings.variation.positionJitter > 0)
            {
                score += 1;
            }

            // Performance considerations (0-1 points)
            if (settings.useSpatialOptimization && settings.maxPropsPerBiome < 200)
            {
                score += 1;
            }

            return score switch
            {
                8 => "A+ (Exceptional)",
                7 => "A (Excellent)", 
                6 => "A- (Very Good)",
                5 => "B+ (Good)",
                4 => "B (Above Average)",
                3 => "B- (Average)",
                2 => "C+ (Below Average)",
                1 => "C (Standard)",
                _ => "C- (Limited)"
            };
        }

        private void ValidateProfile()
        {
            var issues = new System.Collections.Generic.List<string>();
            
            if (string.IsNullOrEmpty(profile.biomeName))
            {
                issues.Add("Biome name is empty");
            }

            if (profile.propSettings == null)
            {
                issues.Add("Prop settings are not configured");
            }
            else
            {
                if (profile.propSettings.propPrefabs == null || profile.propSettings.propPrefabs.Length == 0)
                {
                    issues.Add("No prop prefabs assigned");
                }

                if (profile.propSettings.allowedPropLayers.Count == 0)
                {
                    issues.Add("No allowed prop layers specified");
                }

                if (profile.propSettings.baseDensity <= 0)
                {
                    issues.Add("Base density must be greater than 0");
                }
            }
            
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation Passed", 
                    "BiomeArtProfile validation completed successfully. No issues found.", "OK");
            }
            else
            {
                string issueList = string.Join("\n• ", issues);
                EditorUtility.DisplayDialog("Validation Issues", 
                    $"The following issues were found:\n• {issueList}", "OK");
            }
        }

        private void ResetToDefaults()
        {
            profile.propSettings = new PropPlacementSettings
            {
                strategy = PropPlacementStrategy.Clustered,
                baseDensity = 0.1f,
                densityMultiplier = 1f,
                densityCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 1f),
                clustering = new ClusteringSettings(),
                avoidance = new AvoidanceSettings(),
                variation = new VariationSettings(),
                maxPropsPerBiome = 100,
                useSpatialOptimization = true
            };
            
            EditorUtility.SetDirty(profile);
        }
    }
}