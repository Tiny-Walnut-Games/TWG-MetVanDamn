#if UNITY_EDITOR
using TinyWalnutGames.MetVD.Core;
using Unity.Entities; // for ECS world queries
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
#if !METVD_GRAPH_REF
	internal enum DistrictPlacementStrategy : byte { PoissonDisc = 0, JitteredGrid = 1 }
#endif
	[InitializeOnLoad]
	public static class ProceduralLayoutGizmoDrawer
		{
		private static MetVDGizmoSettings _settings;
		private static bool _showUnplacedDistricts = true;
		private static bool _showPlacedDistricts = true;
		private static bool _showConnections = true;
		private static bool _showBiomeRadius = true;
		private static bool _showRandomizationMode = true;
		private static bool _showSectors = true;
		private static bool _showRooms = true;

		// Cached ECS data for sector/room drawing (play mode only)
		private static readonly System.Collections.Generic.List<int2> _sectorCoords = new();
		private static readonly System.Collections.Generic.List<int2> _roomCoords = new();
		private static double _lastEcsSampleTime;
		private const double EcsSampleInterval = 0.25; // seconds

		static ProceduralLayoutGizmoDrawer()
			{
			LoadSettings();
			SceneView.duringSceneGui += OnSceneGUI;
			EditorApplication.update += SampleHierarchyEcsData;
			}

		private static void LoadSettings()
			{
			string [ ] guids = AssetDatabase.FindAssets("t:MetVDGizmoSettings");
			if (guids.Length > 0)
				{
				_settings = AssetDatabase.LoadAssetAtPath<MetVDGizmoSettings>(AssetDatabase.GUIDToAssetPath(guids [ 0 ]));
				}
			}

		private static void OnSceneGUI(SceneView _) // underscore to indicate unused parameter
			{
			// Only show debug panel if there are MetVanDAMN components in the scene
			DistrictAuthoring [ ] districts = Object.FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);
			WorldConfigurationAuthoring worldConfig = Object.FindFirstObjectByType<WorldConfigurationAuthoring>();

			if (districts.Length == 0 && worldConfig == null)
				{
				return; // No MetVanDAMN components, don't show debug panel
				}

			if (_settings == null)
				{
				LoadSettings();
				if (_settings == null)
					{
					return;
					}
				}
			DrawDebugControls();
			DrawProceduralLayoutGizmos();
			}

		private static void DrawDebugControls()
			{
			Handles.BeginGUI();
			GUILayout.BeginArea(new Rect(10, 10, 270, 260));
			GUILayout.BeginVertical("box");
			GUILayout.Label("Procedural Layout Debug", EditorStyles.boldLabel);
			_showUnplacedDistricts = GUILayout.Toggle(_showUnplacedDistricts, "Show Unplaced Districts (0,0)");
			_showPlacedDistricts = GUILayout.Toggle(_showPlacedDistricts, "Show Placed Districts");
			_showConnections = GUILayout.Toggle(_showConnections, "Show Connections");
			_showBiomeRadius = GUILayout.Toggle(_showBiomeRadius, "Show Biome Radius");
			_showRandomizationMode = GUILayout.Toggle(_showRandomizationMode, "Show Randomization Mode");
			_showSectors = GUILayout.Toggle(_showSectors, "Show Sectors (Level 1)");
			_showRooms = GUILayout.Toggle(_showRooms, "Show Rooms (Level 2)");
			GUILayout.Space(8);
			if (GUILayout.Button("Preview Layout"))
				{
				PreviewProceduralLayout();
				}
			if (GUILayout.Button("Frame All Districts"))
				{
				FrameAllDistricts();
				}
			if (GUILayout.Button("MetVD Snap Districts To Grid"))
				{
				MetVDGizmoDrawer.SnapAllDistrictsToGrid();
				}
			GUILayout.EndVertical();
			GUILayout.EndArea();
			Handles.EndGUI();
			}

		private static void DrawProceduralLayoutGizmos()
			{
			DistrictAuthoring [ ] districts = Object.FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);
			WorldConfigurationAuthoring worldConfig = Object.FindFirstObjectByType<WorldConfigurationAuthoring>();
			if (_showRandomizationMode && worldConfig != null)
				{
				DrawRandomizationModeInfo(worldConfig);
				}
			foreach (DistrictAuthoring district in districts)
				{
				DrawDistrictGizmos(district);
				}
			if (_showConnections)
				{
				DrawConnectionGizmos();
				}

			if (Application.isPlaying)
				{
				DrawSectorsAndRooms();
				}
			}

		private static void DrawRandomizationModeInfo(WorldConfigurationAuthoring worldConfig)
			{
			SceneView sceneView = SceneView.currentDrawingSceneView;
			if (sceneView == null)
				{
				return;
				}

			Camera cam = sceneView.camera;
			if (cam == null)
				{
				return;
				}

			Vector3 anchor = cam.transform.position + cam.transform.forward * 10f;
			Handles.color = Color.yellow;
			Handles.Label(anchor, $"Randomization Mode: {worldConfig.randomizationMode}\nSeed: {worldConfig.seed}\nWorld Size: {worldConfig.worldSize}");
			}

		private static void DrawDistrictGizmos(DistrictAuthoring district)
			{
			Vector3 position = GetGizmoPosition(district);
			bool isUnplaced = district.gridCoordinates.x == 0 && district.gridCoordinates.y == 0;
			if (isUnplaced && _showUnplacedDistricts)
				{
				DrawUnplacedDistrictGizmo(position, district);
				}
			else if (!isUnplaced && _showPlacedDistricts)
				{
				DrawPlacedDistrictGizmo(position, district);
				}

			if (_showBiomeRadius)
				{
				DrawBiomeRadiusGizmo(position, district);
				}
			}

		private static void DrawUnplacedDistrictGizmo(Vector3 position, DistrictAuthoring district)
			{
			Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
			Handles.DrawWireCube(position, Vector3.one * 2f);
			Handles.color = Color.gray;
			Handles.Label(position + Vector3.up * 1.5f, $"UNPLACED\nNode: {district.nodeId}\nLevel: {district.level}");
			}

		private static void DrawPlacedDistrictGizmo(Vector3 position, DistrictAuthoring district)
			{
			Color biomeColor = _settings != null ? _settings.placedDistrictColor : Color.cyan;
			Handles.color = new Color(biomeColor.r, biomeColor.g, biomeColor.b, 0.3f);
			Vector3 size = new(_settings.districtSize.x, 0.5f, _settings.districtSize.y);
			DrawDistrictBounds(position, size, biomeColor);
			Handles.color = _settings.labelColor;
			string label = $"District {district.nodeId}\nCoords: ({district.gridCoordinates.x}, {district.gridCoordinates.y})\nLevel: {district.level}";
			Handles.Label(position + Vector3.up * 2f, label);
			}

		private static void DrawDistrictBounds(Vector3 position, Vector3 size, Color biomeColor)
			{
			var corners = new Vector3 [ 4 ];
			corners [ 0 ] = position + new Vector3(-size.x / 2, 0, -size.z / 2);
			corners [ 1 ] = position + new Vector3(size.x / 2, 0, -size.z / 2);
			corners [ 2 ] = position + new Vector3(size.x / 2, 0, size.z / 2);
			corners [ 3 ] = position + new Vector3(-size.x / 2, 0, size.z / 2);
			Handles.color = new Color(biomeColor.r, biomeColor.g, biomeColor.b, 0.2f);
			Handles.DrawAAConvexPolygon(corners);
			Handles.color = biomeColor;
			Handles.DrawLine(corners [ 0 ], corners [ 1 ]);
			Handles.DrawLine(corners [ 1 ], corners [ 2 ]);
			Handles.DrawLine(corners [ 2 ], corners [ 3 ]);
			Handles.DrawLine(corners [ 3 ], corners [ 0 ]);
			}

		private static void DrawBiomeRadiusGizmo(Vector3 position, DistrictAuthoring _)
			{
			Color biomeColor = GetBiomeColor(BiomeType.HubArea);
			Handles.color = new Color(biomeColor.r, biomeColor.g, biomeColor.b, 0.1f);
			Handles.DrawWireDisc(position, Vector3.up, _settings != null ? _settings.biomeRadius : 5f);
			}

		private static void DrawConnectionGizmos()
			{
			ConnectionAuthoring [ ] connections = Object.FindObjectsByType<ConnectionAuthoring>(FindObjectsSortMode.None);
			foreach (ConnectionAuthoring connection in connections)
				{
				if (connection.from != null && connection.to != null)
					{
					Vector3 fromPos = GetGizmoPosition(connection.from);
					Vector3 toPos = GetGizmoPosition(connection.to);
					DrawConnectionArrow(fromPos, toPos, connection);
					}
				}
			}

		private static void DrawConnectionArrow(Vector3 from, Vector3 to, ConnectionAuthoring connection)
			{
			Color connectionColor = connection.type == ConnectionType.OneWay ? _settings.oneWayColor : _settings.connectionColor;
			Handles.color = connectionColor;
			Handles.DrawLine(from + Vector3.up * 0.5f, to + Vector3.up * 0.5f);
			Vector3 direction = (to - from).normalized;
			Vector3 right = Vector3.Cross(direction, Vector3.up) * _settings.connectionArrowSize;
			// Outbound arrow
			Vector3 arrowPos = Vector3.Lerp(from, to, 0.7f) + Vector3.up * 0.5f;
			Vector3 back = -direction * _settings.connectionArrowSize;
			Handles.DrawLine(arrowPos, arrowPos + back + right);
			Handles.DrawLine(arrowPos, arrowPos + back - right);
			if (connection.type == ConnectionType.Bidirectional)
				{
				arrowPos = Vector3.Lerp(from, to, 0.3f) + Vector3.up * 0.5f;
				Vector3 reverseBack = direction * _settings.connectionArrowSize; // invert direction for second arrow
				Handles.DrawLine(arrowPos, arrowPos + reverseBack + right);
				Handles.DrawLine(arrowPos, arrowPos + reverseBack - right);
				}
			}

		private static void DrawSectorsAndRooms()
			{
			if (!_showSectors && !_showRooms)
				{
				return;
				}
			// Convert ECS coordinates directly (sectors small quads, rooms small discs)
			Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
			if (_showSectors)
				{
				Handles.color = new Color(0.95f, 0.75f, 0.2f, 0.35f); // amber
				foreach (int2 c in _sectorCoords)
					{
					Vector3 p = new(c.x, 0f, c.y);
					float size = (_settings != null ? _settings.gridCellSize : 1f) * 0.6f;
					Vector3 half = new(size * 0.5f, 0, size * 0.5f);
					Vector3 [ ] quad = { p - half, p + new Vector3(half.x, 0, -half.z), p + half, p + new Vector3(-half.x, 0, half.z) };
					Handles.DrawAAConvexPolygon(quad);
					}
				}
			if (_showRooms)
				{
				Handles.color = new Color(0.4f, 0.8f, 1f, 0.5f); // cyan tint
				foreach (int2 c in _roomCoords)
					{
					Vector3 p = new(c.x, 0f, c.y);
					float radius = (_settings != null ? _settings.gridCellSize : 1f) * 0.25f;
					Handles.DrawSolidDisc(p + Vector3.up * 0.01f, Vector3.up, radius);
					}
				}
			}

		private static void SampleHierarchyEcsData()
			{
			if (!Application.isPlaying)
				{
				return;
				}

			if (EditorApplication.timeSinceStartup - _lastEcsSampleTime < EcsSampleInterval)
				{
				return;
				}

			_lastEcsSampleTime = EditorApplication.timeSinceStartup;
			World world = World.DefaultGameObjectInjectionWorld;
			if (world == null || !world.IsCreated)
				{
				return;
				}

			EntityManager em = world.EntityManager;
			try
				{
				EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<NodeId>());
				using Unity.Collections.NativeArray<NodeId> nodeIds = query.ToComponentDataArray<NodeId>(Unity.Collections.Allocator.Temp);
				_sectorCoords.Clear();
				_roomCoords.Clear();
				for (int i = 0; i < nodeIds.Length; i++)
					{
					NodeId n = nodeIds [ i ];
					if (n.Level == 1)
						{
						_sectorCoords.Add(n.Coordinates);
						}
					else if (n.Level == 2)
						{
						_roomCoords.Add(n.Coordinates);
						}
					}
				}
			catch { /* swallow sampling errors safely */ }
			}

		private static Vector3 GetGizmoPosition(DistrictAuthoring district)
			{
			return _settings != null && _settings.useGridCoordinatesForGizmos
				? new Vector3(
					district.gridCoordinates.x * _settings.gridCellSize,
					0,
					district.gridCoordinates.y * _settings.gridCellSize
				) + _settings.gridOriginOffset
				: district.transform.position;
			}

		private static Color GetBiomeColor(BiomeType biomeType)
			{
			// 🔥 FIXED: Complete color mapping for all 27 biome types in Gizmo system
			return biomeType switch
				{
					// Light-aligned biomes
					BiomeType.SolarPlains => new Color(1f, 0.9f, 0.4f, 1f),       // Bright golden
					BiomeType.CrystalCaverns => new Color(0.6f, 0.8f, 1f, 1f),    // Crystal blue
					BiomeType.SkyGardens => new Color(0.4f, 0.7f, 0.9f, 1f),      // Sky blue

					// Dark-aligned biomes
					BiomeType.ShadowRealms => new Color(0.2f, 0.1f, 0.3f, 1f),    // Dark purple
					BiomeType.DeepUnderwater => new Color(0.1f, 0.3f, 0.6f, 1f),  // Deep blue
					BiomeType.VoidChambers => new Color(0.1f, 0.1f, 0.1f, 1f),    // Near black

					// Hazard/Energy biomes
					BiomeType.VolcanicCore => new Color(1f, 0.3f, 0.1f, 1f),       // Lava red
					BiomeType.PowerPlant => new Color(0.2f, 0.8f, 0.3f, 1f),      // Electric green
					BiomeType.PlasmaFields => new Color(0.9f, 0.2f, 0.9f, 1f),    // Plasma purple

					// Ice/Crystal biomes
					BiomeType.FrozenWastes => new Color(0.9f, 0.95f, 1f, 1f),     // Ice white
					BiomeType.IceCatacombs => new Color(0.7f, 0.85f, 0.95f, 1f),  // Ice blue
					BiomeType.CryogenicLabs => new Color(0.6f, 0.9f, 1f, 1f),     // Cryo blue
					BiomeType.IcyCanyon => new Color(0.8f, 0.9f, 0.95f, 1f),      // Canyon ice
					BiomeType.Tundra => new Color(0.8f, 0.9f, 1f, 1f),            // Tundra blue

					// Earth/Nature biomes
					BiomeType.Forest => new Color(0.2f, 0.6f, 0.2f, 1f),          // Forest green
					BiomeType.Mountains => new Color(0.5f, 0.5f, 0.5f, 1f),       // Mountain gray
					BiomeType.Desert => new Color(0.9f, 0.8f, 0.3f, 1f),          // Desert tan

					// Water biomes
					BiomeType.Ocean => new Color(0.2f, 0.4f, 0.8f, 1f),           // Ocean blue

					// Space biomes
					BiomeType.Cosmic => new Color(0.3f, 0.1f, 0.6f, 1f),          // Cosmic purple

					// Crystal biomes
					BiomeType.Crystal => new Color(0.8f, 0.5f, 1f, 1f),           // Crystal violet

					// Ruins/Ancient biomes
					BiomeType.Ruins => new Color(0.6f, 0.5f, 0.4f, 1f),           // Ancient stone
					BiomeType.AncientRuins => new Color(0.7f, 0.6f, 0.3f, 1f),    // Weathered gold

					// Volcanic/Fire biomes
					BiomeType.Volcanic => new Color(0.8f, 0.2f, 0.1f, 1f),        // Volcanic red
					BiomeType.Hell => new Color(1f, 0.1f, 0f, 1f),                // Hell fire red

					// Neutral/Mixed biomes
					BiomeType.HubArea => new Color(0.8f, 0.8f, 0.8f, 1f),         // Neutral gray
					BiomeType.TransitionZone => new Color(0.6f, 0.6f, 0.8f, 1f),  // Transition purple

					// Unknown/fallback
					BiomeType.Unknown => new Color(0.5f, 0.5f, 0.5f, 1f),         // Unknown gray

					// Default fallback
					_ => Color.gray,
					};
			}

		private static void PreviewProceduralLayout()
			{
			WorldConfigurationAuthoring worldConfig = Object.FindFirstObjectByType<WorldConfigurationAuthoring>();
			if (worldConfig == null)
				{
				Debug.LogWarning("Procedural Layout Preview: WorldConfigurationAuthoring not found in scene.");
				return;
				}
			DistrictAuthoring [ ] districts = Object.FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);
			if (districts.Length == 0)
				{
				Debug.LogWarning("Procedural Layout Preview: No DistrictAuthoring components found.");
				return;
				}
			var unplaced = new System.Collections.Generic.List<DistrictAuthoring>();
			foreach (DistrictAuthoring d in districts)
				{
				if (d.level == 0 && d.gridCoordinates.x == 0 && d.gridCoordinates.y == 0)
					{
					unplaced.Add(d);
					}
				}

			if (unplaced.Count == 0)
				{
				Debug.Log("Procedural Layout Preview: All districts already placed.");
				return;
				}
			int targetCount = worldConfig.targetSectors > 0 ? math.min(worldConfig.targetSectors, unplaced.Count) : unplaced.Count;
			var random = new Unity.Mathematics.Random((uint)(worldConfig.seed == 0 ? 1 : worldConfig.seed));
			DistrictPlacementStrategy strategy = targetCount > 16 ? DistrictPlacementStrategy.JitteredGrid : DistrictPlacementStrategy.PoissonDisc;
			var positions = new int2 [ targetCount ];
			GeneratePositionsPreview(positions, worldConfig.worldSize, strategy, ref random);
			for (int i = 0; i < targetCount; i++)
				{
				unplaced [ i ].gridCoordinates = positions [ i ];
				EditorUtility.SetDirty(unplaced [ i ]);
				}
			Debug.Log($"Procedural Layout Preview: Placed {targetCount} districts using {strategy} strategy.");
			SceneView.RepaintAll();
			}

		private static void GeneratePositionsPreview(int2 [ ] positions, int2 worldSize, DistrictPlacementStrategy strategy, ref Unity.Mathematics.Random random)
			{
			if (strategy == DistrictPlacementStrategy.PoissonDisc)
				{
				float minDistance = math.min(worldSize.x, worldSize.y) * 0.2f;
				int maxAttempts = 30;
				for (int i = 0; i < positions.Length; i++)
					{
					bool valid = false; int attempts = 0;
					while (!valid && attempts < maxAttempts)
						{
						int2 candidate = new(random.NextInt(0, worldSize.x), random.NextInt(0, worldSize.y));
						valid = true;
						for (int j = 0; j < i; j++)
							{
							float dist = math.length(new float2(candidate - positions [ j ]));
							if (dist < minDistance) { valid = false; break; }
							}
						if (valid)
							{
							positions [ i ] = candidate;
							}

						attempts++;
						}
					if (!valid)
						{
						positions [ i ] = new(random.NextInt(0, worldSize.x), random.NextInt(0, worldSize.y));
						}
					}
				}
			else
				{
				int gridDim = (int)math.ceil(math.sqrt(positions.Length));
				float2 cellSize = new float2(worldSize) / gridDim;
				float jitterAmount = math.min(cellSize.x, cellSize.y) * 0.3f;
				for (int i = 0; i < positions.Length; i++)
					{
					int gridX = i % gridDim; int gridY = i / gridDim;
					float2 cellCenter = new float2(gridX + 0.5f, gridY + 0.5f) * cellSize;
					float2 jitter = new(random.NextFloat(-jitterAmount, jitterAmount), random.NextFloat(-jitterAmount, jitterAmount));
					float2 finalPos = cellCenter + jitter;
					positions [ i ] = new(math.clamp((int)finalPos.x, 0, worldSize.x - 1), math.clamp((int)finalPos.y, 0, worldSize.y - 1));
					}
				}
			}

		private static void FrameAllDistricts()
			{
			SceneView sceneView = SceneView.lastActiveSceneView;
			if (sceneView == null)
				{
				return;
				}

			DistrictAuthoring [ ] districts = Object.FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);
			if (districts.Length == 0)
				{
				return;
				}

			Bounds bounds = new(); bool init = false;
			foreach (DistrictAuthoring d in districts)
				{
				Vector3 pos = GetGizmoPosition(d);
				if (!init) { bounds = new Bounds(pos, Vector3.zero); init = true; }
				else
					{
					bounds.Encapsulate(pos);
					}
				}
			bounds.Expand(10f);
			sceneView.Frame(bounds, false);
			}
		}
	}
#endif
