#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

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