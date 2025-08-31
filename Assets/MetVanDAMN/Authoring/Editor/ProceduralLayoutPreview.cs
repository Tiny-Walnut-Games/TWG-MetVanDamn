#if UNITY_EDITOR
using System.Collections.Generic; // Added for List<>
using System.Linq; // Added for LINQ extension methods
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Shared;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;


namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	/// <summary>
	/// Editor utility for previewing procedural layout in edit mode
	/// </summary>
	public class ProceduralLayoutPreview : EditorWindow
		{
		[MenuItem("Tiny Walnut Games/MetVanDAMN/World Layout Preview")]
		public static void ShowWindow ()
			{
			GetWindow<ProceduralLayoutPreview>("Layout Preview");
			}

		private WorldConfigurationAuthoring _worldConfig;
		private DistrictAuthoring [ ] _districts;
		private int _previewDistrictCount = 5;
		private int2 _previewWorldSize = new(32, 32);
		private RandomizationMode _previewMode = RandomizationMode.Partial;
		private uint _previewSeed = 12345;

		private void OnGUI ()
			{
			GUILayout.Label("Procedural Layout Preview", EditorStyles.boldLabel);

			EditorGUILayout.Space();

			// Configuration section
			GUILayout.Label("Preview Configuration", EditorStyles.boldLabel);
			this._previewDistrictCount = EditorGUILayout.IntField("District Count", this._previewDistrictCount);
			this._previewWorldSize.x = EditorGUILayout.IntField("World Width", this._previewWorldSize.x);
			this._previewWorldSize.y = EditorGUILayout.IntField("World Height", this._previewWorldSize.y);
			this._previewMode = (RandomizationMode)EditorGUILayout.EnumPopup("Randomization Mode", this._previewMode);
			this._previewSeed = (uint)EditorGUILayout.IntField("Seed", (int)this._previewSeed);

			EditorGUILayout.Space();

			// Scene analysis section
			GUILayout.Label("Current Scene", EditorStyles.boldLabel);
			this._worldConfig = FindFirstObjectByType<WorldConfigurationAuthoring>();
			this._districts = FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);

			if (this._worldConfig != null)
				{
				EditorGUILayout.LabelField("World Config Found", this._worldConfig.name);
				EditorGUILayout.LabelField("Current Seed", this._worldConfig.seed.ToString());
				EditorGUILayout.LabelField("Current World Size", this._worldConfig.worldSize.ToString());
				EditorGUILayout.LabelField("Randomization Mode", this._worldConfig.randomizationMode.ToString());
				}
			else
				{
				EditorGUILayout.HelpBox("No WorldConfigurationAuthoring found in scene", MessageType.Warning);
				}

			EditorGUILayout.LabelField("Districts in Scene", this._districts.Length.ToString());

			EditorGUILayout.Space();

			// Actions section
			GUILayout.Label("Actions", EditorStyles.boldLabel);

			if (GUILayout.Button("Preview Layout Algorithm"))
				{
				this.PreviewLayoutAlgorithm();
				}

			if (GUILayout.Button("Generate Preview Districts"))
				{
				this.GeneratePreviewDistricts();
				}

			if (this._worldConfig != null && GUILayout.Button("Apply Preview Settings to Scene"))
				{
				this.ApplyPreviewSettingsToScene();
				}

			if (this._districts.Length > 0 && GUILayout.Button("Show District Info"))
				{
				this.ShowDistrictInfo();
				}

			EditorGUILayout.Space();

			// Information section
			EditorGUILayout.HelpBox(
				"This tool allows you to preview the procedural layout algorithms without entering Play mode. " +
				"Use 'Preview Layout Algorithm' to see how districts would be placed with current settings.",
				MessageType.Info);
			}

		private void PreviewLayoutAlgorithm ()
			{
			Debug.Log($"🔮 Previewing Layout Algorithm");
			Debug.Log($"   Districts: {this._previewDistrictCount}");
			Debug.Log($"   World Size: {this._previewWorldSize}");
			Debug.Log($"   Randomization: {this._previewMode}");
			Debug.Log($"   Seed: {this._previewSeed}");

			// Simulate the district placement algorithm
			var random = new Unity.Mathematics.Random(this._previewSeed);
			DistrictPlacementStrategy strategy = this._previewDistrictCount > 16 ?
				DistrictPlacementStrategy.JitteredGrid :
				DistrictPlacementStrategy.PoissonDisc;

			Debug.Log($"   Strategy: {strategy}");

			// Generate positions using the same algorithm as DistrictLayoutSystem
			var positions = new int2 [ this._previewDistrictCount ];

			if (strategy == DistrictPlacementStrategy.PoissonDisc)
				{
				this.GeneratePreviewPoissonDisc(positions, this._previewWorldSize, ref random);
				}
			else
				{
				this.GeneratePreviewJitteredGrid(positions, this._previewWorldSize, ref random);
				}

			Debug.Log("   Calculated Positions:");
			for (int i = 0; i < positions.Length; i++)
				{
				Debug.Log($"     District {i + 1}: ({positions [ i ].x}, {positions [ i ].y})");
				}

			// Simulate rule randomization
			this.PreviewRuleRandomization(ref random);
			}

		private void GeneratePreviewPoissonDisc (int2 [ ] positions, int2 worldSize, ref Unity.Mathematics.Random random)
			{
			float minDistance = math.min(worldSize.x, worldSize.y) * 0.2f;
			int maxAttempts = 30;

			for (int i = 0; i < positions.Length; i++)
				{
				bool validPosition = false;
				int attempts = 0;

				while (!validPosition && attempts < maxAttempts)
					{
					var candidate = new int2(
						random.NextInt(0, worldSize.x),
						random.NextInt(0, worldSize.y)
					);

					validPosition = true;
					for (int j = 0; j < i; j++)
						{
						float distance = math.length(new float2(candidate - positions [ j ]));
						if (distance < minDistance)
							{
							validPosition = false;
							break;
							}
						}

					if (validPosition)
						{
						positions [ i ] = candidate;
						}

					attempts++;
					}

				if (!validPosition)
					{
					positions [ i ] = new int2(
						random.NextInt(0, worldSize.x),
						random.NextInt(0, worldSize.y)
					);
					}
				}
			}

		private void GeneratePreviewJitteredGrid (int2 [ ] positions, int2 worldSize, ref Unity.Mathematics.Random random)
			{
			int gridDim = (int)math.ceil(math.sqrt(positions.Length));
			float2 cellSize = new float2(worldSize) / gridDim;
			float jitterAmount = math.min(cellSize.x, cellSize.y) * 0.3f;

			for (int i = 0; i < positions.Length; i++)
				{
				int gridX = i % gridDim;
				int gridY = i / gridDim;

				float2 cellCenter = new float2(gridX + 0.5f, gridY + 0.5f) * cellSize;
				var jitter = new float2(
					random.NextFloat(-jitterAmount, jitterAmount),
					random.NextFloat(-jitterAmount, jitterAmount)
				);

				float2 finalPosition = cellCenter + jitter;
				positions [ i ] = new int2(
					math.clamp((int)finalPosition.x, 0, worldSize.x - 1),
					math.clamp((int)finalPosition.y, 0, worldSize.y - 1)
				);
				}
			}

		private void PreviewRuleRandomization (ref Unity.Mathematics.Random random)
			{
			Debug.Log($"🎲 Rule Randomization Preview (Mode: {this._previewMode})");

			switch (this._previewMode)
				{
				case RandomizationMode.None:
					Debug.Log("   Biome Polarities: Sun|Moon|Heat|Cold (curated)");
					Debug.Log("   Upgrades: Jump|DoubleJump|Dash|WallJump (curated)");
					break;

				case RandomizationMode.Partial:
					Polarity polarities = this.GenerateRandomPolarities(ref random, false);
					Debug.Log($"   Biome Polarities: {polarities} (randomized)");
					Debug.Log("   Upgrades: Jump|DoubleJump|Dash|WallJump (curated)");
					break;

				case RandomizationMode.Full:
					Polarity fullPolarities = this.GenerateRandomPolarities(ref random, true);
					uint upgrades = this.GenerateRandomUpgrades(ref random);
					Debug.Log($"   Biome Polarities: {fullPolarities} (randomized)");
					Debug.Log($"   Upgrades: 0x{upgrades:X} (randomized, always includes Jump)");
					break;
				default:
					break;
				}
			}

		private Polarity GenerateRandomPolarities (ref Unity.Mathematics.Random random, bool allowMore)
			{
			Polarity [ ] availablePolarities = new [ ]
			{
				Polarity.Sun, Polarity.Moon, Polarity.Heat, Polarity.Cold,
				Polarity.Earth, Polarity.Wind, Polarity.Life, Polarity.Tech
			};

			Polarity result = Polarity.None;
			int count = allowMore ? random.NextInt(2, 6) : random.NextInt(2, 4);

			for (int i = 0; i < count; i++)
				{
				int index = random.NextInt(0, availablePolarities.Length);
				result |= availablePolarities [ index ];
				}

			return result;
			}

		private uint GenerateRandomUpgrades (ref Unity.Mathematics.Random random)
			{
			uint upgrades = 1u; // Always include Jump (bit 0)

			for (int i = 1; i < 8; i++)
				{
				if (random.NextFloat() > 0.4f)
					{
					upgrades |= 1u << i;
					}
				}

			return upgrades;
			}

		private void GeneratePreviewDistricts ()
			{
			// Create actual district authoring objects in the scene for preview
			Debug.Log("🏗️ Generate Preview Districts - Creating scene objects");

			if (this._worldConfig == null)
				{
				Debug.LogWarning("Cannot generate preview districts: No WorldGenerationConfig assigned");
				return;
				}

			// Clear existing preview districts
			this.ClearExistingPreviewDistricts();

			// Calculate district positions based on preview settings
			List<Vector3> districtPositions = this.CalculateDistrictPositions();

			// Create parent object for organization
			var previewParent = new GameObject("[PREVIEW] Generated Districts");
			previewParent.transform.position = Vector3.zero;
			Undo.RegisterCreatedObjectUndo(previewParent, "Generate Preview Districts");

			// Create district authoring objects at calculated positions
			for (int i = 0; i < districtPositions.Count; i++)
				{
				Vector3 position = districtPositions [ i ];
				var districtGO = new GameObject($"District_Preview_{i:D3}");
				districtGO.transform.SetParent(previewParent.transform);
				districtGO.transform.position = position;

				// Add DistrictAuthoring component
				DistrictAuthoring districtAuthoring = districtGO.AddComponent<DistrictAuthoring>();
				districtAuthoring.nodeId = (uint)(1000 + i); // Preview node IDs start from 1000
				districtAuthoring.districtType = this.GetRandomDistrictType();
				districtAuthoring.biomeType = this.GetRandomBiomeType();
				districtAuthoring.size = new float2(UnityEngine.Random.Range(50f, 150f), UnityEngine.Random.Range(50f, 150f));

				// Add visual indicator for preview
				var visualIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
				visualIndicator.name = "Preview_Indicator";
				visualIndicator.transform.SetParent(districtGO.transform);
				visualIndicator.transform.localPosition = Vector3.zero;
				visualIndicator.transform.localScale = new Vector3(
					districtAuthoring.size.x * 0.01f,
					5f,
					districtAuthoring.size.y * 0.01f
				);

				// Apply preview material
				if (visualIndicator.TryGetComponent(out Renderer renderer))
					{
					Material previewMaterial = this.CreatePreviewMaterial(districtAuthoring.biomeType);
					renderer.material = previewMaterial;
					}

				// Register for undo
				Undo.RegisterCreatedObjectUndo(districtGO, "Generate Preview District");

				Debug.Log($"Created preview district at {position} with biome {districtAuthoring.biomeType}");
				}

			// Select the parent for easy management
			Selection.activeGameObject = previewParent;

			Debug.Log($"🎯 Generated {districtPositions.Count} preview districts");
			}

		private void ClearExistingPreviewDistricts ()
			{
			// Find and remove existing preview district objects
			GameObject [ ] existingPreviews = GameObject.FindGameObjectsWithTag("Untagged")
				.Where(go => go.name.Contains("[PREVIEW]") || go.name.Contains("District_Preview_"))
				.ToArray();

			foreach (GameObject preview in existingPreviews)
				{
				Undo.DestroyObjectImmediate(preview);
				}
			}

		private List<Vector3> CalculateDistrictPositions ()
			{
			var positions = new List<Vector3>();
			var random = new Unity.Mathematics.Random(this._previewSeed);

			// Calculate grid-based positions with some randomization
			int gridSize = Mathf.CeilToInt(Mathf.Sqrt(this._previewWorldSize.x * this._previewWorldSize.y / 10000f)); // Rough district count
			float spacingX = this._previewWorldSize.x / gridSize;
			float spacingY = this._previewWorldSize.y / gridSize;

			for (int x = 0; x < gridSize; x++)
				{
				for (int y = 0; y < gridSize; y++)
					{
					var basePosition = new Vector3(
						x * spacingX - this._previewWorldSize.x * 0.5f,
						0f,
						y * spacingY - this._previewWorldSize.y * 0.5f
					);

					// Add randomization based on mode
					Vector3 randomOffset = Vector3.zero;
					switch (this._previewMode)
						{
						case RandomizationMode.Partial:
							randomOffset = new Vector3(
								random.NextFloat(-spacingX * 0.25f, spacingX * 0.25f),
								0f,
								random.NextFloat(-spacingY * 0.25f, spacingY * 0.25f)
							);
							break;
						case RandomizationMode.Full:
							randomOffset = new Vector3(
								random.NextFloat(-spacingX * 0.4f, spacingX * 0.4f),
								0f,
								random.NextFloat(-spacingY * 0.4f, spacingY * 0.4f)
							);
							break;
						case RandomizationMode.None:
						default:
							randomOffset = new Vector3(
								random.NextFloat(-spacingX * 0.1f, spacingX * 0.1f),
								0f,
								random.NextFloat(-spacingY * 0.1f, spacingY * 0.1f)
							);
							break;
						}

					positions.Add(basePosition + randomOffset);
					}
				}

			return positions;
			}

		private DistrictType GetRandomDistrictType ()
			{
			System.Array values = System.Enum.GetValues(typeof(DistrictType));
			return (DistrictType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
			}

		private BiomeType GetRandomBiomeType ()
			{
			System.Array values = System.Enum.GetValues(typeof(BiomeType));
			return (BiomeType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
			}

		private Material CreatePreviewMaterial (BiomeType biomeType)
			{
			var material = new Material(Shader.Find("Standard"));

			// Assign colors based on biome type
			Color biomeColor = biomeType switch
				{
					BiomeType.Forest => Color.green,
					BiomeType.Desert => Color.yellow,
					BiomeType.Mountains => Color.gray,
					BiomeType.Ocean => Color.blue,
					BiomeType.Tundra => Color.cyan,
					BiomeType.Volcanic => Color.red,
					BiomeType.Crystal => Color.magenta,
					BiomeType.Ruins => new Color(0.5f, 0.3f, 0.1f), // Brown
					BiomeType.Cosmic => new Color(0.1f, 0.1f, 0.3f), // Dark blue
					_ => Color.white
					};

			material.color = biomeColor;
			material.SetFloat("_Metallic", 0.0f);
			material.SetFloat("_Glossiness", 0.5f);

			return material;
			}

		private void ApplyPreviewSettingsToScene ()
			{
			if (this._worldConfig != null)
				{
				Undo.RecordObject(this._worldConfig, "Apply Preview Settings");
				this._worldConfig.seed = (int)this._previewSeed;
				this._worldConfig.worldSize = this._previewWorldSize;
				this._worldConfig.randomizationMode = this._previewMode;
				EditorUtility.SetDirty(this._worldConfig);
				Debug.Log("✅ Applied preview settings to WorldConfigurationAuthoring");
				}
			}

		private void ShowDistrictInfo ()
			{
			Debug.Log($"📋 District Information ({this._districts.Length} districts):");

			int unplacedCount = 0;
			int placedCount = 0;

			foreach (DistrictAuthoring district in this._districts)
				{
				bool isUnplaced = district.gridCoordinates.x == 0 && district.gridCoordinates.y == 0;
				if (isUnplaced)
					{
					unplacedCount++;
					}
				else
					{
					placedCount++;
					}

				Debug.Log($"   District {district.nodeId}: " +
						 $"Pos({district.gridCoordinates.x}, {district.gridCoordinates.y}), " +
						 $"Level {district.level}, " +
						 $"Status: {(isUnplaced ? "UNPLACED" : "PLACED")}");
				}

			Debug.Log($"   Summary: {placedCount} placed, {unplacedCount} unplaced");
			}
		}
	}
#endif
