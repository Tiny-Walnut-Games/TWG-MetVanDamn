using System.IO; // For Path utilities
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	/// <summary>
	/// Custom property drawer for BiomeArtProfile to show additional info in inspector
	/// (Updated to align with PropPlacementSettings nesting: props + layers now under profile.propSettings)
	/// </summary>
	[CustomEditor(typeof(BiomeArtProfile))]
	public class BiomeArtProfileEditor : UnityEditor.Editor
		{
		public override void OnInspectorGUI ()
			{
			var profile = (BiomeArtProfile)target;

			// Ensure propSettings exists (non-destructive safeguard)
			if (profile.propSettings == null)
				{
				profile.propSettings = new PropPlacementSettings();
				EditorUtility.SetDirty(profile);
				}

			// Draw default inspector
			DrawDefaultInspector();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

			if (string.IsNullOrEmpty(profile.biomeName))
				{
				EditorGUILayout.HelpBox("Biome name is required for proper identification.", MessageType.Warning);
				}

			if (profile.floorTile == null && profile.wallTile == null && profile.backgroundTile == null)
				{
				EditorGUILayout.HelpBox("At least one tile type should be assigned for visual generation.", MessageType.Warning);
				}

			PropPlacementSettings propSettings = profile.propSettings;
			if (propSettings != null)
				{
				bool hasPrefabs = propSettings.propPrefabs != null && propSettings.propPrefabs.Length > 0;
				bool hasLayers = propSettings.allowedPropLayers != null && propSettings.allowedPropLayers.Count > 0;
				if (hasPrefabs && !hasLayers)
					{
					EditorGUILayout.HelpBox("Prop prefabs are assigned but no allowed layers specified. Props may not be placed.", MessageType.Info);
					}
				}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Auto-Configure Default Layers"))
				{
				AutoConfigureDefaultLayers(profile);
				}
			if (GUILayout.Button("Create Test Tilemap"))
				{
				CreateTestTilemap(profile);
				}
			EditorGUILayout.EndHorizontal();
			}

		private void AutoConfigureDefaultLayers (BiomeArtProfile profile)
			{
			Undo.RecordObject(profile, "Auto-Configure Layers");
			profile.propSettings ??= new PropPlacementSettings();
			profile.propSettings.allowedPropLayers ??= new System.Collections.Generic.List<string>();

			profile.propSettings.allowedPropLayers.Clear();
			profile.propSettings.allowedPropLayers.Add("FloorProps");
			profile.propSettings.allowedPropLayers.Add("WalkableProps");
			profile.propSettings.allowedPropLayers.Add("OverheadProps");
			EditorUtility.SetDirty(profile);
			}

		private void CreateTestTilemap (BiomeArtProfile profile)
			{
			GameObject testGrid = new($"Test {profile.biomeName} Grid");
			testGrid.AddComponent<Grid>();

			GameObject floorLayer = new("Floor", typeof(UnityEngine.Tilemaps.Tilemap), typeof(UnityEngine.Tilemaps.TilemapRenderer));
			floorLayer.transform.SetParent(testGrid.transform);

			UnityEngine.Tilemaps.Tilemap tilemap = floorLayer.GetComponent<UnityEngine.Tilemaps.Tilemap>();
			UnityEngine.Tilemaps.TilemapRenderer renderer = floorLayer.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();

			if (profile.floorTile != null)
				{
				tilemap.SetTile(Vector3Int.zero, profile.floorTile);
				}

			if (profile.materialOverride != null)
				{
				renderer.material = profile.materialOverride;
				}

			if (!string.IsNullOrEmpty(profile.sortingLayerOverride))
				{
				renderer.sortingLayerName = profile.sortingLayerOverride;
				}

			Selection.activeGameObject = testGrid;
			EditorGUIUtility.PingObject(testGrid);
			}
		}

	/// <summary>
	/// Menu utilities for creating sample biome art profiles (updated for nested propSettings structure)
	/// </summary>
	public static class BiomeArtProfileMenus
		{
		[MenuItem("Assets/Create/MetVanDAMN/Sample Biome Profiles/Solar Plains Profile")]
		public static void CreateSolarPlainsProfile ()
			{
			CreateSampleProfile("SolarPlainsProfile", "Solar Plains", new Color(1f, 0.9f, 0.3f));
			}

		[MenuItem("Assets/Create/MetVanDAMN/Sample Biome Profiles/Crystal Caverns Profile")]
		public static void CreateCrystalCavernsProfile ()
			{
			CreateSampleProfile("CrystalCavernsProfile", "Crystal Caverns", new Color(0.7f, 0.9f, 1f));
			}

		[MenuItem("Assets/Create/MetVanDAMN/Sample Biome Profiles/Shadow Realms Profile")]
		public static void CreateShadowRealmsProfile ()
			{
			CreateSampleProfile("ShadowRealmsProfile", "Shadow Realms", new Color(0.3f, 0.2f, 0.4f));
			}

		private static void CreateSampleProfile (string fileName, string biomeName, Color debugColor)
			{
			BiomeArtProfile profile = ScriptableObject.CreateInstance<BiomeArtProfile>();
			profile.biomeName = biomeName;
			profile.debugColor = debugColor;
			profile.propSettings = new PropPlacementSettings
				{
				baseDensity = 0.15f, // replaced legacy propSpawnChance approximation
				densityMultiplier = 1f,
				allowedPropLayers = new System.Collections.Generic.List<string> { "FloorProps", "WalkableProps", "OverheadProps" }
				};

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
