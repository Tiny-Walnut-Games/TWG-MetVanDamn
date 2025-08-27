using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Tilemaps;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Biome;
using System.Collections.Generic;
using System.Linq;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    /// <summary>
    /// Enhanced biome color legend panel for the World Debugger
    /// Addresses TODO: "Quick biome color legend panel in World Debugger"
    /// </summary>
    public class BiomeColorLegendWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool autoRefresh = true;
        private float refreshInterval = 1f;
        private double lastRefreshTime;
        
        private List<BiomeInfo> biomeInfos = new();
        private Dictionary<BiomeType, Color> biomeColors = new();

        [System.Serializable]
        private class BiomeInfo
        {
            public BiomeType type;
            public Color color;
            public Color currentColor; // Runtime color (may differ from design-time)
            public string name;
            public int instanceCount;
            public Vector3 averagePosition;
            public BiomeArtProfile artProfile;
            public bool isVisible = true;
            public bool isRuntimeActive = false; // True if found in ECS systems during play mode
            public bool hasColorOverride = false; // True if runtime color differs from design color
            public string colorSource = ""; // Source of current color (e.g., "TilemapRenderer", "ECS", "Design")
            public uint nodeId = 0; // NodeId for better tracking
        }

        [MenuItem("Tools/MetVanDAMN/World Debugger/Biome Color Legend")]
        public static void ShowWindow()
        {
            var window = GetWindow<BiomeColorLegendWindow>("Biome Legend");
            window.minSize = new Vector2(300, 200);
            window.RefreshBiomeData();
        }

        private void OnEnable()
        {
            lastRefreshTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
            RefreshBiomeData();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > refreshInterval)
            {
                RefreshBiomeData();
                CheckForRuntimeColorChanges();
                lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void CheckForRuntimeColorChanges()
        {
            // Enhanced runtime synchronization with ECS biome systems and tilemap renderers
            if (Application.isPlaying)
            {
                SynchronizeWithECSBiomeSystems();
            }
            
            SynchronizeWithTilemapRenderers();
        }

        private void SynchronizeWithECSBiomeSystems()
        {
            // Runtime ECS biome system synchronization for play mode
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            // Query ECS entities with biome components for runtime color data
            var entityManager = world.EntityManager;
            var biomeQuery = entityManager.CreateEntityQuery(typeof(TinyWalnutGames.MetVD.Core.Biome), typeof(NodeId));
            
            if (biomeQuery.CalculateEntityCount() > 0)
            {
                var entities = biomeQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                var biomes = biomeQuery.ToComponentDataArray<TinyWalnutGames.MetVD.Core.Biome>(Unity.Collections.Allocator.Temp);
                var nodeIds = biomeQuery.ToComponentDataArray<NodeId>(Unity.Collections.Allocator.Temp);
                
                for (int i = 0; i < entities.Length; i++)
                {
                    var nodeId = nodeIds[i].Value;
                    var biomeType = biomes[i].Type.ToString();
                    
                    // Update biome data if runtime color information is available
                    var existingEntry = biomeInfos.Find(entry => 
                        entry.nodeId == nodeId || entry.type.ToString() == biomeType);
                    
                    if (existingEntry != null)
                    {
                        existingEntry.isRuntimeActive = true;
                        // Additional ECS data could be pulled here for enhanced runtime info
                    }
                }
                
                entities.Dispose();
                biomes.Dispose();
                nodeIds.Dispose();
            }
            
            biomeQuery.Dispose();
        }

        private void SynchronizeWithTilemapRenderers()
        {
            // TilemapRenderer color detection and updates
            var tilemapRenderers = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
            
            foreach (var renderer in tilemapRenderers)
            {
                if (renderer.material != null && renderer.material.HasProperty("_Color"))
                {
                    Color rendererColor = renderer.material.color;
                    
                    // Try to match tilemap renderer to biome by checking parent hierarchy
                    var biomeAuthoring = renderer.GetComponentInParent<BiomeFieldAuthoring>();
                    if (biomeAuthoring != null)
                    {
                        var existingEntry = biomeInfos.Find(entry => entry.nodeId == biomeAuthoring.nodeId);
                        if (existingEntry != null && existingEntry.currentColor != rendererColor)
                        {
                            existingEntry.currentColor = rendererColor;
                            existingEntry.hasColorOverride = true;
                            existingEntry.colorSource = "TilemapRenderer";
                        }
                    }
                    else
                    {
                        // Try to match by name patterns
                        string rendererName = renderer.gameObject.name.ToLowerInvariant();
                        var matchingEntry = biomeInfos.Find(entry => 
                            rendererName.Contains(entry.type.ToString().ToLowerInvariant()) ||
                            rendererName.Contains(entry.nodeId.ToString()));
                            
                        if (matchingEntry != null && matchingEntry.currentColor != rendererColor)
                        {
                            matchingEntry.currentColor = rendererColor;
                            matchingEntry.hasColorOverride = true;
                            matchingEntry.colorSource = $"TilemapRenderer ({renderer.name})";
                        }
                    }
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            
            // Header controls
            DrawHeaderControls();
            
            EditorGUILayout.Space(10);
            
            // Biome legend content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            if (biomeInfos.Count == 0)
            {
                DrawEmptyState();
            }
            else
            {
                DrawBiomeLegend();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(5);
            
            // Footer stats
            DrawFooterStats();
        }

        private void DrawHeaderControls()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                RefreshBiomeData();
            }
            
            GUILayout.Space(10);
            
            autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh, GUILayout.Width(100));
            
            if (autoRefresh)
            {
                GUILayout.Label("Interval:", GUILayout.Width(50));
                refreshInterval = EditorGUILayout.Slider(refreshInterval, 0.5f, 5f, GUILayout.Width(100));
                GUILayout.Label("s", GUILayout.Width(15));
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Focus All", GUILayout.Width(70)))
            {
                FocusAllBiomes();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.FlexibleSpace();
            
            GUIStyle centeredStyle = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Italic
            };
            
            GUILayout.Label("No biomes found in scene", centeredStyle);
            GUILayout.Space(5);
            
            GUIStyle smallCenteredStyle = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
            
            GUILayout.Label("Add BiomeFieldAuthoring components to see biome information", smallCenteredStyle);
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawBiomeLegend()
        {
            EditorGUILayout.LabelField("Biome Color Legend", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            foreach (var biomeInfo in biomeInfos.OrderBy(b => b.type.ToString()))
            {
                DrawBiomeEntry(biomeInfo);
            }
        }

        private void DrawBiomeEntry(BiomeInfo biomeInfo)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            
            // Color indicator
            Rect colorRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUI.DrawRect(colorRect, biomeInfo.color);
            EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.y, colorRect.width, 1), Color.black);
            EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.yMax - 1, colorRect.width, 1), Color.black);
            EditorGUI.DrawRect(new Rect(colorRect.x, colorRect.y, 1, colorRect.height), Color.black);
            EditorGUI.DrawRect(new Rect(colorRect.xMax - 1, colorRect.y, 1, colorRect.height), Color.black);
            
            GUILayout.Space(5);
            
            // Biome info
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(biomeInfo.name, EditorStyles.boldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField($"({biomeInfo.instanceCount} instances)", GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            
            // Visibility toggle
            bool newVisibility = EditorGUILayout.Toggle(biomeInfo.isVisible, GUILayout.Width(20));
            if (newVisibility != biomeInfo.isVisible)
            {
                biomeInfo.isVisible = newVisibility;
                UpdateBiomeVisibility(biomeInfo);
            }
            
            if (GUILayout.Button("Focus", GUILayout.Width(50)))
            {
                FocusBiome(biomeInfo);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Additional details
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Type: {biomeInfo.type}", EditorStyles.miniLabel, GUILayout.Width(150));
            if (biomeInfo.artProfile != null)
            {
                EditorGUILayout.LabelField($"Profile: {biomeInfo.artProfile.name}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("Profile: None", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            if (biomeInfo.instanceCount > 1)
            {
                EditorGUILayout.LabelField($"Avg Position: {biomeInfo.averagePosition:F1}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(2);
        }

        private void DrawFooterStats()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            
            EditorGUILayout.LabelField($"Total Biomes: {biomeInfos.Count}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Total Instances: {biomeInfos.Sum(b => b.instanceCount)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Unique Types: {biomeInfos.Select(b => b.type).Distinct().Count()}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshBiomeData()
        {
            biomeInfos.Clear();
            biomeColors.Clear();
            
            var biomeAuthorings = FindObjectsOfType<BiomeFieldAuthoring>();
            var biomesByType = new Dictionary<BiomeType, List<BiomeFieldAuthoring>>();
            
            // Group biomes by type
            foreach (var biomeAuthoring in biomeAuthorings)
            {
                if (biomeAuthoring == null) continue;
                
                BiomeType biomeType = biomeAuthoring.biomeType;
                
                if (!biomesByType.ContainsKey(biomeType))
                {
                    biomesByType[biomeType] = new List<BiomeFieldAuthoring>();
                }
                
                biomesByType[biomeType].Add(biomeAuthoring);
            }
            
            // Sync with runtime ECS data if available
            SyncWithRuntimeBiomeData(biomesByType);
            
            // Create biome info entries
            foreach (var kvp in biomesByType)
            {
                BiomeType type = kvp.Key;
                List<BiomeFieldAuthoring> instances = kvp.Value;
                
                Color biomeColor = GetBiomeColor(type, instances);
                Vector3 averagePosition = CalculateAveragePosition(instances);
                BiomeArtProfile artProfile = GetMostCommonArtProfile(instances);
                
                var biomeInfo = new BiomeInfo
                {
                    type = type,
                    color = biomeColor,
                    name = GetBiomeDisplayName(type, artProfile),
                    instanceCount = instances.Count,
                    averagePosition = averagePosition,
                    artProfile = artProfile,
                    isVisible = true
                };
                
                biomeInfos.Add(biomeInfo);
                biomeColors[type] = biomeColor;
            }
        }

        /// <summary>
        /// Syncs biome color data with runtime ECS systems when available
        /// </summary>
        private void SyncWithRuntimeBiomeData(Dictionary<BiomeType, List<BiomeFieldAuthoring>> biomesByType)
        {
            // Sync with runtime biome field system if available during play mode
            if (Application.isPlaying && World.DefaultGameObjectInjectionWorld != null)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                // Removed: var biomeSystem = world.GetExistingSystemManaged<BiomeFieldSystem>();
                // The line above caused CS0315 because BiomeFieldSystem is an ISystem, not a ComponentSystemBase.
                // If you need to interact with the system, use the unmanaged API or just access the EntityManager as below.

                // Access runtime biome data through ECS query
                SyncWithECSBiomeData(world, biomesByType);
            }

            // Also sync with any runtime tile renderers that may have modified colors
            SyncWithTileMapRenderers(biomesByType);
        }

        private void SyncWithECSBiomeData(World world, Dictionary<BiomeType, List<BiomeFieldAuthoring>> biomesByType)
        {
            try
            {
                var entityManager = world.EntityManager;
                
                // Query all biome entities with art profile references
                using var query = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<TinyWalnutGames.MetVD.Core.Biome>(),
                    ComponentType.ReadOnly<BiomeArtProfileReference>()
                );
                
                using var entities = query.ToEntityArray(Allocator.Temp);
                using var biomeComponents = query.ToComponentDataArray<TinyWalnutGames.MetVD.Core.Biome>(Allocator.Temp);
                using var artProfileRefs = query.ToComponentDataArray<BiomeArtProfileReference>(Allocator.Temp);
                
                for (int i = 0; i < entities.Length; i++)
                {
                    var biomeComponent = biomeComponents[i];
                    var artProfileRef = artProfileRefs[i];                    

                    // Example fix in SyncWithECSBiomeData:
                    if (artProfileRef.ProfileRef.IsValid())
                    {
                        var profile = artProfileRef.ProfileRef.Value;

                        // Update biome color information based on runtime state
                        if (profile.debugColor.a > 0f)
                        {
                            // Runtime debug color takes precedence
                            UpdateBiomeColorFromRuntime(biomeComponent.Type, profile.debugColor);
                        }
                    }
                    if (artProfileRef.ProfileRef.Value != null)
                    {
                        var profile = artProfileRef.ProfileRef.Value;

                        // Update biome info entry if it exists
                        var existingEntry = biomeInfos.Find(entry => entry.type == biomeComponent.Type);
                        if (existingEntry != null)
                        {
                            existingEntry.isRuntimeActive = true;
                            existingEntry.artProfile = profile;
                            existingEntry.name = GetBiomeDisplayName(biomeComponent.Type, profile);

                            // If runtime color differs from design color, mark as override
                            if (existingEntry.color != profile.debugColor && profile.debugColor.a > 0f)
                            {
                                existingEntry.currentColor = profile.debugColor;
                                existingEntry.hasColorOverride = true;
                                existingEntry.colorSource = "ECS BiomeArtProfile";
                            }
                        }
                    }
                    if (artProfileRef.ProfileRef)
                        {
                            var profile = artProfileRef.ProfileRef.Value;
                        
                            // Update biome color information based on runtime state
                            if (profile.debugColor.a > 0f)
                            {
                                // Runtime debug color takes precedence
                                UpdateBiomeColorFromRuntime(biomeComponent.Type, profile.debugColor);
                            }                            
                        }
                    if (artProfileRef.ProfileRef.Value != null)
                        {
                        var profile = artProfileRef.ProfileRef.Value;
                        
                        // Update biome info entry if it exists
                        var existingEntry = biomeInfos.Find(entry => entry.type == biomeComponent.Type);
                        if (existingEntry != null)
                        {
                            existingEntry.isRuntimeActive = true;
                            existingEntry.artProfile = profile;
                            existingEntry.name = GetBiomeDisplayName(biomeComponent.Type, profile);
                            
                            // If runtime color differs from design color, mark as override
                            if (existingEntry.color != profile.debugColor && profile.debugColor.a > 0f)
                            {
                                existingEntry.currentColor = profile.debugColor;
                                existingEntry.hasColorOverride = true;
                                existingEntry.colorSource = "ECS BiomeArtProfile";
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Handle potential ECS access issues gracefully
                Debug.LogWarning($"Could not sync with ECS biome data: {ex.Message}");
            }
        }

        private void SyncWithTileMapRenderers(Dictionary<BiomeType, List<BiomeFieldAuthoring>> biomesByType)
        {
            var tilemapRenderers = FindObjectsOfType<UnityEngine.Tilemaps.TilemapRenderer>();
            
            foreach (var renderer in tilemapRenderers)
            {
                if (renderer.material != null && renderer.material.HasProperty("_Color"))
                {
                    // Check if this tilemap is associated with any biome
                    var associatedBiome = FindBiomeForTilemap(renderer, biomesByType);
                    if (associatedBiome != null)
                    {
                        // Update color from runtime material
                        Color runtimeColor = renderer.material.color;
                        if (runtimeColor.a > 0f) // Valid color
                        {
                            UpdateBiomeColorFromRuntime(associatedBiome.biomeType, runtimeColor);
                        }
                    }
                }
            }
        }

        private BiomeFieldAuthoring FindBiomeForTilemap(UnityEngine.Tilemaps.TilemapRenderer renderer, Dictionary<BiomeType, List<BiomeFieldAuthoring>> biomesByType)
        {
            // Find biome by proximity to tilemap or by name matching
            foreach (var kvp in biomesByType)
            {
                foreach (var biome in kvp.Value)
                {
                    if (biome.artProfile?.biomeName != null && 
                        renderer.name.Contains(biome.artProfile.biomeName))
                    {
                        return biome;
                    }
                    
                    // Or by proximity
                    float distance = Vector3.Distance(biome.transform.position, renderer.transform.position);
                    if (distance < biome.fieldRadius + 5f) // Within biome influence + buffer
                    {
                        return biome;
                    }
                }
            }
            
            return null;
        }

        private void UpdateBiomeColorFromRuntime(BiomeType biomeType, Color runtimeColor)
        {
            biomeColors[biomeType] = runtimeColor;
        }

        private Color GetBiomeColor(BiomeType type, List<BiomeFieldAuthoring> instances)
        {
            // Try to get color from art profile first
            var profileWithColor = instances.FirstOrDefault(i => i.artProfile != null);
            if (profileWithColor?.artProfile != null)
            {
                return profileWithColor.artProfile.debugColor;
            }
            
            // Fallback to default colors based on biome type
            return GetDefaultBiomeColor(type);
        }

        private Color GetDefaultBiomeColor(BiomeType type)
        {
            // Generate consistent colors based on biome type
            switch (type.ToString().ToLower())
            {
                case "forest": return new Color(0.2f, 0.6f, 0.2f, 1f);
                case "desert": return new Color(0.9f, 0.8f, 0.3f, 1f);
                case "mountain": return new Color(0.5f, 0.5f, 0.5f, 1f);
                case "water": return new Color(0.2f, 0.4f, 0.8f, 1f);
                case "swamp": return new Color(0.3f, 0.4f, 0.2f, 1f);
                case "tundra": return new Color(0.8f, 0.9f, 1f, 1f);
                case "volcanic": return new Color(0.8f, 0.2f, 0.1f, 1f);
                default:
                    // Generate color based on hash of type name
                    int hash = type.GetHashCode();
                    float r = ((hash & 0xFF0000) >> 16) / 255f;
                    float g = ((hash & 0x00FF00) >> 8) / 255f;
                    float b = (hash & 0x0000FF) / 255f;
                    return new Color(r * 0.7f + 0.3f, g * 0.7f + 0.3f, b * 0.7f + 0.3f, 1f);
            }
        }

        private Vector3 CalculateAveragePosition(List<BiomeFieldAuthoring> instances)
        {
            if (instances.Count == 0) return Vector3.zero;
            
            Vector3 sum = Vector3.zero;
            foreach (var instance in instances)
            {
                sum += instance.transform.position;
            }
            
            return sum / instances.Count;
        }

        private BiomeArtProfile GetMostCommonArtProfile(List<BiomeFieldAuthoring> instances)
        {
            return instances
                .Where(i => i.artProfile != null)
                .GroupBy(i => i.artProfile)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;
        }

        private string GetBiomeDisplayName(BiomeType type, BiomeArtProfile artProfile)
        {
            if (artProfile != null && !string.IsNullOrEmpty(artProfile.biomeName))
            {
                return artProfile.biomeName;
            }
            
            return type.ToString();
        }

        private void FocusBiome(BiomeInfo biomeInfo)
        {
            var instances = FindObjectsOfType<BiomeFieldAuthoring>()
                .Where(b => b.biomeType.Equals(biomeInfo.type))
                .ToArray();
            
            if (instances.Length > 0)
            {
                Selection.objects = instances.Cast<Object>().ToArray();
                
                if (instances.Length == 1)
                {
                    SceneView.FrameLastActiveSceneView();
                }
                else
                {
                    // Frame all instances
                    var bounds = new Bounds(instances[0].transform.position, Vector3.zero);
                    foreach (var instance in instances)
                    {
                        bounds.Encapsulate(instance.transform.position);
                    }
                    
                    SceneView.lastActiveSceneView.Frame(bounds, false);
                }
            }
        }

        private void FocusAllBiomes()
        {
            var allBiomes = FindObjectsOfType<BiomeFieldAuthoring>();
            
            if (allBiomes.Length > 0)
            {
                Selection.objects = allBiomes.Cast<Object>().ToArray();
                
                var bounds = new Bounds(allBiomes[0].transform.position, Vector3.zero);
                foreach (var biome in allBiomes)
                {
                    bounds.Encapsulate(biome.transform.position);
                }
                
                SceneView.lastActiveSceneView.Frame(bounds, false);
            }
        }

        private void UpdateBiomeVisibility(BiomeInfo biomeInfo)
        {
            var instances = FindObjectsOfType<BiomeFieldAuthoring>()
                .Where(b => b.biomeType.Equals(biomeInfo.type))
                .ToArray();
            
            foreach (var instance in instances)
            {
                instance.gameObject.SetActive(biomeInfo.isVisible);
            }
            
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Get the current biome color mapping for use by other systems
        /// </summary>
        public static Dictionary<BiomeType, Color> GetBiomeColorMapping()
        {
            var window = Resources.FindObjectsOfTypeAll<BiomeColorLegendWindow>().FirstOrDefault();
            return window?.biomeColors ?? new Dictionary<BiomeType, Color>();
        }
    }
}
