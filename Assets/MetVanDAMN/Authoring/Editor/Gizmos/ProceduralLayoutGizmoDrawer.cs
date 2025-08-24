#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
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
            
            GUILayout.BeginArea(new Rect(10, 10, 250, 200));
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

            // Draw randomization mode info
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
            
            // Determine if this is an unplaced district (coordinates 0,0)
            bool isUnplaced = district.nodeId.coordinates.x == 0 && district.nodeId.coordinates.y == 0;
            
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
            // Draw gray placeholder for unplaced districts
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            
            // Draw wireframe cube
            Handles.DrawWireCube(position, Vector3.one * 2f);
            
            // Draw label
            Handles.color = Color.gray;
            Handles.Label(position + Vector3.up * 1.5f, $"UNPLACED\nNode: {district.nodeId.value}\nLevel: {district.nodeId.level}");
        }

        private static void DrawPlacedDistrictGizmo(Vector3 position, DistrictAuthoring district)
        {
            // Use a descriptive default color for placed districts since biomeType is no longer on DistrictAuthoring
            Color biomeColor = _settings != null ? _settings.placedDistrictColor : Color.cyan;
            
            // Draw filled district area
            Handles.color = new Color(biomeColor.r, biomeColor.g, biomeColor.b, 0.3f);
            Vector3 size = new Vector3(_settings.districtSize.x, 0.5f, _settings.districtSize.y);
            
            // Draw district bounds
            DrawDistrictBounds(position, size, biomeColor);
            
            // Draw district label
            Handles.color = _settings.labelColor;
            string label = $"District {district.nodeId}\nCoords: ({district.gridCoordinates.x}, {district.gridCoordinates.y})\nLevel: {district.level}";
            Handles.Label(position + Vector3.up * 2f, label);
        }

        private static void DrawDistrictBounds(Vector3 position, Vector3 size, Color biomeColor)
        {
            // Draw filled quad
            Vector3[] corners = new Vector3[4];
            corners[0] = position + new Vector3(-size.x/2, 0, -size.z/2);
            corners[1] = position + new Vector3(size.x/2, 0, -size.z/2);
            corners[2] = position + new Vector3(size.x/2, 0, size.z/2);
            corners[3] = position + new Vector3(-size.x/2, 0, size.z/2);
            
            Handles.color = new Color(biomeColor.r, biomeColor.g, biomeColor.b, 0.2f);
            Handles.DrawAAConvexPolygon(corners);
            
            // Draw outline
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
            
            // Draw wire disc for biome influence radius
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
            
            // Draw connection line
            Handles.DrawLine(from + Vector3.up * 0.5f, to + Vector3.up * 0.5f);
            
            // Draw arrow head(s)
            Vector3 direction = (to - from).normalized;
            Vector3 arrowPos = Vector3.Lerp(from, to, 0.7f) + Vector3.up * 0.5f;
            
            // Draw arrow
            Vector3 right = Vector3.Cross(direction, Vector3.up) * _settings.connectionArrowSize;
            Vector3 back = -direction * _settings.connectionArrowSize;
            
            Handles.DrawLine(arrowPos, arrowPos + back + right);
            Handles.DrawLine(arrowPos, arrowPos + back - right);
            
            // For bidirectional, draw second arrow
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
                    district.nodeId.coordinates.x * _settings.gridCellSize,
                    0,
                    district.nodeId.coordinates.y * _settings.gridCellSize
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
            Debug.Log("Preview Layout: This would trigger procedural layout preview in edit mode");
            // TODO: Implement preview layout generation that runs systems in edit mode
        }

        private static void FrameAllDistricts()
        {
            var districts = Object.FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);
            if (districts.Length == 0) return;

            Bounds bounds = new Bounds();
            bool boundsInitialized = false;

            foreach (var district in districts)
            {
                Vector3 pos = GetGizmoPosition(district);
                if (!boundsInitialized)
                {
                    bounds = new Bounds(pos, Vector3.zero);
                    boundsInitialized = true;
                }
                else
                {
                    bounds.Encapsulate(pos);
                }
            }

            // Expand bounds slightly
            bounds.Expand(10f);

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.Frame(bounds, false);
            }
        }
    }
}
#endif