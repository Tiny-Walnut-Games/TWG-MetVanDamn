#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Shared;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Graph; // for DistrictPlacementStrategy

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    // Fallback duplicate (only if graph assembly reference stripped) - kept internal & conditional
#if !METVD_GRAPH_REF // define this symbol if needed to avoid enum clash
    internal enum DistrictPlacementStrategy : byte { PoissonDisc = 0, JitteredGrid = 1 }
#endif
    /// <summary>
    /// Enhanced gizmo drawer for procedural world layout debugging
    /// Provides visualization for district placement, connections, and rule randomization
    /// </summary>
    [InitializeOnLoad]
    public static class ProceduralLayoutGizmoDrawer
    {
        private static MetVDGizmoSettings _settings;
        private static bool _showUnplacedDistricts = true;
        private static bool _showPlacedDistricts = true;
        private static bool _showConnections = true;
        private static bool _showBiomeRadius = true;
        private static bool _showRandomizationMode = true;

        static ProceduralLayoutGizmoDrawer()
        {
            LoadSettings();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void LoadSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:MetVDGizmoSettings");
            if (guids.Length > 0)
            {
                _settings = AssetDatabase.LoadAssetAtPath<MetVDGizmoSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (_settings == null)
            {
                LoadSettings();
                if (_settings == null) return;
            }

            DrawDebugControls();
            DrawProceduralLayoutGizmos();
        }

        private static void DrawDebugControls()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 250, 220));
            GUILayout.BeginVertical("box");
            GUILayout.Label("Procedural Layout Debug", EditorStyles.boldLabel);
            _showUnplacedDistricts = GUILayout.Toggle(_showUnplacedDistricts, "Show Unplaced Districts (0,0)");
            _showPlacedDistricts = GUILayout.Toggle(_showPlacedDistricts, "Show Placed Districts");
            _showConnections = GUILayout.Toggle(_showConnections, "Show Connections");
            _showBiomeRadius = GUILayout.Toggle(_showBiomeRadius, "Show Biome Radius");
            _showRandomizationMode = GUILayout.Toggle(_showRandomizationMode, "Show Randomization Mode");
            GUILayout.Space(10);
            if (GUILayout.Button("Preview Layout"))
            {
                PreviewProceduralLayout();
            }
            if (GUILayout.Button("Frame All Districts"))
            {
                FrameAllDistricts();
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private static void DrawProceduralLayoutGizmos()
        {
            var districts = Object.FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);
            var worldConfig = Object.FindFirstObjectByType<WorldConfigurationAuthoring>();
            if (_showRandomizationMode && worldConfig != null)
            {
                DrawRandomizationModeInfo(worldConfig);
            }
            foreach (var district in districts)
            {
                DrawDistrictGizmos(district);
            }
            if (_showConnections)
            {
                DrawConnectionGizmos();
            }
        }

        private static void DrawRandomizationModeInfo(WorldConfigurationAuthoring worldConfig)
        {
            var camera = SceneView.currentDrawingSceneView?.camera;
            if (camera == null) return;
            Vector3 screenPos = camera.transform.position + camera.transform.forward * 10f;
            Handles.color = Color.yellow;
            Handles.Label(screenPos, $"Randomization Mode: {worldConfig.randomizationMode}\nSeed: {worldConfig.seed}\nWorld Size: {worldConfig.worldSize}");
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
            Vector3[] corners = new Vector3[4];
            corners[0] = position + new Vector3(-size.x / 2, 0, -size.z / 2);
            corners[1] = position + new Vector3(size.x / 2, 0, -size.z / 2);
            corners[2] = position + new Vector3(size.x / 2, 0, size.z / 2);
            corners[3] = position + new Vector3(-size.x / 2, 0, size.z / 2);
            Handles.color = new Color(biomeColor.r, biomeColor.g, biomeColor.b, 0.2f);
            Handles.DrawAAConvexPolygon(corners);
            Handles.color = biomeColor;
            Handles.DrawLine(corners[0], corners[1]);
            Handles.DrawLine(corners[1], corners[2]);
            Handles.DrawLine(corners[2], corners[3]);
            Handles.DrawLine(corners[3], corners[0]);
        }

        private static void DrawBiomeRadiusGizmo(Vector3 position, DistrictAuthoring district)
        {
            Color biomeColor = GetBiomeColor(BiomeType.HubArea);
            Handles.color = new Color(biomeColor.r, biomeColor.g, biomeColor.b, 0.1f);
            Handles.DrawWireDisc(position, Vector3.up, _settings.biomeRadius);
        }

        private static void DrawConnectionGizmos()
        {
            var connections = Object.FindObjectsByType<ConnectionAuthoring>(FindObjectsSortMode.None);
            foreach (var connection in connections)
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
            Vector3 arrowPos = Vector3.Lerp(from, to, 0.7f) + Vector3.up * 0.5f;
            Vector3 right = Vector3.Cross(direction, Vector3.up) * _settings.connectionArrowSize;
            Vector3 back = -direction * _settings.connectionArrowSize;
            Handles.DrawLine(arrowPos, arrowPos + back + right);
            Handles.DrawLine(arrowPos, arrowPos + back - right);
            if (connection.type == ConnectionType.Bidirectional)
            {
                arrowPos = Vector3.Lerp(from, to, 0.3f) + Vector3.up * 0.5f;
                direction = -direction;
                back = -direction * _settings.connectionArrowSize;
                Handles.DrawLine(arrowPos, arrowPos + back + right);
                Handles.DrawLine(arrowPos, arrowPos + back - right);
            }
        }

        private static Vector3 GetGizmoPosition(DistrictAuthoring district)
        {
            if (_settings.useGridCoordinatesForGizmos)
            {
                return new Vector3(
                    district.gridCoordinates.x * _settings.gridCellSize,
                    0,
                    district.gridCoordinates.y * _settings.gridCellSize
                ) + _settings.gridOriginOffset;
            }
            return district.transform.position;
        }

        private static Color GetBiomeColor(BiomeType biomeType)
        {
            return biomeType switch
            {
                BiomeType.SolarPlains => Color.yellow,
                BiomeType.CrystalCaverns => Color.cyan,
                BiomeType.SkyGardens => Color.green,
                BiomeType.ShadowRealms => new Color(0.5f, 0f, 0.5f, 1f),
                BiomeType.DeepUnderwater => Color.blue,
                BiomeType.VoidChambers => Color.black,
                BiomeType.VolcanicCore => Color.red,
                BiomeType.PowerPlant => new Color(1f, 0.5f, 0f, 1f),
                BiomeType.PlasmaFields => Color.magenta,
                BiomeType.FrozenWastes => Color.white,
                BiomeType.IceCatacombs => new Color(0.7f, 0.9f, 1f, 1f),
                BiomeType.CryogenicLabs => new Color(0.5f, 0.8f, 1f, 1f),
                BiomeType.HubArea => new Color(0.8f, 0.8f, 0.8f, 1f),
                BiomeType.TransitionZone => new Color(0.6f, 0.6f, 0.6f, 1f),
                BiomeType.AncientRuins => new Color(0.6f, 0.4f, 0.2f, 1f),
                _ => Color.gray
            };
        }

        private static void PreviewProceduralLayout()
        {
            var worldConfig = Object.FindFirstObjectByType<WorldConfigurationAuthoring>();
            if (worldConfig == null)
            {
                Debug.LogWarning("Procedural Layout Preview: WorldConfigurationAuthoring not found in scene.");
                return;
            }
            var districts = Object.FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);
            if (districts.Length == 0)
            {
                Debug.LogWarning("Procedural Layout Preview: No DistrictAuthoring components found.");
                return;
            }
            // Collect unplaced (level 0 & at 0,0) districts
            var unplaced = new System.Collections.Generic.List<DistrictAuthoring>();
            foreach (var d in districts)
            {
                if (d.level == 0 && d.gridCoordinates.x == 0 && d.gridCoordinates.y == 0)
                    unplaced.Add(d);
            }
            if (unplaced.Count == 0)
            {
                Debug.Log("Procedural Layout Preview: All districts already placed.");
                return;
            }
            int targetCount = worldConfig.targetSectors > 0 ? math.min(worldConfig.targetSectors, unplaced.Count) : unplaced.Count;
            var random = new Unity.Mathematics.Random((uint)(worldConfig.seed == 0 ? 1 : worldConfig.seed));
            var strategy = targetCount > 16 ? DistrictPlacementStrategy.JitteredGrid : DistrictPlacementStrategy.PoissonDisc;
            var positions = new int2[targetCount];
            GeneratePositionsPreview(positions, worldConfig.worldSize, strategy, ref random);
            for (int i = 0; i < targetCount; i++)
            {
                unplaced[i].gridCoordinates = positions[i];
                EditorUtility.SetDirty(unplaced[i]);
            }
            Debug.Log($"Procedural Layout Preview: Placed {targetCount} districts using {strategy} strategy.");
            SceneView.RepaintAll();
        }

        private static void GeneratePositionsPreview(int2[] positions, int2 worldSize, DistrictPlacementStrategy strategy, ref Unity.Mathematics.Random random)
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
                            float dist = math.length(new float2(candidate - positions[j]));
                            if (dist < minDistance) { valid = false; break; }
                        }
                        if (valid) positions[i] = candidate;
                        attempts++;
                    }
                    if (!valid)
                        positions[i] = new(random.NextInt(0, worldSize.x), random.NextInt(0, worldSize.y));
                }
            }
            else // JitteredGrid
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
                    positions[i] = new(math.clamp((int)finalPos.x, 0, worldSize.x - 1), math.clamp((int)finalPos.y, 0, worldSize.y - 1));
                }
            }
        }

        private static void FrameAllDistricts()
        {
            var districts = Object.FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);
            if (districts.Length == 0) return;
            Bounds bounds = new(); bool init = false;
            foreach (var d in districts)
            {
                Vector3 pos = GetGizmoPosition(d);
                if (!init) { bounds = new Bounds(pos, Vector3.zero); init = true; }
                else bounds.Encapsulate(pos);
            }
            bounds.Expand(10f);
            SceneView.lastActiveSceneView?.Frame(bounds, false);
        }
    }
}
#endif
