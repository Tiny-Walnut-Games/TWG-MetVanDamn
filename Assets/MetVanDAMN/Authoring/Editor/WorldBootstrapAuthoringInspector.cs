#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Unity.Entities;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	/// <summary>
	/// Custom inspector for WorldBootstrapAuthoring with preview functionality
	/// Provides an editor "Preview" button to regenerate world without entering play mode
	/// </summary>
	[CustomEditor(typeof(WorldBootstrapAuthoring))]
	public class WorldBootstrapAuthoringInspector : UnityEditor.Editor
		{
		private bool _showAdvancedOptions = false;
		private bool _showGenerationRanges = true;
		private bool _showDebugOptions = true;

		// Preview state
		private bool _hasPreviewData = false;
		private int _previewBiomes = 0;
		private int _previewDistricts = 0;
		private int _previewSectors = 0;
		private int _previewRooms = 0;
		private uint _lastPreviewSeed = 0;

		public override void OnInspectorGUI()
			{
			var bootstrap = (WorldBootstrapAuthoring)target;

			EditorGUILayout.Space();
			EditorGUILayout.HelpBox(
				"World Bootstrap generates the complete world hierarchy procedurally from a single configuration. " +
				"This replaces manual DistrictAuthoring placement with configurable ranges for biomes, districts, sectors, and rooms.",
				MessageType.Info);

			EditorGUILayout.Space();

			// Main configuration
			DrawWorldConfiguration(bootstrap);

			EditorGUILayout.Space();

			// Generation ranges
			_showGenerationRanges = EditorGUILayout.Foldout(_showGenerationRanges, "Generation Ranges", true);
			if (_showGenerationRanges)
				{
				EditorGUI.indentLevel++;
				DrawGenerationRanges(bootstrap);
				EditorGUI.indentLevel--;
				}

			EditorGUILayout.Space();

			// Debug options
			_showDebugOptions = EditorGUILayout.Foldout(_showDebugOptions, "Debug Options", true);
			if (_showDebugOptions)
				{
				EditorGUI.indentLevel++;
				DrawDebugOptions(bootstrap);
				EditorGUI.indentLevel--;
				}

			EditorGUILayout.Space();

			// Advanced options
			_showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, "Advanced Options", false);
			if (_showAdvancedOptions)
				{
				EditorGUI.indentLevel++;
				DrawAdvancedOptions(bootstrap);
				EditorGUI.indentLevel--;
				}

			EditorGUILayout.Space();

			// Preview section
			DrawPreviewSection(bootstrap);

			if (GUI.changed)
				{
				EditorUtility.SetDirty(bootstrap);
				}
			}

		private void DrawWorldConfiguration(WorldBootstrapAuthoring bootstrap)
			{
			EditorGUILayout.LabelField("World Configuration", EditorStyles.boldLabel);

			bootstrap.seed = EditorGUILayout.IntField(new GUIContent("Seed", "Seed for deterministic generation (0 = random)"), bootstrap.seed);
			Vector2Int worldSizeVector = EditorGUILayout.Vector2IntField(new GUIContent("World Size", "World bounds size (X,Z)"),
				new Vector2Int(bootstrap.worldSize.x, bootstrap.worldSize.y));
			bootstrap.worldSize = new int2(worldSizeVector.x, worldSizeVector.y);
			bootstrap.randomizationMode = (Shared.RandomizationMode)EditorGUILayout.EnumPopup(
				new GUIContent("Randomization Mode", "Rule randomization mode"), bootstrap.randomizationMode);
			}

		private void DrawGenerationRanges(WorldBootstrapAuthoring bootstrap)
			{
			// Biomes
			EditorGUILayout.LabelField("Biomes", EditorStyles.miniBoldLabel);
			bootstrap.biomeCount = EditorGUILayout.Vector2IntField("Count Range", bootstrap.biomeCount);
			bootstrap.biomeWeight = EditorGUILayout.Slider("Weight", bootstrap.biomeWeight, 0.1f, 2.0f);

			EditorGUILayout.Space(5);

			// Districts
			EditorGUILayout.LabelField("Districts", EditorStyles.miniBoldLabel);
			bootstrap.districtCount = EditorGUILayout.Vector2IntField("Count Range", bootstrap.districtCount);
			bootstrap.districtMinDistance = EditorGUILayout.Slider("Min Distance", bootstrap.districtMinDistance, 5f, 50f);
			bootstrap.districtWeight = EditorGUILayout.Slider("Weight", bootstrap.districtWeight, 0.1f, 2.0f);

			EditorGUILayout.Space(5);

			// Sectors
			EditorGUILayout.LabelField("Sectors", EditorStyles.miniBoldLabel);
			bootstrap.sectorsPerDistrict = EditorGUILayout.Vector2IntField("Per District Range", bootstrap.sectorsPerDistrict);
			var sectorGrid = new Vector2Int(bootstrap.sectorGridSize.x, bootstrap.sectorGridSize.y);
			sectorGrid = EditorGUILayout.Vector2IntField("Grid Size", sectorGrid);
			bootstrap.sectorGridSize = new int2(sectorGrid.x, sectorGrid.y);

			EditorGUILayout.Space(5);

			// Rooms
			EditorGUILayout.LabelField("Rooms", EditorStyles.miniBoldLabel);
			bootstrap.roomsPerSector = EditorGUILayout.Vector2IntField("Per Sector Range", bootstrap.roomsPerSector);
			bootstrap.targetLoopDensity = EditorGUILayout.Slider("Loop Density", bootstrap.targetLoopDensity, 0.1f, 1.0f);
			}

		private void DrawDebugOptions(WorldBootstrapAuthoring bootstrap)
			{
			bootstrap.enableDebugVisualization = EditorGUILayout.Toggle("Enable Debug Visualization", bootstrap.enableDebugVisualization);
			bootstrap.logGenerationSteps = EditorGUILayout.Toggle("Log Generation Steps", bootstrap.logGenerationSteps);
			}

		private void DrawAdvancedOptions(WorldBootstrapAuthoring bootstrap)
			{
			EditorGUILayout.HelpBox("Advanced options for fine-tuning generation behavior", MessageType.None);
			// TODO: Add SLIGHTLY more advanced options here - intentionally limited to allow customization without overwhelming
			bootstrap.districtPlacementAttempts = EditorGUILayout.IntField(new GUIContent("District Placement Attempts", "Max attempts to place each district"), bootstrap.districtPlacementAttempts);
			}

		private void DrawPreviewSection(WorldBootstrapAuthoring bootstrap)
			{
			EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();

			GUI.backgroundColor = new Color(0.7f, 1.0f, 0.7f);
			if (GUILayout.Button("🔄 Preview Generation", GUILayout.Height(30)))
				{
				PreviewGeneration(bootstrap);
				}
			GUI.backgroundColor = Color.white;

			GUI.backgroundColor = new Color(1.0f, 0.9f, 0.7f);
			if (GUILayout.Button("🎲 Random Seed", GUILayout.Width(100), GUILayout.Height(30)))
				{
				bootstrap.seed = 0; // This will generate a random seed
				EditorUtility.SetDirty(bootstrap);
				}
			GUI.backgroundColor = Color.white;

			EditorGUILayout.EndHorizontal();

			// Runtime Regeneration Controls (only show during play mode)
			if (Application.isPlaying)
				{
				EditorGUILayout.Space(10);
				EditorGUILayout.LabelField("🎮 Runtime Regeneration", EditorStyles.boldLabel);

				EditorGUILayout.BeginHorizontal();

				// Full randomization button
				GUI.backgroundColor = new Color(1.0f, 0.8f, 0.8f); // Light red
				if (GUILayout.Button("🎲 FULL Random", GUILayout.Height(35)))
					{
					PerformFullRandomRegeneration(bootstrap);
					}

				// Partial randomization button
				GUI.backgroundColor = new Color(0.8f, 1.0f, 0.8f); // Light green
				if (GUILayout.Button("🔄 Partial Random", GUILayout.Height(35)))
					{
					PerformPartialRandomRegeneration(bootstrap);
					}

				GUI.backgroundColor = Color.white;
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space(5);

				EditorGUILayout.BeginHorizontal();
				GUI.backgroundColor = new Color(0.9f, 0.9f, 1.0f); // Light blue
				if (GUILayout.Button("🔨 Regenerate Current", GUILayout.Height(30)))
					{
					RegenerateWithCurrentSettings(bootstrap);
					}

				GUI.backgroundColor = new Color(1.0f, 1.0f, 0.8f); // Light yellow
				if (GUILayout.Button("🎯 New Random Seed", GUILayout.Height(30)))
					{
					GenerateNewRandomSeed(bootstrap);
					}

				GUI.backgroundColor = Color.white;
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space(5);
				EditorGUILayout.HelpBox(
					"🎮 RUNTIME CONTROLS:\n" +
					"FULL Random: Completely different world (all parameters randomized)\n" +
					"Partial Random: Same structure, randomized details\n" +
					"Regenerate Current: Same exact settings for testing\n" +
					"New Random Seed: Keep settings, change seed only",
					MessageType.Info);
				}

			if (_hasPreviewData)
				{
				EditorGUILayout.Space(5);
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.LabelField($"Preview Results (Seed: {_lastPreviewSeed})", EditorStyles.miniBoldLabel);
				EditorGUILayout.LabelField($"🌿 Biomes: {_previewBiomes}");
				EditorGUILayout.LabelField($"🏰 Districts: {_previewDistricts}");
				EditorGUILayout.LabelField($"🏘️ Sectors: {_previewSectors}");
				EditorGUILayout.LabelField($"🏠 Rooms: {_previewRooms}");
				EditorGUILayout.EndVertical();
				}

			EditorGUILayout.Space(5);
			EditorGUILayout.HelpBox(
				"Preview calculates generation results without entering Play mode. " +
				"The actual generation happens at runtime when the scene starts.",
				MessageType.Info);
			}

		private void PreviewGeneration(WorldBootstrapAuthoring bootstrap)
			{
			// Calculate preview results based on configuration
			uint seed = bootstrap.seed == 0 ? (uint)UnityEngine.Random.Range(1, int.MaxValue) : (uint)bootstrap.seed;
			var random = new Unity.Mathematics.Random(seed);

			_previewBiomes = random.NextInt(bootstrap.biomeCount.x, bootstrap.biomeCount.y + 1);
			_previewDistricts = random.NextInt(bootstrap.districtCount.x, bootstrap.districtCount.y + 1);

			int totalSectors = 0;
			int totalRooms = 0;

			for (int i = 0; i < _previewDistricts; i++)
				{
				int sectors = random.NextInt(bootstrap.sectorsPerDistrict.x, bootstrap.sectorsPerDistrict.y + 1);
				totalSectors += sectors;

				for (int j = 0; j < sectors; j++)
					{
					int rooms = random.NextInt(bootstrap.roomsPerSector.x, bootstrap.roomsPerSector.y + 1);
					totalRooms += rooms;
					}
				}

			_previewSectors = totalSectors;
			_previewRooms = totalRooms;
			_lastPreviewSeed = seed;
			_hasPreviewData = true;

			if (bootstrap.logGenerationSteps)
				{
				Debug.Log($"🔮 WorldBootstrap Preview: {_previewDistricts} districts, {_previewSectors} sectors, {_previewRooms} rooms (Seed: {seed})");
				}

			// Update the seed if it was 0 (random)
			if (bootstrap.seed == 0)
				{
				bootstrap.seed = (int)seed;
				EditorUtility.SetDirty(bootstrap);
				}
			}

		#region Runtime Regeneration Methods

		private void PerformFullRandomRegeneration(WorldBootstrapAuthoring bootstrap)
			{
			if (!Application.isPlaying) return;

			Debug.Log("🎲 FULL Random Regeneration: Randomizing ALL parameters");

			// Randomize all range-based parameters
			var random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Millisecond);

			// Randomize seed
			bootstrap.seed = random.NextInt(1, 999999);

			// Randomize world size (using int2 not Vector2Int)
			bootstrap.worldSize = new int2(random.NextInt(30, 100), random.NextInt(30, 100));

			// Randomize biome count
			bootstrap.biomeCount = new Vector2Int(random.NextInt(2, 6), random.NextInt(4, 8));

			// Randomize district count
			bootstrap.districtCount = new Vector2Int(random.NextInt(1, 4), random.NextInt(3, 8));

			// Randomize sectors per district
			bootstrap.sectorsPerDistrict = new Vector2Int(random.NextInt(1, 3), random.NextInt(2, 6));

			// Randomize rooms per sector
			bootstrap.roomsPerSector = new Vector2Int(random.NextInt(1, 4), random.NextInt(3, 8));

			// Randomize float parameters
			bootstrap.biomeWeight = random.NextFloat(0.1f, 2.0f);
			bootstrap.districtMinDistance = random.NextFloat(5f, 50f);
			bootstrap.districtWeight = random.NextFloat(0.1f, 2.0f);

			EditorUtility.SetDirty(bootstrap);
			TriggerWorldRegeneration(bootstrap, "FULL_RANDOM");
			}

		private void PerformPartialRandomRegeneration(WorldBootstrapAuthoring bootstrap)
			{
			if (!Application.isPlaying) return;

			Debug.Log("🔄 Partial Random Regeneration: Keeping structure, randomizing details");

			// Only randomize seed and minor parameters while preserving structure
			var random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Millisecond);

			// Change seed for different generation while keeping settings
			bootstrap.seed = random.NextInt(1, 999999);

			// Slightly adjust range parameters (±1 within reasonable bounds)
			bootstrap.roomsPerSector = new Vector2Int(
				Mathf.Max(1, bootstrap.roomsPerSector.x + random.NextInt(-1, 2)),
				Mathf.Max(bootstrap.roomsPerSector.x + 1, bootstrap.roomsPerSector.y + random.NextInt(-1, 2))
			);

			bootstrap.sectorsPerDistrict = new Vector2Int(
				Mathf.Max(1, bootstrap.sectorsPerDistrict.x + random.NextInt(-1, 2)),
				Mathf.Max(bootstrap.sectorsPerDistrict.x + 1, bootstrap.sectorsPerDistrict.y + random.NextInt(-1, 2))
			);

			EditorUtility.SetDirty(bootstrap);
			TriggerWorldRegeneration(bootstrap, "PARTIAL_RANDOM");
			}

		private void RegenerateWithCurrentSettings(WorldBootstrapAuthoring bootstrap)
			{
			if (!Application.isPlaying) return;

			Debug.Log("🔨 Regenerating with EXACT current settings for testing");
			TriggerWorldRegeneration(bootstrap, "CURRENT_SETTINGS");
			}

		private void GenerateNewRandomSeed(WorldBootstrapAuthoring bootstrap)
			{
			if (!Application.isPlaying) return;

			int newSeed = UnityEngine.Random.Range(1, 999999);
			bootstrap.seed = newSeed;
			EditorUtility.SetDirty(bootstrap);

			Debug.Log($"🎯 New Random Seed Generated: {newSeed}");
			TriggerWorldRegeneration(bootstrap, "NEW_SEED");
			}

		private void TriggerWorldRegeneration(WorldBootstrapAuthoring bootstrap, string regenerationType)
			{
			try
				{
				Debug.Log($"🔄 Triggering world regeneration: {regenerationType} with seed {bootstrap.seed}");

				// Simple approach: disable and re-enable the GameObject
				// This triggers the authoring conversion process again
				bootstrap.gameObject.SetActive(false);
				EditorApplication.delayCall += () =>
					{
						if (bootstrap != null && bootstrap.gameObject != null)
							{
							bootstrap.gameObject.SetActive(true);
							Debug.Log($"✅ World regeneration completed: {regenerationType}");
							}
					};
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"World regeneration failed: {ex.Message}");
				Debug.LogError($"Stack trace: {ex.StackTrace}");
				}
			}

		#endregion

		private void OnSceneGUI()
			{
			var bootstrap = (WorldBootstrapAuthoring)target;

			if (bootstrap.enableDebugVisualization)
				{
				// Draw world bounds in scene view
				Handles.color = new Color(0.2f, 0.9f, 0.4f, 0.5f);
				Vector3 center = bootstrap.transform.position;
				var size = new Vector3(bootstrap.worldSize.x, 0, bootstrap.worldSize.y);

				Handles.DrawWireCube(center, size);

				// Draw corner labels
				Handles.color = Color.white;
				Vector3 halfSize = size * 0.5f;
				Handles.Label(center + new Vector3(halfSize.x, 1, halfSize.z), "NE");
				Handles.Label(center + new Vector3(-halfSize.x, 1, halfSize.z), "NW");
				Handles.Label(center + new Vector3(halfSize.x, 1, -halfSize.z), "SE");
				Handles.Label(center + new Vector3(-halfSize.x, 1, -halfSize.z), "SW");
				}
			}
		}
	}
#endif
