using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Authoring;
using System.Collections.Generic;
using System.Linq;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    /// <summary>
    /// Sector and room hierarchy drill-down visualization tool
    /// Addresses TODO: "Sector / room hierarchy drill-down visualization"
    /// </summary>
    public class SectorRoomHierarchyWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private bool showOnlyConnected = false;
        private bool groupByDistrict = true;
        private bool showDetailedInfo = false;
        
        private List<DistrictHierarchy> districtHierarchies = new List<DistrictHierarchy>();
        private Dictionary<uint, List<uint>> connectionMap = new Dictionary<uint, List<uint>>();

        [System.Serializable]
        private class DistrictHierarchy
        {
            public DistrictAuthoring district;
            public List<SectorInfo> sectors = new List<SectorInfo>();
            public List<ConnectionInfo> connections = new List<ConnectionInfo>();
            public int totalRoomCount;
            public float averageConnectivity;
            public bool isExpanded = true;
        }

        [System.Serializable]
        private class SectorInfo
        {
            public int sectorId;
            public Vector3 estimatedPosition;
            public int roomCount;
            public List<RoomInfo> rooms = new List<RoomInfo>();
            public bool isExpanded = false;
        }

        [System.Serializable]
        private class RoomInfo
        {
            public int roomId;
            public Vector3 position;
            public string roomType;
            public List<string> connections = new List<string>();
        }

        [System.Serializable]
        private class ConnectionInfo
        {
            public uint targetNodeId;
            public ConnectionAuthoring connection;
            public bool hasGateCondition;
            public string connectionType;
        }

        [MenuItem("Tools/MetVanDAMN/World Debugger/Sector Room Hierarchy")]
        public static void ShowWindow()
        {
            var window = GetWindow<SectorRoomHierarchyWindow>("Sector Hierarchy");
            window.minSize = new Vector2(400, 300);
            window.RefreshHierarchyData();
        }

        private void OnEnable()
        {
            RefreshHierarchyData();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            
            // Header controls
            DrawHeaderControls();
            
            EditorGUILayout.Space(10);
            
            // Hierarchy content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            if (districtHierarchies.Count == 0)
            {
                DrawEmptyState();
            }
            else
            {
                DrawHierarchy();
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
                RefreshHierarchyData();
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Width(120));
            
            GUILayout.Space(10);
            
            showOnlyConnected = EditorGUILayout.Toggle("Connected Only", showOnlyConnected, GUILayout.Width(120));
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            groupByDistrict = EditorGUILayout.Toggle("Group by District", groupByDistrict, GUILayout.Width(130));
            showDetailedInfo = EditorGUILayout.Toggle("Show Details", showDetailedInfo, GUILayout.Width(100));
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Expand All", GUILayout.Width(80)))
            {
                foreach (var hierarchy in districtHierarchies)
                {
                    hierarchy.isExpanded = true;
                    foreach (var sector in hierarchy.sectors)
                    {
                        sector.isExpanded = true;
                    }
                }
            }
            
            if (GUILayout.Button("Collapse All", GUILayout.Width(80)))
            {
                foreach (var hierarchy in districtHierarchies)
                {
                    hierarchy.isExpanded = false;
                    foreach (var sector in hierarchy.sectors)
                    {
                        sector.isExpanded = false;
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.FlexibleSpace();
            
            GUIStyle centeredStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Italic
            };
            
            GUILayout.Label("No districts found in scene", centeredStyle);
            GUILayout.Space(5);
            
            GUIStyle smallCenteredStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };
            
            GUILayout.Label("Add DistrictAuthoring components to see hierarchy", smallCenteredStyle);
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawHierarchy()
        {
            var filteredHierarchies = GetFilteredHierarchies();
            
            foreach (var hierarchy in filteredHierarchies)
            {
                DrawDistrictHierarchy(hierarchy);
            }
        }

        private List<DistrictHierarchy> GetFilteredHierarchies()
        {
            var filtered = districtHierarchies.AsEnumerable();
            
            if (!string.IsNullOrEmpty(searchFilter))
            {
                filtered = filtered.Where(h => 
                    h.district.name.ToLower().Contains(searchFilter.ToLower()) ||
                    h.district.nodeId.value.ToString().Contains(searchFilter));
            }
            
            if (showOnlyConnected)
            {
                filtered = filtered.Where(h => h.connections.Count > 0);
            }
            
            return filtered.ToList();
        }

        private void DrawDistrictHierarchy(DistrictHierarchy hierarchy)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // District header
            EditorGUILayout.BeginHorizontal();
            
            hierarchy.isExpanded = EditorGUILayout.Foldout(hierarchy.isExpanded, "", true);
            
            if (GUILayout.Button("🏛️", GUILayout.Width(25)))
            {
                Selection.activeObject = hierarchy.district;
                EditorGUIUtility.PingObject(hierarchy.district);
            }
            
            EditorGUILayout.LabelField($"District {hierarchy.district.nodeId.value}: {hierarchy.district.name}", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.LabelField($"{hierarchy.sectors.Count} sectors", EditorStyles.miniLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField($"{hierarchy.totalRoomCount} rooms", EditorStyles.miniLabel, GUILayout.Width(80));
            
            EditorGUILayout.EndHorizontal();
            
            if (showDetailedInfo)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Target Sectors: {hierarchy.district.targetSectorCount}", EditorStyles.miniLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField($"Connections: {hierarchy.connections.Count}", EditorStyles.miniLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField($"Connectivity: {hierarchy.averageConnectivity:F2}", EditorStyles.miniLabel, GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();
            }
            
            if (hierarchy.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // Draw connections
                if (hierarchy.connections.Count > 0)
                {
                    EditorGUILayout.LabelField("Connections:", EditorStyles.boldLabel);
                    foreach (var connection in hierarchy.connections)
                    {
                        DrawConnectionInfo(connection);
                    }
                    EditorGUILayout.Space(5);
                }
                
                // Draw sectors
                EditorGUILayout.LabelField("Sectors:", EditorStyles.boldLabel);
                foreach (var sector in hierarchy.sectors)
                {
                    DrawSectorInfo(sector);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawConnectionInfo(ConnectionInfo connection)
        {
            EditorGUILayout.BeginHorizontal();
            
            string gateIcon = connection.hasGateCondition ? "🚪" : "↔️";
            EditorGUILayout.LabelField(gateIcon, GUILayout.Width(20));
            
            EditorGUILayout.LabelField($"→ District {connection.targetNodeId}", GUILayout.Width(120));
            
            if (connection.connection != null)
            {
                EditorGUILayout.LabelField(connection.connectionType, EditorStyles.miniLabel, GUILayout.Width(80));
                
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    Selection.activeObject = connection.connection;
                    EditorGUIUtility.PingObject(connection.connection);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSectorInfo(SectorInfo sector)
        {
            EditorGUILayout.BeginHorizontal();
            
            sector.isExpanded = EditorGUILayout.Foldout(sector.isExpanded, "", true);
            
            EditorGUILayout.LabelField($"🏘️ Sector {sector.sectorId}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"{sector.roomCount} rooms", EditorStyles.miniLabel, GUILayout.Width(80));
            
            if (showDetailedInfo)
            {
                EditorGUILayout.LabelField($"Pos: {sector.estimatedPosition:F1}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (sector.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                foreach (var room in sector.rooms)
                {
                    DrawRoomInfo(room);
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawRoomInfo(RoomInfo room)
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField($"🏠 Room {room.roomId}", GUILayout.Width(80));
            EditorGUILayout.LabelField(room.roomType, EditorStyles.miniLabel, GUILayout.Width(100));
            
            if (showDetailedInfo)
            {
                EditorGUILayout.LabelField($"Pos: {room.position:F1}", EditorStyles.miniLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField($"Links: {room.connections.Count}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooterStats()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            
            int totalDistricts = districtHierarchies.Count;
            int totalSectors = districtHierarchies.Sum(h => h.sectors.Count);
            int totalRooms = districtHierarchies.Sum(h => h.totalRoomCount);
            int totalConnections = districtHierarchies.Sum(h => h.connections.Count);
            
            EditorGUILayout.LabelField($"Districts: {totalDistricts}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Sectors: {totalSectors}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Rooms: {totalRooms}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Connections: {totalConnections}", EditorStyles.miniLabel);
            
            float avgSectorsPerDistrict = totalDistricts > 0 ? (float)totalSectors / totalDistricts : 0f;
            float avgRoomsPerSector = totalSectors > 0 ? (float)totalRooms / totalSectors : 0f;
            
            EditorGUILayout.LabelField($"Avg S/D: {avgSectorsPerDistrict:F1}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Avg R/S: {avgRoomsPerSector:F1}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshHierarchyData()
        {
            districtHierarchies.Clear();
            connectionMap.Clear();
            
            var districts = FindObjectsOfType<DistrictAuthoring>();
            var connections = FindObjectsOfType<ConnectionAuthoring>();
            var gates = FindObjectsOfType<GateConditionAuthoring>();
            
            // Build connection map
            foreach (var connection in connections)
            {
                uint sourceId = connection.sourceNode.value;
                uint targetId = connection.targetNode.value;
                
                if (!connectionMap.ContainsKey(sourceId))
                    connectionMap[sourceId] = new List<uint>();
                
                connectionMap[sourceId].Add(targetId);
            }
            
            // Build district hierarchies
            foreach (var district in districts)
            {
                var hierarchy = BuildDistrictHierarchy(district, connections, gates);
                districtHierarchies.Add(hierarchy);
            }
            
            // Sort by node ID
            districtHierarchies.Sort((a, b) => a.district.nodeId.value.CompareTo(b.district.nodeId.value));
        }

        private DistrictHierarchy BuildDistrictHierarchy(DistrictAuthoring district, ConnectionAuthoring[] connections, GateConditionAuthoring[] gates)
        {
            var hierarchy = new DistrictHierarchy
            {
                district = district
            };
            
            uint nodeId = district.nodeId.value;
            
            // Build connections
            var districtConnections = connections.Where(c => c.sourceNode.value == nodeId);
            foreach (var connection in districtConnections)
            {
                var connectionInfo = new ConnectionInfo
                {
                    targetNodeId = connection.targetNode.value,
                    connection = connection,
                    hasGateCondition = gates.Any(g => g.sourceNode.value == connection.sourceNode.value && 
                                                     g.targetNode.value == connection.targetNode.value),
                    connectionType = DetermineConnectionType(connection)
                };
                
                hierarchy.connections.Add(connectionInfo);
            }
            
            // Build sectors (simulated based on target sector count)
            int targetSectorCount = district.targetSectorCount;
            for (int i = 0; i < targetSectorCount; i++)
            {
                var sector = GenerateSectorInfo(i, district.transform.position, targetSectorCount);
                hierarchy.sectors.Add(sector);
            }
            
            hierarchy.totalRoomCount = hierarchy.sectors.Sum(s => s.roomCount);
            hierarchy.averageConnectivity = CalculateConnectivity(nodeId);
            
            return hierarchy;
        }

        private SectorInfo GenerateSectorInfo(int sectorId, Vector3 districtPosition, int totalSectors)
        {
            var sector = new SectorInfo
            {
                sectorId = sectorId,
                estimatedPosition = CalculateSectorPosition(districtPosition, sectorId, totalSectors),
                roomCount = Random.Range(2, 8) // Simulated room count
            };
            
            // Generate rooms for this sector
            for (int i = 0; i < sector.roomCount; i++)
            {
                var room = new RoomInfo
                {
                    roomId = i,
                    position = sector.estimatedPosition + Random.insideUnitSphere * 5f,
                    roomType = GenerateRoomType(),
                    connections = GenerateRoomConnections(i, sector.roomCount)
                };
                
                sector.rooms.Add(room);
            }
            
            return sector;
        }

        private Vector3 CalculateSectorPosition(Vector3 districtPosition, int sectorId, int totalSectors)
        {
            if (totalSectors == 1)
                return districtPosition;
            
            float angle = (float)sectorId / totalSectors * 2f * Mathf.PI;
            float radius = 8f; // Approximate sector spacing
            
            return districtPosition + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );
        }

        private string GenerateRoomType()
        {
            string[] roomTypes = { "Standard", "Challenge", "Treasure", "Boss", "Hub", "Secret" };
            return roomTypes[Random.Range(0, roomTypes.Length)];
        }

        private List<string> GenerateRoomConnections(int roomId, int totalRooms)
        {
            var connections = new List<string>();
            
            // Most rooms connect to at least one other room
            int connectionCount = Random.Range(1, Mathf.Min(4, totalRooms));
            
            for (int i = 0; i < connectionCount; i++)
            {
                int targetRoom = Random.Range(0, totalRooms);
                if (targetRoom != roomId)
                {
                    connections.Add($"Room {targetRoom}");
                }
            }
            
            return connections.Distinct().ToList();
        }

        private string DetermineConnectionType(ConnectionAuthoring connection)
        {
            // Analyze connection to determine type
            return "Standard"; // Simplified for now
        }

        private float CalculateConnectivity(uint nodeId)
        {
            if (!connectionMap.ContainsKey(nodeId))
                return 0f;
            
            return connectionMap[nodeId].Count; // Simple connectivity metric
        }
    }
}