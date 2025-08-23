using UnityEngine;
using UnityEditor;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    /// <summary>
    /// Custom property drawer for BiomeArtProfile to show additional info in inspector
    /// </summary>
    [CustomEditor(typeof(BiomeArtProfile))]
    public class BiomeArtProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            BiomeArtProfile profile = (BiomeArtProfile)target;
            
            // Draw default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            
            // Validation feedback
            if (string.IsNullOrEmpty(profile.biomeName))
            {
                EditorGUILayout.HelpBox("Biome name is required for proper identification.", MessageType.Warning);
            }
            
            if (profile.floorTile == null && profile.wallTile == null && profile.backgroundTile == null)
            {
                EditorGUILayout.HelpBox("At least one tile type should be assigned for visual generation.", MessageType.Warning);
            }
            
            if (profile.propPrefabs != null && profile.propPrefabs.Length > 0 && profile.allowedPropLayers.Count == 0)
            {
                EditorGUILayout.HelpBox("Prop prefabs are assigned but no allowed layers specified. Props may not be placed.", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            // Quick setup buttons
            if (GUILayout.Button("Auto-Configure Default Layers"))
            {
                AutoConfigureDefaultLayers(profile);
            }
            
            if (GUILayout.Button("Create Test Tilemap"))
            {
                CreateTestTilemap(profile);
            }
        }
        
        private void AutoConfigureDefaultLayers(BiomeArtProfile profile)
        {
            Undo.RecordObject(profile, "Auto-Configure Layers");
            
            profile.allowedPropLayers.Clear();
            profile.allowedPropLayers.Add("FloorProps");
            profile.allowedPropLayers.Add("WalkableProps");
            profile.allowedPropLayers.Add("OverheadProps");
            
            EditorUtility.SetDirty(profile);
        }
        
        private void CreateTestTilemap(BiomeArtProfile profile)
        {
            // Create a test tilemap to preview the biome art
            GameObject testGrid = new GameObject($"Test {profile.biomeName} Grid");
            testGrid.AddComponent<Grid>();
            
            // Create a simple floor layer for testing
            GameObject floorLayer = new GameObject("Floor", typeof(UnityEngine.Tilemaps.Tilemap), typeof(UnityEngine.Tilemaps.TilemapRenderer));
            floorLayer.transform.SetParent(testGrid.transform);
            
            var tilemap = floorLayer.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            var renderer = floorLayer.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            
            // Apply biome tile if available
            if (profile.floorTile != null)
            {
                tilemap.SetTile(Vector3Int.zero, profile.floorTile);
            }
            
            // Apply material override if specified
            if (profile.materialOverride != null)
            {
                renderer.material = profile.materialOverride;
            }
            
            // Apply sorting layer override if specified
            if (!string.IsNullOrEmpty(profile.sortingLayerOverride))
            {
                renderer.sortingLayerName = profile.sortingLayerOverride;
            }
            
            Selection.activeGameObject = testGrid;
            EditorGUIUtility.PingObject(testGrid);
        }
    }
    
    /// <summary>
    /// Menu utilities for creating sample biome art profiles
    /// </summary>
    public static class BiomeArtProfileMenus
    {
        [MenuItem("Assets/Create/MetVanDAMN/Sample Biome Profiles/Solar Plains Profile")]
        public static void CreateSolarPlainsProfile()
        {
            CreateSampleProfile("SolarPlainsProfile", "Solar Plains", new Color(1f, 0.9f, 0.3f));
        }
        
        [MenuItem("Assets/Create/MetVanDAMN/Sample Biome Profiles/Crystal Caverns Profile")]
        public static void CreateCrystalCavernsProfile()
        {
            CreateSampleProfile("CrystalCavernsProfile", "Crystal Caverns", new Color(0.7f, 0.9f, 1f));
        }
        
        [MenuItem("Assets/Create/MetVanDAMN/Sample Biome Profiles/Shadow Realms Profile")]
        public static void CreateShadowRealmsProfile()
        {
            CreateSampleProfile("ShadowRealmsProfile", "Shadow Realms", new Color(0.3f, 0.2f, 0.4f));
        }
        
        private static void CreateSampleProfile(string fileName, string biomeName, Color debugColor)
        {
            BiomeArtProfile profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
            profile.biomeName = biomeName;
            profile.debugColor = debugColor;
            profile.propSpawnChance = 0.15f;
            
            // Set up default prop layers
            profile.allowedPropLayers.Add("FloorProps");
            profile.allowedPropLayers.Add("WalkableProps");
            profile.allowedPropLayers.Add("OverheadProps");
            
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }
            
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + fileName + ".asset");
            
            AssetDatabase.CreateAsset(profile, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = profile;
        }
    }
}